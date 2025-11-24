namespace Pruny.Core.Models;

public class PriceSource
{
    public required PriceSourceType Type { get; init; }
    public string? ProductionLineId { get; init; }
    public decimal? CustomValue { get; init; }
    public decimal? ApiPrice { get; init; }
    public List<Adjustment> Adjustments { get; init; } = new();
}

public enum PriceSourceType
{
    Api,
    ProductionLine,
    Custom
}
