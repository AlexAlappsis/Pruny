namespace Pruny.Library.Events;

public class WorkspaceModifiedEventArgs : EventArgs
{
    public required string WorkspaceId { get; init; }
    public required string Reason { get; init; }
}