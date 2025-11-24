namespace Pruny.Core.Models;

public class WorkforceConfig
{
    public required string Id { get; init; }
    public required Dictionary<string, decimal> CostPerWorkerTypePerMinute { get; init; }
}
