using FluentAssertions;
using Pruny.Core.Calculation;
using Pruny.Core.Models;

namespace Pruny.Core.Tests.Calculation;

public class EfficiencyCalculationTests
{
    [Fact]
    public void CalculateUnitCosts_FullWorkforce_HasFullEfficiency()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 }
            }
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
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];
        unitCost.OverallEfficiency.Should().Be(1.0m);
    }

    [Fact]
    public void CalculateUnitCosts_HalfWorkforce_HasHalfEfficiency()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 }
            }
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
            WorkforceOverride = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 5 }
            },
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];
        unitCost.OverallEfficiency.Should().Be(0.5m);
    }

    [Fact]
    public void CalculateUnitCosts_QuarterWorkforce_HasQuarterEfficiency()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 20 }
            }
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
            WorkforceOverride = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 5 }
            },
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];
        unitCost.OverallEfficiency.Should().Be(0.25m);
    }

    [Fact]
    public void CalculateUnitCosts_MultipleWorkerTypes_CalculatesLinearEfficiency()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 },
                new() { WorkforceType = WorkforceType.SETTLER, Count = 5 }
            }
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
            WorkforceOverride = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 },
                new() { WorkforceType = WorkforceType.SETTLER, Count = 2 }
            },
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            },
            {
                "settler-basic",
                new()
                {
                    Name = "settler-basic",
                    WorkforceType = WorkforceType.SETTLER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var expectedEfficiency = (10m + 2m) / (10m + 5m);
        unitCost.OverallEfficiency.Should().Be(expectedEfficiency);
    }

    [Fact]
    public void CalculateUnitCosts_AdditionalEfficiencyModifier_MultipliesWorkforceEfficiency()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 }
            }
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
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal> { 1.5m }
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];
        unitCost.OverallEfficiency.Should().Be(1.5m);
    }

    [Fact]
    public void CalculateUnitCosts_ReducedWorkforceWithAdditionalModifier_MultipliesBoth()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 }
            }
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
            WorkforceOverride = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 5 }
            },
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal> { 2.0m }
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var expectedEfficiency = 0.5m * 2.0m;
        unitCost.OverallEfficiency.Should().Be(expectedEfficiency);
    }

    [Fact]
    public void CalculateUnitCosts_MultipleEfficiencyModifiers_MultipliesAllTogether()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 10 }
            }
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
            WorkforceOverride = new List<WorkforceRequirement>
            {
                new() { WorkforceType = WorkforceType.PIONEER, Count = 8 }
            },
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal> { 1.5m, 1.2m, 0.9m }
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>
        {
            {
                "pioneer-basic",
                new()
                {
                    Name = "pioneer-basic",
                    WorkforceType = WorkforceType.PIONEER,
                    MaterialConsumption = new List<WorkforceMaterialConsumption>()
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        var workforceEfficiency = 8m / 10m;
        var expectedEfficiency = workforceEfficiency * 1.5m * 1.2m * 0.9m;
        unitCost.OverallEfficiency.Should().Be(expectedEfficiency);
    }
}
