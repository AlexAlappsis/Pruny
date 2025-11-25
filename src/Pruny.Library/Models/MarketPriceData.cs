namespace Pruny.Library.Models;

public class MarketPriceData
{
    public required Dictionary<string, decimal> Prices { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
    public string? Source { get; init; }
}