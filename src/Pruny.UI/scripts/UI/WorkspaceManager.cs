using Godot;

namespace Pruny.UI;

public partial class WorkspaceManager : CenterContainer
{
    private MainUI? _mainUI;
    private VBoxContainer? _workspaceList;
    private Button? _createButton;
    private Button? _loadButton;
    private Button? _backButton;
    private LineEdit? _newWorkspaceNameInput;

    public override void _Ready()
    {
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;

        if (_mainUI == null)
        {
            GD.PrintErr("WorkspaceManager: Could not find MainUI in parent chain");
        }
        else
        {
            GD.Print("WorkspaceManager: Successfully found MainUI");
        }

        _workspaceList = GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/WorkspaceList");
        _createButton = GetNode<Button>("VBoxContainer/ButtonsContainer/CreateButton");
        _loadButton = GetNode<Button>("VBoxContainer/ButtonsContainer/LoadButton");
        _backButton = GetNode<Button>("VBoxContainer/ButtonsContainer/BackButton");
        _newWorkspaceNameInput = GetNode<LineEdit>("VBoxContainer/NewWorkspaceContainer/NameInput");

        _createButton.Pressed += OnCreatePressed;
        _loadButton.Pressed += OnLoadPressed;
        _backButton.Pressed += OnBackPressed;

        RefreshWorkspaceList();
    }

    private void RefreshWorkspaceList()
    {
        if (_workspaceList == null)
            return;

        foreach (var child in _workspaceList.GetChildren())
        {
            child.QueueFree();
        }

        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.DataManager == null)
        {
            GD.PrintErr("WorkspaceManager: SessionManager or DataManager not available");
            return;
        }

        var workspaces = sessionManager.DataManager.ListWorkspaces();

        if (workspaces.Length == 0)
        {
            var noWorkspacesLabel = new Label();
            noWorkspacesLabel.Text = "No workspaces found. Create one to get started!";
            noWorkspacesLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _workspaceList.AddChild(noWorkspacesLabel);
        }
        else
        {
            foreach (var workspace in workspaces)
            {
                var button = new Button();
                button.Text = workspace.Replace(".workspace.json", "");
                button.CustomMinimumSize = new Vector2(400, 40);
                button.Pressed += () => OnWorkspaceSelected(workspace);
                _workspaceList.AddChild(button);
            }
        }
    }

    private void OnCreatePressed()
    {
        if (_newWorkspaceNameInput == null || string.IsNullOrWhiteSpace(_newWorkspaceNameInput.Text))
        {
            GD.PrintErr("WorkspaceManager: Please enter a workspace name");
            return;
        }

        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.Session == null)
        {
            GD.PrintErr("WorkspaceManager: No session available");
            return;
        }

        try
        {
            var workspaceName = _newWorkspaceNameInput.Text.Trim();
            GD.Print($"WorkspaceManager: Creating workspace '{workspaceName}'");

            sessionManager.Session.CreateNewWorkspace(workspaceName);

            var filename = workspaceName;
            sessionManager.DataManager?.SaveWorkspace(filename);

            _newWorkspaceNameInput.Text = "";
            RefreshWorkspaceList();

            GD.Print($"WorkspaceManager: Workspace '{workspaceName}' created and loaded");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WorkspaceManager: Failed to create workspace - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Failed to Create Workspace",
                "An error occurred while creating the workspace.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnLoadPressed()
    {
        GD.Print("WorkspaceManager: Use the workspace list to select a workspace to load");
    }

    private void OnWorkspaceSelected(string filename)
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.DataManager == null)
        {
            GD.PrintErr("WorkspaceManager: No DataManager available");
            return;
        }

        try
        {
            GD.Print($"WorkspaceManager: Loading workspace '{filename}'");
            sessionManager.DataManager.LoadWorkspace(filename);
            GD.Print($"WorkspaceManager: Workspace loaded successfully");

            _mainUI?.LoadDashboard();
        }
        catch (FileNotFoundException ex)
        {
            GD.PrintErr($"WorkspaceManager: Workspace file not found - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Workspace Not Found",
                $"The workspace file '{filename}' could not be found.",
                ex.Message);
        }
        catch (InvalidDataException ex)
        {
            GD.PrintErr($"WorkspaceManager: Workspace file is corrupt - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Corrupt Workspace File",
                $"The workspace file '{filename}' is corrupted or invalid. You may need to create a new workspace.",
                ex.Message);
            RefreshWorkspaceList();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WorkspaceManager: Failed to load workspace - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Failed to Load Workspace",
                "An unexpected error occurred while loading the workspace.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnBackPressed()
    {
        _mainUI?.LoadDashboard();
    }
}
