using Godot;

namespace Pruny.UI;

public partial class Header : HBoxContainer
{
    private Label? _workspaceNameLabel;
    private Label? _sessionStateLabel;
    private Label? _dirtyIndicator;
    private Button? _saveButton;

    public override void _Ready()
    {
        _workspaceNameLabel = GetNode<Label>("WorkspaceNameLabel");
        _sessionStateLabel = GetNode<Label>("SessionStateLabel");
        _dirtyIndicator = GetNode<Label>("DirtyIndicator");
        _saveButton = GetNode<Button>("SaveButton");

        if (_saveButton != null)
        {
            _saveButton.Pressed += OnSavePressed;
        }

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

    private void OnSavePressed()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.Session == null || sessionManager.DataManager == null)
        {
            GD.PrintErr("Header: Cannot save - SessionManager or DataManager not available");
            return;
        }

        var workspace = sessionManager.Session.CurrentWorkspace;
        if (workspace == null)
        {
            GD.PrintErr("Header: Cannot save - No workspace loaded");
            return;
        }

        try
        {
            var filename = workspace.Name;
            sessionManager.DataManager.SaveWorkspace(filename);
            GD.Print($"Header: Workspace '{filename}' saved successfully");

            UpdateDisplay();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Header: Failed to save workspace - {ex.Message}");
            var mainUI = GetTree().Root.GetNode<Control>("MainUI");
            if (mainUI != null)
            {
                Dialogs.ErrorDialog.Show(mainUI, "Failed to Save Workspace",
                    "An error occurred while saving the workspace.",
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private void UpdateDisplay()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.Session == null)
            return;

        var workspace = sessionManager.Session.CurrentWorkspace;
        var isDirty = sessionManager.Session.IsDirty;

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
            _dirtyIndicator.Visible = isDirty;
        }

        if (_saveButton != null)
        {
            _saveButton.Disabled = !isDirty || workspace == null;
        }
    }
}
