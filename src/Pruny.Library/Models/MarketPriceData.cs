namespace Pruny.Library.Models;

public class MarketPriceData
{
    public required List<MarketPrice> Prices { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
    public string? Source { get; init; }
}