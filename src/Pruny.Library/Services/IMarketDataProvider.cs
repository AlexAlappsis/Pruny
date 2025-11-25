namespace Pruny.Library.Services;

using Pruny.Library.Models;

public interface IMarketDataProvider
{
    Task<MarketPriceData> FetchMarketPricesAsync(CancellationToken cancellationToken = default);
}