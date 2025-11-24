namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public class CalculationEngine : ICalculationEngine
{
    public CalculationResult CalculateUnitCosts(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes,
        Dictionary<string, Building> buildings,
        WorkforceConfig workforceConfig)
    {
        var result = new CalculationResult();

        try
        {
            var resolver = new DependencyResolver();
            var orderedLineIds = resolver.ResolveDependencyOrder(productionLines, recipes);

            var linesById = productionLines.ToDictionary(l => l.Id);

            foreach (var lineId in orderedLineIds)
            {
                if (!linesById.TryGetValue(lineId, out var line))
                    continue;

                if (!recipes.TryGetValue(line.RecipeId, out var recipe))
                {
                    result.Errors.Add($"Recipe {line.RecipeId} not found for production line {lineId}");
                    continue;
                }

                if (!buildings.TryGetValue(recipe.BuildingId, out var building))
                {
                    result.Errors.Add($"Building {recipe.BuildingId} not found for recipe {line.RecipeId}");
                    continue;
                }

                var unitCosts = CalculateProductionLineUnitCosts(
                    line, recipe, building, workforceConfig, result.UnitCosts);

                foreach (var unitCost in unitCosts)
                {
                    result.UnitCosts[unitCost.MaterialId] = unitCost;
                }
            }
        }
        catch (CircularDependencyException ex)
        {
            result.Errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Calculation failed: {ex.Message}");
        }

        return result;
    }

    private List<UnitCost> CalculateProductionLineUnitCosts(
        ProductionLine line,
        Recipe recipe,
        Building building,
        WorkforceConfig workforceConfig,
        Dictionary<string, UnitCost> previousUnitCosts)
    {
        var workforce = line.WorkforceOverride ?? building.DefaultWorkforce;
        var workforceEfficiency = CalculateWorkforceEfficiency(workforce, building.DefaultWorkforce);
        var overallEfficiency = CalculateOverallEfficiency(workforceEfficiency, line.AdditionalEfficiencyModifiers);
        var adjustedDuration = recipe.DurationMinutes / overallEfficiency;

        var workforceCost = CalculateWorkforceCost(workforce, workforceConfig, adjustedDuration);
        var inputCosts = CalculateInputCosts(line, recipe, previousUnitCosts);
        var totalCost = workforceCost + inputCosts;

        var totalOutputQuantity = recipe.Outputs.Sum(o => o.Quantity);

        return recipe.Outputs.Select(output =>
        {
            var outputPrice = ResolvePrice(output.MaterialId, line.OutputPriceSources, previousUnitCosts);
            var costPerUnit = totalCost / output.Quantity;

            return new UnitCost
            {
                MaterialId = output.MaterialId,
                ProductionLineId = line.Id,
                CostPerUnit = costPerUnit,
                WorkforceCost = workforceCost / totalOutputQuantity,
                InputCosts = inputCosts / totalOutputQuantity,
                OverallEfficiency = overallEfficiency
            };
        }).ToList();
    }

    private decimal CalculateWorkforceEfficiency(
        List<WorkforceRequirement> actual,
        List<WorkforceRequirement> required)
    {
        var totalRequired = required.Sum(r => r.Count);
        if (totalRequired == 0)
            return 1.0m;

        var totalActual = actual.Sum(a => a.Count);
        return totalActual / (decimal)totalRequired;
    }

    private decimal CalculateOverallEfficiency(
        decimal workforceEfficiency,
        List<decimal> additionalModifiers)
    {
        var overall = workforceEfficiency;
        foreach (var modifier in additionalModifiers)
        {
            overall *= modifier;
        }
        return overall;
    }

    private decimal CalculateWorkforceCost(
        List<WorkforceRequirement> workforce,
        WorkforceConfig config,
        decimal durationMinutes)
    {
        decimal totalCost = 0;

        foreach (var worker in workforce)
        {
            if (config.CostPerWorkerTypePerMinute.TryGetValue(worker.WorkforceType, out var costPerMinute))
            {
                totalCost += worker.Count * costPerMinute * durationMinutes;
            }
        }

        return totalCost;
    }

    private decimal CalculateInputCosts(
        ProductionLine line,
        Recipe recipe,
        Dictionary<string, UnitCost> previousUnitCosts)
    {
        decimal totalInputCost = 0;

        foreach (var input in recipe.Inputs)
        {
            var price = ResolvePrice(input.MaterialId, line.InputPriceSources, previousUnitCosts);
            totalInputCost += input.Quantity * price;
        }

        return totalInputCost;
    }

    private decimal ResolvePrice(
        string materialId,
        Dictionary<string, PriceSource> priceSources,
        Dictionary<string, UnitCost> previousUnitCosts)
    {
        if (!priceSources.TryGetValue(materialId, out var priceSource))
            return 0m;

        decimal basePrice = priceSource.Type switch
        {
            PriceSourceType.Api => priceSource.ApiPrice ?? 0m,
            PriceSourceType.Custom => priceSource.CustomValue ?? 0m,
            PriceSourceType.ProductionLine =>
                priceSource.ProductionLineId != null &&
                previousUnitCosts.TryGetValue(materialId, out var unitCost)
                    ? unitCost.CostPerUnit
                    : 0m,
            _ => 0m
        };

        return ApplyAdjustments(basePrice, priceSource.Adjustments);
    }

    private decimal ApplyAdjustments(decimal basePrice, List<Adjustment> adjustments)
    {
        var price = basePrice;

        foreach (var adjustment in adjustments)
        {
            price = adjustment.Type switch
            {
                AdjustmentType.Percentage => price * adjustment.Value,
                AdjustmentType.Flat => price + adjustment.Value,
                _ => price
            };
        }

        return price;
    }
}
