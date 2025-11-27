namespace Pruny.Library.Services;

using Pruny.Library.Models;

public class MarketDataAnalyzer
{
    private readonly MarketPriceData? _marketData;

    public MarketDataAnalyzer(MarketPriceData? marketData)
    {
        _marketData = marketData;
    }

    public List<string> GetAvailableExchangeCodes()
    {
        if (_marketData == null || _marketData.Prices.Count == 0)
            return new List<string>();

        return _marketData.Prices
            .Select(p => p.ExchangeCode)
            .Distinct()
            .OrderBy(code => code)
            .ToList();
    }

    public List<string> GetAvailableMaterialTickers()
    {
        if (_marketData == null || _marketData.Prices.Count == 0)
            return new List<string>();

        return _marketData.Prices
            .Select(p => p.Ticker)
            .Distinct()
            .OrderBy(ticker => ticker)
            .ToList();
    }

    public bool HasPriceData(string materialId, string exchangeCode)
    {
        if (_marketData == null)
            return false;

        return _marketData.Prices.Any(p =>
            p.Ticker == materialId &&
            p.ExchangeCode == exchangeCode);
    }
}
