namespace Pruny.Library.Events;

public class MarketDataRequestedEventArgs : EventArgs
{
    public DateTimeOffset? Timestamp { get; init; }
    public required string WorkspaceId { get; init; }
}
