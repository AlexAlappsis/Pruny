namespace Pruny.Library.Services;

using Pruny.Core.Models;
using Pruny.Library.Models;

public class PriceSourceBuilder
{
    public PriceSourceRegistry BuildRegistry(
        MarketPriceData marketData,
        Workspace workspace)
    {
        var registry = new PriceSourceRegistry();

        RegisterApiPrices(registry, marketData);
        RegisterCustomPrices(registry, workspace);

        return registry;
    }

    private void RegisterApiPrices(PriceSourceRegistry registry, MarketPriceData marketData)
    {
        foreach (var price in marketData.Prices)
        {
            if (price.Average.HasValue)
                registry.RegisterPrice(price.Ticker, $"{price.ExchangeCode}-AVG", price.Average.Value);

            if (price.Ask.HasValue)
                registry.RegisterPrice(price.Ticker, $"{price.ExchangeCode}-ASK", price.Ask.Value);

            if (price.Bid.HasValue)
                registry.RegisterPrice(price.Ticker, $"{price.ExchangeCode}-BID", price.Bid.Value);
        }
    }

    private void RegisterCustomPrices(PriceSourceRegistry registry, Workspace workspace)
    {
        foreach (var (materialId, sources) in workspace.CustomPrices)
        {
            foreach (var (sourceId, price) in sources)
            {
                registry.RegisterPrice(materialId, sourceId, price);
            }
        }
    }
}
