namespace Pruny.Core.Models;

public class WorkforceTypeConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required WorkforceType WorkforceType { get; init; }
    public required List<WorkforceMaterialConsumption> MaterialConsumption { get; init; }
}
