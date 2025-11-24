namespace Pruny.Core.Models;

public class UnitCost
{
    public required string MaterialId { get; init; }
    public required string ProductionLineId { get; init; }
    public required decimal CostPerUnit { get; init; }
    public required decimal WorkforceCost { get; init; }
    public required decimal InputCosts { get; init; }
    public required decimal OverallEfficiency { get; init; }
    public decimal? OutputPrice { get; init; }
    public decimal? ProfitPerUnit { get; init; }
    public decimal? ProfitPerRun { get; init; }
    public decimal? ProfitPer24Hours { get; init; }
}
