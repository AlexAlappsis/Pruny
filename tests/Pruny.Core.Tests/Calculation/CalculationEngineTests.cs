using FluentAssertions;
using Pruny.Core.Calculation;
using Pruny.Core.Models;

namespace Pruny.Core.Tests.Calculation;

public class CalculationEngineTests
{
    [Fact]
    public void CalculateUnitCosts_SimpleRecipe_CalculatesCorrectly()
    {
        var engine = new CalculationEngine();

        var material = new Material { Id = "MAT1", Name = "Material 1" };
        var outputMaterial = new Material { Id = "OUT1", Name = "Output 1" };

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
                {
                    "MAT1",
                    new PriceSource
                    {
                        Type = PriceSourceType.Custom,
                        SourceIdentifier = "custom-MAT1",
                        Adjustments = new List<Adjustment>()
                    }
                }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                {
                    "OUT1",
                    new PriceSource
                    {
                        Type = PriceSourceType.Custom,
                        SourceIdentifier = "custom-OUT1",
                        Adjustments = new List<Adjustment>()
                    }
                }
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
                    MaterialConsumption = new List<WorkforceMaterialConsumption>
                    {
                        new()
                        {
                            MaterialId = "FOOD",
                            QuantityPer100WorkersPer24Hours = 100,
                            PriceSource = new PriceSource
                            {
                                Type = PriceSourceType.Custom,
                                SourceIdentifier = "custom-FOOD",
                                Adjustments = new List<Adjustment>()
                            }
                        }
                    }
                }
            }
        };

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("MAT1", "custom-MAT1", 100m);
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 300m);
        priceRegistry.RegisterPrice("FOOD", "custom-FOOD", 10m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        result.UnitCosts.Should().ContainKey("OUT1");

        var unitCost = result.UnitCosts["OUT1"];
        unitCost.MaterialId.Should().Be("OUT1");
        unitCost.ProductionLineId.Should().Be("LINE1");
        unitCost.OverallEfficiency.Should().Be(1.0m);

        var expectedInputCost = 10m * 100m;
        var expectedWorkforceCost = 10 * (100m * 10m / 100m) / (24m * 60m) * 60m;
        var expectedTotalCost = expectedInputCost + expectedWorkforceCost;
        var expectedCostPerUnit = expectedTotalCost / 5m;

        unitCost.CostPerUnit.Should().BeApproximately(expectedCostPerUnit, 0.01m);
        unitCost.InputCosts.Should().BeApproximately(expectedInputCost / 5m, 0.01m);
        unitCost.WorkforceCost.Should().BeApproximately(expectedWorkforceCost / 5m, 0.01m);
    }

    [Fact]
    public void CalculateUnitCosts_WithEfficiencyModifiers_AdjustsDuration()
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
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-MAT1", Adjustments = new List<Adjustment>() } }
            },
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
        priceRegistry.RegisterPrice("MAT1", "custom-MAT1", 100m);
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
        unitCost.OverallEfficiency.Should().Be(2.0m);
    }

    [Fact]
    public void CalculateUnitCosts_WithReducedWorkforce_ReducesEfficiency()
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
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 5 } },
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
    public void CalculateUnitCosts_WithOrderedAdjustments_AppliesInSequence()
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
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "OUT1", Quantity = 1 } },
            DurationMinutes = 60
        };

        var productionLine = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                {
                    "MAT1",
                    new PriceSource
                    {
                        Type = PriceSourceType.Custom,
                        SourceIdentifier = "custom-MAT1",
                        Adjustments = new List<Adjustment>
                        {
                            new() { Type = AdjustmentType.Percentage, Value = 1.5m },
                            new() { Type = AdjustmentType.Flat, Value = 10m }
                        }
                    }
                }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>();

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("MAT1", "custom-MAT1", 100m);
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

        var expectedPrice = (100m * 1.5m) + 10m;
        unitCost.CostPerUnit.Should().Be(expectedPrice);
    }

    [Fact]
    public void CalculateUnitCosts_WithProfitCalculation_ComputesCorrectly()
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
                { "MAT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-MAT1", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>();

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("MAT1", "custom-MAT1", 100m);
        priceRegistry.RegisterPrice("OUT1", "custom-OUT1", 300m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { productionLine },
            new Dictionary<string, Recipe> { { "RCP1", recipe } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        var unitCost = result.UnitCosts["OUT1"];

        unitCost.OutputPrice.Should().Be(300m);
        unitCost.ProfitPerUnit.Should().Be(300m - 200m);
        unitCost.ProfitPerRun.Should().Be((300m - 200m) * 5m);

        var runsPerDay = 24m * 60m / 60m;
        unitCost.ProfitPer24Hours.Should().Be((300m - 200m) * 5m * runsPerDay);
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
                { "OUT1", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-OUT1", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>();

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

        unitCost.OutputPrice.Should().BeNull();
        unitCost.ProfitPerUnit.Should().BeNull();
        unitCost.ProfitPerRun.Should().BeNull();
        unitCost.ProfitPer24Hours.Should().BeNull();
    }

    [Fact]
    public void CalculateUnitCosts_WithDependentProductionLines_ResolvesInOrder()
    {
        var engine = new CalculationEngine();

        var building = new Building
        {
            Id = "BLD1",
            Name = "Building 1",
            DefaultWorkforce = new List<WorkforceRequirement>()
        };

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "BASE", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "INTERMEDIATE", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe2 = new Recipe
        {
            Id = "RCP2",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "INTERMEDIATE", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "FINAL", Quantity = 1 } },
            DurationMinutes = 60
        };

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "BASE", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-BASE", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "INTERMEDIATE", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-INTERMEDIATE", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line2 = new ProductionLine
        {
            Id = "LINE2",
            RecipeId = "RCP2",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "INTERMEDIATE", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE1", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>
            {
                { "FINAL", new PriceSource { Type = PriceSourceType.Custom, SourceIdentifier = "custom-FINAL", Adjustments = new List<Adjustment>() } }
            },
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>();

        var priceRegistry = new PriceSourceRegistry();
        priceRegistry.RegisterPrice("BASE", "custom-BASE", 100m);
        priceRegistry.RegisterPrice("INTERMEDIATE", "custom-INTERMEDIATE", 0m);
        priceRegistry.RegisterPrice("FINAL", "custom-FINAL", 0m);

        var result = engine.CalculateUnitCosts(
            new List<ProductionLine> { line2, line1 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 }, { "RCP2", recipe2 } },
            new Dictionary<string, Building> { { "BLD1", building } },
            workforceConfigs,
            priceRegistry
        );

        result.IsSuccess.Should().BeTrue();
        result.UnitCosts["INTERMEDIATE"].CostPerUnit.Should().Be(100m);
        result.UnitCosts["FINAL"].CostPerUnit.Should().Be(100m);
    }
}
