namespace Pruny.Library.Models;

using Pruny.Core.Models;

public class GameData
{
    public required Dictionary<string, Material> Materials { get; init; }
    public required Dictionary<string, Recipe> Recipes { get; init; }
    public required Dictionary<string, Building> Buildings { get; init; }
    public string? Version { get; init; }
    public DateTimeOffset? LoadedAt { get; init; }
}