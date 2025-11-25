namespace Pruny.Library.Events;

public class ProductionLineRecalculatedEventArgs : EventArgs
{
    public required HashSet<string> RecalculatedLineIds { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}