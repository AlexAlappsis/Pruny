using FluentAssertions;
using Pruny.Core.Calculation;
using Pruny.Core.Models;

namespace Pruny.Core.Tests.Calculation;

public class ProfitCalculationTests
{
    [Fact]
    public void CalculateUnitCosts_WithOutputPrice_CalculatesProfitPerUnit()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 10 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 5 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 100m, Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 300m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        unitCost.OutputPrice.Should().Be(300m);
        unitCost.CostPerUnit.Should().Be(200m);
        unitCost.ProfitPerUnit.Should().Be(100m);
    }

    [Fact]
    public void CalculateUnitCosts_WithOutputPrice_CalculatesProfitPerRun()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 10 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 5 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 100m, Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 300m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var profitPerUnit = 300m - 200m;
        var profitPerRun = profitPerUnit * 5m;

        unitCost.ProfitPerRun.Should().Be(profitPerRun);
    }

    [Fact]
    public void CalculateUnitCosts_WithOutputPrice_CalculatesProfitPer24Hours()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 10 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 5 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 100m, Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 300m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var profitPerUnit = 300m - 200m;
        var profitPerRun = profitPerUnit * 5m;
        var runsPerDay = 24m * 60m / 60m;
        var profitPer24Hours = profitPerRun * runsPerDay;

        unitCost.ProfitPer24Hours.Should().Be(profitPer24Hours);
        unitCost.ProfitPer24Hours.Should().Be(500m * 24m);
    }

    [Fact]
    public void CalculateUnitCosts_WithEfficiencyBoost_IncreasesRunsPerDay()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 10 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 5 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 100m, Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 300m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal> { 2.0m }
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var adjustedDuration = 60m / 2.0m;
        var runsPerDay = 24m * 60m / adjustedDuration;
        var profitPerRun = (300m - 200m) * 5m;
        var expectedProfitPer24Hours = profitPerRun * runsPerDay;

        unitCost.ProfitPer24Hours.Should().Be(expectedProfitPer24Hours);
        unitCost.ProfitPer24Hours.Should().Be(500m * 48m);
    }

    [Fact]
    public void CalculateUnitCosts_WithZeroOutputPrice_HasNullProfit()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 1 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 0m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        unitCost.OutputPrice.Should().BeNull();
        unitCost.ProfitPerUnit.Should().BeNull();
        unitCost.ProfitPerRun.Should().BeNull();
        unitCost.ProfitPer24Hours.Should().BeNull();
    }

    [Fact]
    public void CalculateUnitCosts_WithNegativeProfit_CalculatesLoss()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 10 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 1 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 100m, Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, CustomValue = 500m, Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfig = new WorkforceConfig
        {
            Id = "WFC1",
            WorkforceTypes = new List<WorkforceTypeConfig>()
        };

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfig
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        unitCost.CostPerUnit.Should().Be(1000m);
        unitCost.ProfitPerUnit.Should().Be(-500m);
        unitCost.ProfitPerRun.Should().Be(-500m);
    }
}
