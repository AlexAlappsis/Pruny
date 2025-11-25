namespace Pruny.Library.Events;

public class CalculationErrorEventArgs : EventArgs
{
    public required string ErrorMessage { get; init; }
    public string? ProductionLineId { get; init; }
    public Exception? Exception { get; init; }
}