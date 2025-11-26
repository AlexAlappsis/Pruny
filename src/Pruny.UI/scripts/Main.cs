using Godot;

namespace Pruny.UI;

public partial class Main : Node
{
    private SessionManager? _sessionManager;

    public override void _Ready()
    {
        GD.Print("Pruny UI initialized successfully!");
        GD.Print($"Godot version: {Engine.GetVersionInfo()["string"]}");

        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        if (_sessionManager?.Session != null)
        {
            GD.Print("SessionManager accessed successfully!");
            GD.Print($"Session initialized: {_sessionManager.Session.IsInitialized}");
            GD.Print($"Session calculating: {_sessionManager.Session.IsCalculating}");

            _sessionManager.SessionStateChangedSignal += OnSessionStateChanged;
        }
        else
        {
            GD.PrintErr("Failed to access SessionManager or Session is null");
        }
    }

    private void OnSessionStateChanged(bool isInitialized, bool isCalculating, string message)
    {
        GD.Print($"Main: Session state changed - {message}");
    }

    public override void _ExitTree()
    {
        if (_sessionManager != null)
        {
            _sessionManager.SessionStateChangedSignal -= OnSessionStateChanged;
        }
    }
}
