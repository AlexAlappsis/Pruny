using FluentAssertions;
using Pruny.Core.Models;
using Pruny.Library.Models;
using Pruny.Library.Services;

namespace Pruny.Library.Tests;

public class PriceSourceBuilderTests
{
    [Fact]
    public void BuildRegistry_registers_api_prices_for_all_available_fields()
    {
        var marketData = new MarketPriceData
        {
            FetchedAt = DateTimeOffset.UtcNow,
            Prices = new List<MarketPrice>
            {
                new() { Ticker = "FE", ExchangeCode = "IC1", Average = 100m, Ask = 110m, Bid = 90m }
            }
        };
        var workspace = CreateWorkspace();
        var builder = new PriceSourceBuilder();

        var registry = builder.BuildRegistry(marketData, workspace);

        registry.GetPrice("FE", PriceSource("IC1-AVG")).Should().Be(100m);
        registry.GetPrice("FE", PriceSource("IC1-ASK")).Should().Be(110m);
        registry.GetPrice("FE", PriceSource("IC1-BID")).Should().Be(90m);
    }

    [Fact]
    public void BuildRegistry_registers_custom_prices_from_workspace()
    {
        var marketData = new MarketPriceData { FetchedAt = DateTimeOffset.UtcNow, Prices = new List<MarketPrice>() };
        var workspace = CreateWorkspace();
        workspace.CustomPrices["H2O"] = new Dictionary<string, decimal> { ["Bulk"] = 42m };
        workspace.CustomPrices["O"] = new Dictionary<string, decimal> { ["Alt"] = 9m };
        var builder = new PriceSourceBuilder();

        var registry = builder.BuildRegistry(marketData, workspace);

        registry.GetPrice("H2O", PriceSource("Bulk")).Should().Be(42m);
        registry.GetPrice("O", PriceSource("Alt")).Should().Be(9m);
    }

    [Fact]
    public void BuildRegistry_handles_partial_api_price_fields()
    {
        var marketData = new MarketPriceData
        {
            FetchedAt = DateTimeOffset.UtcNow,
            Prices = new List<MarketPrice>
            {
                new() { Ticker = "CU", ExchangeCode = "NC1", Ask = 50m, Average = null, Bid = null }
            }
        };
        var workspace = CreateWorkspace();
        var builder = new PriceSourceBuilder();

        var registry = builder.BuildRegistry(marketData, workspace);

        registry.GetPrice("CU", PriceSource("NC1-ASK")).Should().Be(50m);
        registry.GetPrice("CU", PriceSource("NC1-AVG")).Should().Be(0m);
        registry.GetPrice("CU", PriceSource("NC1-BID")).Should().Be(0m);
    }

    private static Workspace CreateWorkspace()
    {
        return new Workspace
        {
            Version = 1,
            Id = "ws",
            Name = "Test Workspace",
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            CustomPrices = new(),
            ProductionLines = new(),
        };
    }

    private static PriceSource PriceSource(string id) =>
        new() { Type = PriceSourceType.Api, SourceIdentifier = id };
}
