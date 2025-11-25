namespace Pruny.Library.Models;

public class MarketPrice
{
    public required string Ticker { get; init; }
    public required string ExchangeCode { get; init; }
    public decimal? Ask { get; init; }
    public decimal? Bid { get; init; }
    public decimal? Average { get; init; }
    public int? Supply { get; init; }
    public int? Demand { get; init; }
    public int? Traded { get; init; }
}