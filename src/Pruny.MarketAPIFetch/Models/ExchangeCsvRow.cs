using CsvHelper.Configuration.Attributes;

namespace Pruny.MarketAPIFetch.Models;

internal class ExchangeCsvRow
{
    [Name("TICKER")]
    public required string Ticker { get; init; }

    [Name("EXCHANGECODE")]
    public required string ExchangeCode { get; init; }

    [Name("ASK")]
    public decimal? Ask { get; init; }

    [Name("BID")]
    public decimal? Bid { get; init; }

    [Name("AVG")]
    public decimal? Avg { get; init; }

    [Name("SUPPLY")]
    public int? Supply { get; init; }

    [Name("DEMAND")]
    public int? Demand { get; init; }

    [Name("TRADED")]
    public int? Traded { get; init; }
}
