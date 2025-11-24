namespace Pruny.Core.Models;

public class Recipe
{
    public required string Id { get; init; }
    public required string BuildingId { get; init; }
    public required List<RecipeItem> Inputs { get; init; }
    public required List<RecipeItem> Outputs { get; init; }
    public required decimal DurationMinutes { get; init; }
}
