using FluentAssertions;
using Pruny.Core.Models;

namespace Pruny.Core.Tests;

public class PriceSourceRegistryTests
{
    [Fact]
    public void GetPrice_returns_registered_price_without_adjustments()
    {
        var registry = new PriceSourceRegistry();
        registry.RegisterPrice("FE", "IC1-AVG", 120m);
        var source = new PriceSource { Type = PriceSourceType.Api, SourceIdentifier = "IC1-AVG" };

        var price = registry.GetPrice("FE", source);

        price.Should().Be(120m);
    }

    [Fact]
    public void GetPrice_applies_percentage_then_flat_adjustments_in_order()
    {
        var registry = new PriceSourceRegistry();
        registry.RegisterPrice("H2O", "IC1-ASK", 10m);
        var source = new PriceSource
        {
            Type = PriceSourceType.Api,
            SourceIdentifier = "IC1-ASK",
            Adjustments = new List<Adjustment>
            {
                new() { Type = AdjustmentType.Percentage, Value = 1.10m },
                new() { Type = AdjustmentType.Flat, Value = 2m }
            }
        };

        var price = registry.GetPrice("H2O", source);

        price.Should().Be(13m);
    }

    [Fact]
    public void GetPrice_returns_zero_when_material_or_source_missing()
    {
        var registry = new PriceSourceRegistry();
        var source = new PriceSource { Type = PriceSourceType.Api, SourceIdentifier = "IC1-AVG" };

        registry.GetPrice("UNKNOWN", source).Should().Be(0m);
    }
}
