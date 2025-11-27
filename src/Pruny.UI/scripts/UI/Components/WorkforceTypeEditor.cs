using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class WorkforceTypeEditor : VBoxContainer
{
    public event Action<WorkforceTypeConfig>? WorkforceTypeChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private HBoxContainer? _headerContainer;
    private Label? _workforceTypeLabel;
    private LineEdit? _workforceTypeInput;
    private Button? _addMaterialButton;
    private Button? _deleteButton;
    private VBoxContainer? _materialsContainer;

    private List<MaterialConsumptionEditor> _materialEditors = new();

    public override void _Ready()
    {
        _headerContainer = GetNode<HBoxContainer>("HeaderContainer");
        _workforceTypeLabel = GetNode<Label>("HeaderContainer/WorkforceTypeLabel");
        _workforceTypeInput = GetNode<LineEdit>("HeaderContainer/WorkforceTypeInput");
        _addMaterialButton = GetNode<Button>("HeaderContainer/AddMaterialButton");
        _deleteButton = GetNode<Button>("HeaderContainer/DeleteButton");
        _materialsContainer = GetNode<VBoxContainer>("MaterialsContainer");

        _workforceTypeInput.TextChanged += OnWorkforceTypeChanged;
        _addMaterialButton.Pressed += OnAddMaterialPressed;
        _deleteButton.Pressed += OnDeletePressed;
    }

    public void SetWorkforceTypeConfig(WorkforceTypeConfig config)
    {
        if (_workforceTypeInput != null)
            _workforceTypeInput.Text = config.WorkforceType;

        ClearMaterials();

        foreach (var consumption in config.MaterialConsumption)
        {
            var editor = CreateMaterialEditor();
            editor.SetConsumption(consumption);
            _materialEditors.Add(editor);
            _materialsContainer?.AddChild(editor);
        }
    }

    public WorkforceTypeConfig GetWorkforceTypeConfig()
    {
        if (_workforceTypeInput == null)
            throw new InvalidOperationException("WorkforceTypeEditor not initialized");

        return new WorkforceTypeConfig
        {
            WorkforceType = _workforceTypeInput.Text,
            MaterialConsumption = _materialEditors.Select(e => e.GetConsumption()).ToList()
        };
    }

    private void OnWorkforceTypeChanged(string newText)
    {
        EmitWorkforceTypeChanged();
    }

    private void OnAddMaterialPressed()
    {
        var editor = CreateMaterialEditor();
        _materialEditors.Add(editor);
        _materialsContainer?.AddChild(editor);
        EmitWorkforceTypeChanged();
    }

    private MaterialConsumptionEditor CreateMaterialEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/MaterialConsumptionEditor.tscn");
        var editor = scene.Instantiate<MaterialConsumptionEditor>();

        editor.ConsumptionChanged += (_) => EmitWorkforceTypeChanged();
        editor.DeleteRequested += () =>
        {
            _materialEditors.Remove(editor);
            editor.QueueFree();
            EmitWorkforceTypeChanged();
        };

        return editor;
    }

    private void ClearMaterials()
    {
        foreach (var editor in _materialEditors)
            editor.QueueFree();
        _materialEditors.Clear();
    }

    private void OnDeletePressed()
    {
        EmitSignal(SignalName.DeleteRequested);
    }

    private void EmitWorkforceTypeChanged()
    {
        WorkforceTypeChanged?.Invoke(GetWorkforceTypeConfig());
    }

    public override void _ExitTree()
    {
        if (_workforceTypeInput != null)
            _workforceTypeInput.TextChanged -= OnWorkforceTypeChanged;
        if (_addMaterialButton != null)
            _addMaterialButton.Pressed -= OnAddMaterialPressed;
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;
    }
}
