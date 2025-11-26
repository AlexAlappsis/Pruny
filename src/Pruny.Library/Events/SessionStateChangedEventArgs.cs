namespace Pruny.Library.Events;

public class SessionStateChangedEventArgs : EventArgs
{
    public required bool IsInitialized { get; init; }
    public required bool IsCalculating { get; init; }
    public string? Message { get; init; }
}
