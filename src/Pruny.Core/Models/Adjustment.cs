namespace Pruny.Core.Models;

public class Adjustment
{
    public AdjustmentType Type { get; init; }
    public decimal Value { get; init; }
}

public enum AdjustmentType
{
    Percentage,
    Flat
}
