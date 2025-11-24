namespace Pruny.Core.Models;

public class Building
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<WorkforceRequirement> DefaultWorkforce { get; init; }
}
