using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class MaterialQuantityEditor : VBoxContainer
{
    public event Action? ItemsChanged;

    private SessionManager? _sessionManager;
    private VBoxContainer? _itemsContainer;
    private Button? _addButton;

    private List<MaterialQuantityItem> _items = new();

    public override void _Ready()
    {
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");
        _itemsContainer = GetNode<VBoxContainer>("ItemsContainer");
        _addButton = GetNode<Button>("AddButton");

        _addButton.Pressed += OnAddPressed;
    }

    public void SetItems(List<RecipeItem>? items)
    {
        ClearItems();

        if (items == null || items.Count == 0)
            return;

        foreach (var item in items)
        {
            AddItem(item.MaterialId, item.Quantity);
        }
    }

    public List<RecipeItem> GetItems()
    {
        return _items
            .Where(i => !string.IsNullOrWhiteSpace(i.GetMaterialId()))
            .Select(i => new RecipeItem
            {
                MaterialId = i.GetMaterialId(),
                Quantity = i.GetQuantity()
            })
            .ToList();
    }

    private void OnAddPressed()
    {
        AddItem("", 0);
        ItemsChanged?.Invoke();
    }

    private void AddItem(string materialId, decimal quantity)
    {
        var item = new MaterialQuantityItem(_sessionManager);
        item.SetMaterialId(materialId);
        item.SetQuantity(quantity);
        item.DeleteRequested += () => RemoveItem(item);
        item.ItemChanged += () => ItemsChanged?.Invoke();

        _itemsContainer?.AddChild(item);
        _items.Add(item);
    }

    private void RemoveItem(MaterialQuantityItem item)
    {
        _items.Remove(item);
        item.QueueFree();
        ItemsChanged?.Invoke();
    }

    private void ClearItems()
    {
        foreach (var item in _items)
            item.QueueFree();
        _items.Clear();
    }

    public override void _ExitTree()
    {
        if (_addButton != null)
            _addButton.Pressed -= OnAddPressed;
    }
}

public partial class MaterialQuantityItem : HBoxContainer
{
    [Signal]
    public delegate void DeleteRequestedEventHandler();

    public event Action? ItemChanged;

    private SessionManager? _sessionManager;
    private SearchableDropdown? _materialDropdown;
    private SpinBox? _quantitySpinBox;
    private Button? _deleteButton;

    public MaterialQuantityItem(SessionManager? sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override void _Ready()
    {
        var materialLabel = new Label { Text = "Material:", CustomMinimumSize = new Vector2(80, 0) };
        AddChild(materialLabel);

        var dropdownScene = GD.Load<PackedScene>("res://scenes/UI/Components/SearchableDropdown.tscn");
        _materialDropdown = dropdownScene.Instantiate<SearchableDropdown>();
        _materialDropdown.CustomMinimumSize = new Vector2(300, 0);
        AddChild(_materialDropdown);

        if (_sessionManager?.Session?.GameData?.Materials != null)
        {
            var materials = _sessionManager.Session.GameData.Materials.Values
                .OrderBy(m => m.Name)
                .Select(m => new DropdownItem { Value = m.Id, DisplayText = m.Name })
                .ToList();
            _materialDropdown.SetItems(materials);
        }

        _materialDropdown.ItemSelected += _ => ItemChanged?.Invoke();

        var quantityLabel = new Label { Text = "Quantity:", CustomMinimumSize = new Vector2(80, 0) };
        AddChild(quantityLabel);

        _quantitySpinBox = new SpinBox
        {
            MinValue = 0,
            MaxValue = 100000,
            Step = 0.01,
            CustomMinimumSize = new Vector2(120, 0)
        };
        _quantitySpinBox.ValueChanged += _ => ItemChanged?.Invoke();
        AddChild(_quantitySpinBox);

        _deleteButton = new Button { Text = "Remove" };
        _deleteButton.Pressed += OnDeletePressed;
        AddChild(_deleteButton);
    }

    public void SetMaterialId(string materialId)
    {
        if (_materialDropdown != null)
            _materialDropdown.SetSelectedValue(materialId);
    }

    public string GetMaterialId()
    {
        return _materialDropdown?.GetSelectedValue() ?? "";
    }

    public void SetQuantity(decimal quantity)
    {
        if (_quantitySpinBox != null)
            _quantitySpinBox.Value = (double)quantity;
    }

    public decimal GetQuantity()
    {
        return _quantitySpinBox?.Value != null ? (decimal)_quantitySpinBox.Value : 0m;
    }

    private void OnDeletePressed()
    {
        EmitSignal(SignalName.DeleteRequested);
    }

    public override void _ExitTree()
    {
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;
    }
}
