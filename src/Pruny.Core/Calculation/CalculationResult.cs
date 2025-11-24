namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public class CalculationResult
{
    public Dictionary<string, UnitCost> UnitCosts { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public bool IsSuccess => Errors.Count == 0;
}
