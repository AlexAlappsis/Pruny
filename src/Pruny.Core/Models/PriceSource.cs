namespace Pruny.Core.Models;

public class PriceSource
{
    public required PriceSourceType Type { get; init; }
    public required string SourceIdentifier { get; init; }
    public List<Adjustment> Adjustments { get; init; } = new();
}

public enum PriceSourceType
{
    Api,
    ProductionLine,
    Custom
}
