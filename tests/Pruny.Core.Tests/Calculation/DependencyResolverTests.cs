using FluentAssertions;
using Pruny.Core.Calculation;
using Pruny.Core.Models;

namespace Pruny.Core.Tests.Calculation;

public class DependencyResolverTests
{
    [Fact]
    public void ResolveDependencyOrder_NoDependencies_ReturnsAllLines()
    {
        var resolver = new DependencyResolver();

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line2 = new ProductionLine
        {
            Id = "LINE2",
            RecipeId = "RCP2",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe2 = new Recipe
        {
            Id = "RCP2",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "MAT2", Quantity = 1 } },
            DurationMinutes = 60
        };

        var result = resolver.ResolveDependencyOrder(
            new List<ProductionLine> { line1, line2 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 }, { "RCP2", recipe2 } }
        );

        result.Should().HaveCount(2);
        result.Should().Contain("LINE1");
        result.Should().Contain("LINE2");
    }

    [Fact]
    public void ResolveDependencyOrder_LinearDependency_ReturnsCorrectOrder()
    {
        var resolver = new DependencyResolver();

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe2 = new Recipe
        {
            Id = "RCP2",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "MAT1", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "MAT2", Quantity = 1 } },
            DurationMinutes = 60
        };

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line2 = new ProductionLine
        {
            Id = "LINE2",
            RecipeId = "RCP2",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "MAT1", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE1", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var result = resolver.ResolveDependencyOrder(
            new List<ProductionLine> { line2, line1 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 }, { "RCP2", recipe2 } }
        );

        result.Should().HaveCount(2);
        result[0].Should().Be("LINE1");
        result[1].Should().Be("LINE2");
    }

    [Fact]
    public void ResolveDependencyOrder_ComplexDependencyGraph_ReturnsCorrectOrder()
    {
        var resolver = new DependencyResolver();

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe2 = new Recipe
        {
            Id = "RCP2",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem>(),
            Outputs = new List<RecipeItem> { new() { MaterialId = "B", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe3 = new Recipe
        {
            Id = "RCP3",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 1 }, new() { MaterialId = "B", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "C", Quantity = 1 } },
            DurationMinutes = 60
        };

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line2 = new ProductionLine
        {
            Id = "LINE2",
            RecipeId = "RCP2",
            InputPriceSources = new Dictionary<string, PriceSource>(),
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line3 = new ProductionLine
        {
            Id = "LINE3",
            RecipeId = "RCP3",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "A", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE1", Adjustments = new List<Adjustment>() } },
                { "B", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE2", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var result = resolver.ResolveDependencyOrder(
            new List<ProductionLine> { line3, line2, line1 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 }, { "RCP2", recipe2 }, { "RCP3", recipe3 } }
        );

        result.Should().HaveCount(3);
        result.IndexOf("LINE1").Should().BeLessThan(result.IndexOf("LINE3"));
        result.IndexOf("LINE2").Should().BeLessThan(result.IndexOf("LINE3"));
    }

    [Fact]
    public void ResolveDependencyOrder_CircularDependency_ThrowsException()
    {
        var resolver = new DependencyResolver();

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "B", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 1 } },
            DurationMinutes = 60
        };

        var recipe2 = new Recipe
        {
            Id = "RCP2",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "B", Quantity = 1 } },
            DurationMinutes = 60
        };

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "B", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE2", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var line2 = new ProductionLine
        {
            Id = "LINE2",
            RecipeId = "RCP2",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "A", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE1", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var act = () => resolver.ResolveDependencyOrder(
            new List<ProductionLine> { line1, line2 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 }, { "RCP2", recipe2 } }
        );

        act.Should().Throw<CircularDependencyException>()
            .Which.DependencyCycle.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencyOrder_SelfReference_ThrowsException()
    {
        var resolver = new DependencyResolver();

        var recipe1 = new Recipe
        {
            Id = "RCP1",
            BuildingId = "BLD1",
            Inputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 1 } },
            Outputs = new List<RecipeItem> { new() { MaterialId = "A", Quantity = 2 } },
            DurationMinutes = 60
        };

        var line1 = new ProductionLine
        {
            Id = "LINE1",
            RecipeId = "RCP1",
            InputPriceSources = new Dictionary<string, PriceSource>
            {
                { "A", new PriceSource { Type = PriceSourceType.ProductionLine, SourceIdentifier = "LINE1", Adjustments = new List<Adjustment>() } }
            },
            OutputPriceSources = new Dictionary<string, PriceSource>(),
            AdditionalEfficiencyModifiers = new List<decimal>()
        };

        var act = () => resolver.ResolveDependencyOrder(
            new List<ProductionLine> { line1 },
            new Dictionary<string, Recipe> { { "RCP1", recipe1 } }
        );

        act.Should().Throw<CircularDependencyException>();
    }
}
