namespace Pruny.Library.Models;

using Pruny.Core.Models;

public class Workspace
{
    public required int Version { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastModifiedAt { get; init; }
    public List<ProductionLine> ProductionLines { get; init; } = new();
    public Dictionary<string, decimal> CustomPrices { get; init; } = new();
    public WorkforceConfig? WorkforceConfig { get; init; }
}