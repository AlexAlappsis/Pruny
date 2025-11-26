using Godot;

namespace Pruny.UI;

public partial class Header : HBoxContainer
{
    private Label? _workspaceNameLabel;
    private Label? _sessionStateLabel;
    private Label? _dirtyIndicator;

    public override void _Ready()
    {
        _workspaceNameLabel = GetNode<Label>("WorkspaceNameLabel");
        _sessionStateLabel = GetNode<Label>("SessionStateLabel");
        _dirtyIndicator = GetNode<Label>("DirtyIndicator");

        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager != null)
        {
            sessionManager.SessionStateChangedSignal += OnSessionStateChanged;
            sessionManager.WorkspaceModifiedSignal += OnWorkspaceModified;
        }

        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager != null)
        {
            sessionManager.SessionStateChangedSignal -= OnSessionStateChanged;
            sessionManager.WorkspaceModifiedSignal -= OnWorkspaceModified;
        }
    }

    private void OnSessionStateChanged(bool isInitialized, bool isCalculating, string message)
    {
        UpdateDisplay();
    }

    private void OnWorkspaceModified(string reason)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.Session == null)
            return;

        var workspace = sessionManager.Session.CurrentWorkspace;
        if (_workspaceNameLabel != null)
        {
            _workspaceNameLabel.Text = workspace != null ? workspace.Name : "No Workspace";
        }

        if (_sessionStateLabel != null)
        {
            var state = sessionManager.Session.IsCalculating ? "Calculating..." :
                       sessionManager.Session.IsInitialized ? "Ready" : "Loading...";
            _sessionStateLabel.Text = state;
        }

        if (_dirtyIndicator != null)
        {
            _dirtyIndicator.Visible = sessionManager.Session.IsDirty;
        }
    }
}
