namespace Pruny.Core.Models;

public class WorkforceMaterialConsumption
{
    public required string MaterialId { get; init; }
    public required decimal QuantityPer100WorkersPer24Hours { get; init; }
    public required PriceSource PriceSource { get; init; }
}
