namespace Pruny.Library.Events;

public class PricesUpdatedEventArgs : EventArgs
{
    public required int MaterialCount { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Source { get; init; }
}