namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public class CalculationEngine : ICalculationEngine
{
    public CalculationResult CalculateProductionLines(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes,
        Dictionary<string, Building> buildings,
        Dictionary<string, WorkforceTypeConfig> workforceConfigs,
        PriceSourceRegistry priceRegistry,
        HashSet<string> buildingsRequiringWholeUnitRounding)
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

                var productionLineCalculations = CalculateProductionLine(
                    line, recipe, building, workforceConfigs, priceRegistry, result.ProductionLineCalculations, buildingsRequiringWholeUnitRounding);

                foreach (var productionLineCalculation in productionLineCalculations)
                {
                    result.ProductionLineCalculations[productionLineCalculation.ProductionLineId] = productionLineCalculation;
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

    private List<ProductionLineCalculation> CalculateProductionLine(
        ProductionLine line,
        Recipe recipe,
        Building building,
        Dictionary<string, WorkforceTypeConfig> workforceConfigs,
        PriceSourceRegistry priceRegistry,
        Dictionary<string, ProductionLineCalculation> previousUnitCosts,
        HashSet<string> buildingsRequiringWholeUnitRounding)
    {
        var workforce = line.WorkforceOverride ?? building.DefaultWorkforce;
        var workforceEfficiency = CalculateWorkforceEfficiency(workforce, building.DefaultWorkforce);
        var overallEfficiency = CalculateOverallEfficiency(workforceEfficiency, line.AdditionalEfficiencyModifiers);
        var adjustedDuration = recipe.DurationMinutes / overallEfficiency;

        var effectiveOutputs = line.OutputOverrides ?? recipe.Outputs;

        if (buildingsRequiringWholeUnitRounding.Contains(recipe.BuildingId))
        {
            effectiveOutputs = ApplyWholeUnitRounding(effectiveOutputs, recipe.DurationMinutes, overallEfficiency, out adjustedDuration);
        }

        var workforceCost = CalculateWorkforceCost(workforce, line.WorkforceConfigMapping, workforceConfigs, adjustedDuration, priceRegistry, previousUnitCosts);
        var inputCosts = CalculateInputCosts(line, recipe, priceRegistry, previousUnitCosts);
        var totalCost = workforceCost + inputCosts;

        var totalOutputQuantity = effectiveOutputs.Sum(o => o.Quantity);

        return effectiveOutputs.Select(output =>
        {
            var outputPrice = ResolvePrice(output.MaterialId, line.OutputPriceSources, priceRegistry, previousUnitCosts);
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

            return new ProductionLineCalculation
            {
                MaterialId = output.MaterialId,
                ProductionLineId = line.Id,
                OutputQuantity = output.Quantity,
                AdjustedDurationMinutes = adjustedDuration,
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
        Dictionary<WorkforceType, string>? workforceConfigMapping,
        Dictionary<string, WorkforceTypeConfig> workforceConfigs,
        decimal durationMinutes,
        PriceSourceRegistry priceRegistry,
        Dictionary<string, ProductionLineCalculation> previousUnitCosts)
    {
        decimal totalCost = 0;

        foreach (var worker in workforce)
        {
            WorkforceTypeConfig? workerTypeConfig = null;

            if (workforceConfigMapping != null &&
                workforceConfigMapping.TryGetValue(worker.WorkforceType, out var configName) &&
                workforceConfigs.TryGetValue(configName, out workerTypeConfig))
            {
            }
            else
            {
                workerTypeConfig = workforceConfigs.Values
                    .FirstOrDefault(c => c.WorkforceType == worker.WorkforceType);
            }

            if (workerTypeConfig == null)
                continue;

            var costPerWorkerPerMinute = CalculateWorkerTypeCostPerMinute(workerTypeConfig, priceRegistry, previousUnitCosts);
            totalCost += worker.Count * costPerWorkerPerMinute * durationMinutes;
        }

        return totalCost;
    }

    private decimal CalculateWorkerTypeCostPerMinute(
        WorkforceTypeConfig workerTypeConfig,
        PriceSourceRegistry priceRegistry,
        Dictionary<string, ProductionLineCalculation> previousUnitCosts)
    {
        decimal totalCostPer100WorkersPer24Hours = 0;

        foreach (var consumption in workerTypeConfig.MaterialConsumption)
        {
            var materialPrice = ResolvePrice(
                consumption.MaterialId,
                new Dictionary<string, PriceSource> { { consumption.MaterialId, consumption.PriceSource } },
                priceRegistry,
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
        PriceSourceRegistry priceRegistry,
        Dictionary<string, ProductionLineCalculation> previousUnitCosts)
    {
        decimal totalInputCost = 0;

        foreach (var input in recipe.Inputs)
        {
            var price = ResolvePrice(input.MaterialId, line.InputPriceSources, priceRegistry, previousUnitCosts);
            totalInputCost += input.Quantity * price;
        }

        return totalInputCost;
    }

    private decimal ResolvePrice(
        string materialId,
        Dictionary<string, PriceSource> priceSources,
        PriceSourceRegistry priceRegistry,
        Dictionary<string, ProductionLineCalculation> previousUnitCosts)
    {
        if (!priceSources.TryGetValue(materialId, out var priceSource))
            return 0m;

        if (priceSource.Type == PriceSourceType.ProductionLine &&
            previousUnitCosts.TryGetValue(priceSource.SourceIdentifier, out var unitCost))
        {
            return unitCost.CostPerUnit;
        }

        return priceRegistry.GetPrice(materialId, priceSource);
    }

    private List<RecipeItem> ApplyWholeUnitRounding(
        List<RecipeItem> outputs,
        decimal baseDurationMinutes,
        decimal overallEfficiency,
        out decimal adjustedDuration)
    {
        var roundedOutputs = new List<RecipeItem>();
        decimal totalAdditionalTime = 0;

        foreach (var output in outputs)
        {
            var roundedQuantity = Math.Ceiling(output.Quantity);
            var delta = roundedQuantity - output.Quantity;

            if (delta > 0 && output.Quantity > 0)
            {
                var additionalTimeForThisOutput = baseDurationMinutes * delta / output.Quantity;
                totalAdditionalTime += additionalTimeForThisOutput;
            }

            roundedOutputs.Add(new RecipeItem
            {
                MaterialId = output.MaterialId,
                Quantity = roundedQuantity
            });
        }

        adjustedDuration = (baseDurationMinutes + totalAdditionalTime) / overallEfficiency;
        return roundedOutputs;
    }
}
