namespace Pruny.Core.Models;

public class ProductionLineGroup
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public List<string> ProductionLineIds { get; set; } = new();
}
