using Godot;
using Pruny.Core.Models;

namespace Pruny.UI;

public partial class ProductionLineManager : CenterContainer
{
    private MainUI? _mainUI;
    private SessionManager? _sessionManager;

    private VBoxContainer? _mainContainer;
    private Label? _titleLabel;
    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _productionLinesContainer;
    private Button? _addProductionLineButton;
    private HBoxContainer? _buttonContainer;
    private Button? _saveButton;
    private Button? _cancelButton;
    private Label? _statusLabel;

    private List<Components.ProductionLineEditor> _productionLineEditors = new();

    public override void _Ready()
    {
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        if (_mainUI == null)
        {
            GD.PrintErr("ProductionLineManager: Could not find MainUI in parent chain");
        }

        if (_sessionManager?.Session == null)
        {
            GD.PrintErr("ProductionLineManager: SessionManager or Session not available");
            return;
        }

        SetupUI();
        LoadProductionLines();
    }

    private void SetupUI()
    {
        _mainContainer = GetNode<VBoxContainer>("MainContainer");
        _titleLabel = GetNode<Label>("MainContainer/TitleLabel");
        _scrollContainer = GetNode<ScrollContainer>("MainContainer/ScrollContainer");
        _productionLinesContainer = GetNode<VBoxContainer>("MainContainer/ScrollContainer/ProductionLinesContainer");
        _addProductionLineButton = GetNode<Button>("MainContainer/AddProductionLineButton");
        _buttonContainer = GetNode<HBoxContainer>("MainContainer/ButtonContainer");
        _saveButton = GetNode<Button>("MainContainer/ButtonContainer/SaveButton");
        _cancelButton = GetNode<Button>("MainContainer/ButtonContainer/CancelButton");
        _statusLabel = GetNode<Label>("MainContainer/StatusLabel");

        _addProductionLineButton.Pressed += OnAddProductionLinePressed;
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;
    }

    private void LoadProductionLines()
    {
        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        ClearProductionLines();

        var productionLines = _sessionManager.Session.CurrentWorkspace.ProductionLines;

        if (productionLines.Count == 0)
        {
            SetStatus("No production lines found. Add production lines to get started.", new Color(1, 1, 0.3f));
            return;
        }

        foreach (var line in productionLines)
        {
            var editor = CreateProductionLineEditor();
            editor.SetProductionLine(line);
            _productionLineEditors.Add(editor);
            _productionLinesContainer?.AddChild(editor);
        }

        SetStatus("");
    }

    private void OnAddProductionLinePressed()
    {
        var editor = CreateProductionLineEditor();
        _productionLineEditors.Add(editor);
        _productionLinesContainer?.AddChild(editor);
    }

    private Components.ProductionLineEditor CreateProductionLineEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/ProductionLineEditor.tscn");
        var editor = scene.Instantiate<Components.ProductionLineEditor>();

        editor.DeleteRequested += () =>
        {
            _productionLineEditors.Remove(editor);
            editor.QueueFree();
        };

        return editor;
    }

    private void ClearProductionLines()
    {
        foreach (var editor in _productionLineEditors)
            editor.QueueFree();
        _productionLineEditors.Clear();
    }

    private void OnSavePressed()
    {
        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        if (!ValidateProductionLines())
        {
            return;
        }

        try
        {
            var productionLines = _productionLineEditors.Select(e => e.GetProductionLine()).ToList();

            _sessionManager.Session.WorkspaceManager.ApplyChanges(
                ws => ws.ProductionLines = productionLines,
                "Production lines updated");

            _sessionManager.Session.RecalculateAll();

            SetStatus("Production lines applied successfully!", new Color(0.3f, 1, 0.3f));
            GD.Print("ProductionLineManager: Configuration applied and recalculated");

            GetTree().CreateTimer(1.5).Timeout += () => _mainUI?.LoadDashboard();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ProductionLineManager: Failed to apply - {ex.Message}");
            SetStatus($"Failed to apply configuration: {ex.Message}", new Color(1, 0.3f, 0.3f));
            Dialogs.ErrorDialog.Show(this, "Failed to Apply Configuration",
                "An error occurred while applying the production lines.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnCancelPressed()
    {
        _mainUI?.LoadDashboard();
    }

    private bool ValidateProductionLines()
    {
        if (_productionLineEditors.Count == 0)
        {
            SetStatus("Add at least one production line", new Color(1, 0.3f, 0.3f));
            return false;
        }

        var productionLines = _productionLineEditors.Select(e => e.GetProductionLine()).ToList();

        foreach (var line in productionLines)
        {
            if (string.IsNullOrWhiteSpace(line.RecipeId))
            {
                SetStatus("All production lines must have a recipe selected", new Color(1, 0.3f, 0.3f));
                return false;
            }
        }

        var duplicateIds = productionLines
            .GroupBy(pl => pl.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Any())
        {
            SetStatus($"Duplicate production line ID: {duplicateIds.First()}", new Color(1, 0.3f, 0.3f));
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
        if (_addProductionLineButton != null)
            _addProductionLineButton.Pressed -= OnAddProductionLinePressed;
        if (_saveButton != null)
            _saveButton.Pressed -= OnSavePressed;
        if (_cancelButton != null)
            _cancelButton.Pressed -= OnCancelPressed;
    }
}
