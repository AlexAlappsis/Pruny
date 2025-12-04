using Godot;
using Pruny.Core.Models;

namespace Pruny.UI;

public partial class WorkforceConfigManager : CenterContainer
{
    private MainUI? _mainUI;
    private SessionManager? _sessionManager;

    private VBoxContainer? _mainContainer;
    private Label? _titleLabel;
    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _workforceTypesContainer;
    private Button? _addWorkforceTypeButton;
    private HBoxContainer? _buttonContainer;
    private Button? _saveButton;
    private Button? _cancelButton;
    private Label? _statusLabel;

    private List<Components.WorkforceTypeEditor> _workforceTypeEditors = new();

    public override void _Ready()
    {
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        if (_mainUI == null)
        {
            GD.PrintErr("WorkforceConfigManager: Could not find MainUI in parent chain");
        }

        if (_sessionManager?.Session == null)
        {
            GD.PrintErr("WorkforceConfigManager: SessionManager or Session not available");
            return;
        }

        SetupUI();
        LoadWorkforceConfig();
    }

    private void SetupUI()
    {
        _mainContainer = GetNode<VBoxContainer>("MainContainer");
        _titleLabel = GetNode<Label>("MainContainer/TitleLabel");
        _scrollContainer = GetNode<ScrollContainer>("MainContainer/ScrollContainer");
        _workforceTypesContainer = GetNode<VBoxContainer>("MainContainer/ScrollContainer/WorkforceTypesContainer");
        _addWorkforceTypeButton = GetNode<Button>("MainContainer/AddWorkforceTypeButton");
        _buttonContainer = GetNode<HBoxContainer>("MainContainer/ButtonContainer");
        _saveButton = GetNode<Button>("MainContainer/ButtonContainer/SaveButton");
        _cancelButton = GetNode<Button>("MainContainer/ButtonContainer/CancelButton");
        _statusLabel = GetNode<Label>("MainContainer/StatusLabel");

        _addWorkforceTypeButton.Pressed += OnAddWorkforceTypePressed;
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;
    }

    private void LoadWorkforceConfig()
    {
        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        ClearWorkforceTypes();

        var workforceConfigs = _sessionManager.Session.CurrentWorkspace.WorkforceConfigs;

        if (workforceConfigs == null || workforceConfigs.Count == 0)
        {
            SetStatus("No workforce configuration found. Add workforce types to get started.", new Color(1, 1, 0.3f));
            return;
        }

        foreach (var workforceTypeConfig in workforceConfigs.Values)
        {
            var editor = CreateWorkforceTypeEditor();
            editor.SetWorkforceTypeConfig(workforceTypeConfig);
            _workforceTypeEditors.Add(editor);
            _workforceTypesContainer?.AddChild(editor);
        }

        SetStatus("");
    }

    private void OnAddWorkforceTypePressed()
    {
        var editor = CreateWorkforceTypeEditor();
        _workforceTypeEditors.Add(editor);
        _workforceTypesContainer?.AddChild(editor);
    }

    private Components.WorkforceTypeEditor CreateWorkforceTypeEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/WorkforceTypeEditor.tscn");
        var editor = scene.Instantiate<Components.WorkforceTypeEditor>();

        editor.DeleteRequested += () =>
        {
            _workforceTypeEditors.Remove(editor);
            editor.QueueFree();
        };

        return editor;
    }

    private void ClearWorkforceTypes()
    {
        foreach (var editor in _workforceTypeEditors)
            editor.QueueFree();
        _workforceTypeEditors.Clear();
    }

    private void OnSavePressed()
    {
        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        if (!ValidateWorkforceConfig())
        {
            return;
        }

        try
        {
            var workforceConfigs = new Dictionary<string, WorkforceTypeConfig>();
            foreach (var editor in _workforceTypeEditors)
            {
                var config = editor.GetWorkforceTypeConfig();
                workforceConfigs[config.Name] = config;
            }

            _sessionManager.Session.WorkspaceManager.ApplyChanges(
                ws => ws.WorkforceConfigs = workforceConfigs,
                "Workforce configuration updated");

            _sessionManager.Session.RecalculateAll();

            SetStatus("Workforce configuration applied successfully!", new Color(0.3f, 1, 0.3f));
            GD.Print("WorkforceConfigManager: Configuration applied and recalculated");

            GetTree().CreateTimer(1.5).Timeout += () => _mainUI?.LoadDashboard();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WorkforceConfigManager: Failed to apply - {ex.Message}");
            SetStatus($"Failed to apply configuration: {ex.Message}", new Color(1, 0.3f, 0.3f));
            Dialogs.ErrorDialog.Show(this, "Failed to Apply Configuration",
                "An error occurred while applying the workforce configuration.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnCancelPressed()
    {
        _mainUI?.LoadDashboard();
    }

    private bool ValidateWorkforceConfig()
    {
        if (_workforceTypeEditors.Count == 0)
        {
            SetStatus("Add at least one workforce configuration", new Color(1, 0.3f, 0.3f));
            return false;
        }

        var workforceConfigs = _workforceTypeEditors.Select(e => e.GetWorkforceTypeConfig()).ToList();

        foreach (var config in workforceConfigs)
        {
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                SetStatus("All workforce configurations must have a name", new Color(1, 0.3f, 0.3f));
                return false;
            }

            if (config.MaterialConsumption.Count == 0)
            {
                SetStatus($"Workforce config '{config.Name}' must have at least one material", new Color(1, 0.3f, 0.3f));
                return false;
            }
        }

        var duplicateNames = workforceConfigs
            .GroupBy(wt => wt.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            SetStatus($"Duplicate configuration name: {duplicateNames.First()}", new Color(1, 0.3f, 0.3f));
            return false;
        }

        return true;
    }

    private void SetStatus(string message, Color? color = null)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.AddThemeColorOverride("font_color", color ?? new Color(1, 1, 1));
        }
    }

    public override void _ExitTree()
    {
        if (_addWorkforceTypeButton != null)
            _addWorkforceTypeButton.Pressed -= OnAddWorkforceTypePressed;
        if (_saveButton != null)
            _saveButton.Pressed -= OnSavePressed;
        if (_cancelButton != null)
            _cancelButton.Pressed -= OnCancelPressed;
    }
}
