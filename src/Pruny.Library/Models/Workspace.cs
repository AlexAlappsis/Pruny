namespace Pruny.Library.Models;

using Pruny.Core.Models;

public class Workspace
{
    public required int Version { get; set; } = 1;
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; }
    public DateTimeOffset? MarketDataFetchedAt { get; set; }
    public List<ProductionLine> ProductionLines { get; set; } = new();
    public Dictionary<string, Dictionary<string, decimal>> CustomPrices { get; set; } = new();
    public Dictionary<string, WorkforceTypeConfig> WorkforceConfigs { get; set; } = new();
}
