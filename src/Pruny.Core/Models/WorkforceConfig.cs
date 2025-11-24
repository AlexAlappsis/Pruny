namespace Pruny.Core.Models;

public class WorkforceConfig
{
    public required string Id { get; init; }
    public required List<WorkforceTypeConfig> WorkforceTypes { get; init; }
}
