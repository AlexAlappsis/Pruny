namespace Pruny.Core.Models;

public class ProductionLine
{
    public required string Id { get; init; }
    public required string RecipeId { get; init; }
    public List<WorkforceRequirement>? WorkforceOverride { get; init; }
    public List<RecipeItem>? OutputOverrides { get; init; }
    public required Dictionary<string, PriceSource> InputPriceSources { get; init; }
    public required Dictionary<string, PriceSource> OutputPriceSources { get; init; }
    public List<decimal> AdditionalEfficiencyModifiers { get; init; } = new();
}
