namespace Pruny.Core.Models;

public class WorkforceRequirement
{
    public required WorkforceType WorkforceType { get; init; }
    public required int Count { get; init; }
}
