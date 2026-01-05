using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class WorkforceTypeEditor : VBoxContainer
{
    public event Action<WorkforceTypeConfig>? WorkforceTypeChanged;
    public event Action<string, string, decimal>? CustomPriceChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private HBoxContainer? _headerContainer;
    private Label? _nameLabel;
    private LineEdit? _nameInput;
    private Label? _workforceTypeLabel;
    private OptionButton? _workforceTypeDropdown;
    private Button? _addMaterialButton;
    private Button? _deleteButton;
    private VBoxContainer? _materialsContainer;

    private Button? _collapseToggleButton;

    private List<MaterialConsumptionEditor> _materialEditors = new();
    private WorkforceTypeConfig? _pendingConfig;
    private string _configId = Guid.NewGuid().ToString();

    public override void _Ready()
    {
        _headerContainer = GetNode<HBoxContainer>("HeaderContainer");
        _nameLabel = GetNode<Label>("HeaderContainer/NameLabel");
        _nameInput = GetNode<LineEdit>("HeaderContainer/NameInput");
        _workforceTypeLabel = GetNode<Label>("HeaderContainer/WorkforceTypeLabel");
        _workforceTypeDropdown = GetNode<OptionButton>("HeaderContainer/WorkforceTypeDropdown");
        _addMaterialButton = GetNode<Button>("HeaderContainer/AddMaterialButton");
        _deleteButton = GetNode<Button>("HeaderContainer/DeleteButton");
        _materialsContainer = GetNode<VBoxContainer>("MaterialsContainer");

        SetupWorkforceTypeDropdown();

        _nameInput.TextChanged += OnNameChanged;
        _workforceTypeDropdown.ItemSelected += OnWorkforceTypeSelected;
        _addMaterialButton.Pressed += OnAddMaterialPressed;
        _deleteButton.Pressed += OnDeletePressed;

        SetupCollapseButton();

        if (_pendingConfig != null)
        {
            var config = _pendingConfig;
            _pendingConfig = null;
            SetWorkforceTypeConfig(config);
        }
    }

    private void SetupWorkforceTypeDropdown()
    {
        if (_workforceTypeDropdown == null)
            return;

        _workforceTypeDropdown.Clear();
        foreach (WorkforceType type in System.Enum.GetValues(typeof(WorkforceType)))
        {
            _workforceTypeDropdown.AddItem(type.ToString());
        }
    }

    private void SetupCollapseButton()
    {
        _collapseToggleButton = new Button
        {
            Text = "▶ Expand Materials",
            CustomMinimumSize = new Vector2(0, 30)
        };
        _collapseToggleButton.Pressed += OnCollapseTogglePressed;

        var index = _headerContainer?.GetIndex() ?? 0;
        AddChild(_collapseToggleButton);
        MoveChild(_collapseToggleButton, (int)index + 1);

        if (_materialsContainer != null)
        {
            _materialsContainer.Visible = false;
        }
    }

    private void OnCollapseTogglePressed()
    {
        if (_materialsContainer == null || _collapseToggleButton == null)
            return;

        var newVisibility = !_materialsContainer.Visible;
        _materialsContainer.Visible = newVisibility;
        _collapseToggleButton.Text = newVisibility ? "▼ Collapse Materials" : "▶ Expand Materials";
    }

    public void SetWorkforceTypeConfig(WorkforceTypeConfig config)
    {
        if (_nameInput == null || _workforceTypeDropdown == null)
        {
            _pendingConfig = config;
            return;
        }

        _configId = config.Id;
        _nameInput.Text = config.Name;

        var typeIndex = (int)config.WorkforceType;
        if (typeIndex >= 0 && typeIndex < _workforceTypeDropdown.ItemCount)
        {
            _workforceTypeDropdown.Selected = typeIndex;
        }

        ClearMaterials();

        foreach (var consumption in config.MaterialConsumption)
        {
            var editor = CreateMaterialEditor();
            editor.SetConsumption(consumption);
            _materialEditors.Add(editor);
            _materialsContainer?.AddChild(editor);
        }
    }

    public void SetCustomPriceForMaterial(string materialId, decimal price)
    {
        foreach (var editor in _materialEditors)
        {
            var consumption = editor.GetConsumption();
            if (consumption.MaterialId == materialId && consumption.PriceSource.Type == PriceSourceType.Custom)
            {
                editor.SetCustomPrice(price);
            }
        }
    }

    public Dictionary<string, decimal> GetCustomPrices()
    {
        var customPrices = new Dictionary<string, decimal>();
        foreach (var editor in _materialEditors)
        {
            var consumption = editor.GetConsumption();
            if (consumption.PriceSource.Type == PriceSourceType.Custom)
            {
                customPrices[consumption.MaterialId] = editor.GetCustomPrice();
            }
        }
        return customPrices;
    }

    public WorkforceTypeConfig GetWorkforceTypeConfig()
    {
        if (_nameInput == null || _workforceTypeDropdown == null)
            throw new InvalidOperationException("WorkforceTypeEditor not initialized");

        var selectedIndex = _workforceTypeDropdown.Selected;
        var workforceType = (WorkforceType)selectedIndex;

        return new WorkforceTypeConfig
        {
            Id = _configId,
            Name = _nameInput.Text,
            WorkforceType = workforceType,
            MaterialConsumption = _materialEditors.Select(e => e.GetConsumption()).ToList()
        };
    }

    private void OnNameChanged(string newText)
    {
        EmitWorkforceTypeChanged();
    }

    private void OnWorkforceTypeSelected(long index)
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

        editor.SetWorkforceConfigId(_configId);

        editor.ConsumptionChanged += (_) => EmitWorkforceTypeChanged();
        editor.CustomPriceChanged += (configId, materialId, price) =>
        {
            CustomPriceChanged?.Invoke(configId, materialId, price);
        };
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
        if (_collapseToggleButton != null)
            _collapseToggleButton.Pressed -= OnCollapseTogglePressed;
        if (_nameInput != null)
            _nameInput.TextChanged -= OnNameChanged;
        if (_workforceTypeDropdown != null)
            _workforceTypeDropdown.ItemSelected -= OnWorkforceTypeSelected;
        if (_addMaterialButton != null)
            _addMaterialButton.Pressed -= OnAddMaterialPressed;
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;
    }
}
