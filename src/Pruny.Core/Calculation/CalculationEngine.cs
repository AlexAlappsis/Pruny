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

                result.RecalculatedLineIds.Add(lineId);
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

        var workforceCost = CalculateWorkforceCost(workforce, workforceConfig, adjustedDuration, previousUnitCosts);
        var inputCosts = CalculateInputCosts(line, recipe, previousUnitCosts);
        var totalCost = workforceCost + inputCosts;

        var totalOutputQuantity = recipe.Outputs.Sum(o => o.Quantity);

        return recipe.Outputs.Select(output =>
        {
            var outputPrice = ResolvePrice(output.MaterialId, line.OutputPriceSources, previousUnitCosts);
            var costPerUnit = totalCost / output.Quantity;

            decimal? profitPerUnit = null;
            decimal? profitPerRun = null;
            decimal? profitPer24Hours = null;

            if (outputPrice > 0)
            {
                profitPerUnit = outputPrice - costPerUnit;
                profitPerRun = profitPerUnit * output.Quantity;

                var runsPerDay = 24m * 60m / adjustedDuration;
                profitPer24Hours = profitPerRun * runsPerDay;
            }

            return new UnitCost
            {
                MaterialId = output.MaterialId,
                ProductionLineId = line.Id,
                CostPerUnit = costPerUnit,
                WorkforceCost = workforceCost / totalOutputQuantity,
                InputCosts = inputCosts / totalOutputQuantity,
                OverallEfficiency = overallEfficiency,
                OutputPrice = outputPrice > 0 ? outputPrice : null,
                ProfitPerUnit = profitPerUnit,
                ProfitPerRun = profitPerRun,
                ProfitPer24Hours = profitPer24Hours
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
        decimal durationMinutes,
        Dictionary<string, UnitCost> previousUnitCosts)
    {
        decimal totalCost = 0;

        foreach (var worker in workforce)
        {
            var workerTypeConfig = config.WorkforceTypes.FirstOrDefault(w => w.WorkforceType == worker.WorkforceType);
            if (workerTypeConfig == null)
                continue;

            var costPerWorkerPerMinute = CalculateWorkerTypeCostPerMinute(workerTypeConfig, previousUnitCosts);
            totalCost += worker.Count * costPerWorkerPerMinute * durationMinutes;
        }

        return totalCost;
    }

    private decimal CalculateWorkerTypeCostPerMinute(
        WorkforceTypeConfig workerTypeConfig,
        Dictionary<string, UnitCost> previousUnitCosts)
    {
        decimal totalCostPer100WorkersPer24Hours = 0;

        foreach (var consumption in workerTypeConfig.MaterialConsumption)
        {
            var materialPrice = ResolvePrice(
                consumption.MaterialId,
                new Dictionary<string, PriceSource> { { consumption.MaterialId, consumption.PriceSource } },
                previousUnitCosts);

            totalCostPer100WorkersPer24Hours += consumption.QuantityPer100WorkersPer24Hours * materialPrice;
        }

        var costPerSingleWorkerPer24Hours = totalCostPer100WorkersPer24Hours / 100m;
        var costPerSingleWorkerPerMinute = costPerSingleWorkerPer24Hours / (24m * 60m);

        return costPerSingleWorkerPerMinute;
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
