using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class MaterialConsumptionEditor : GridContainer
{
    public event Action<WorkforceMaterialConsumption>? ConsumptionChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private OptionButton? _materialDropdown;
    private SpinBox? _quantityInput;
    private PriceSourceSelector? _priceSourceSelector;
    private Button? _deleteButton;

    private SessionManager? _sessionManager;

    public override void _Ready()
    {
        Columns = 4;

        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        _materialDropdown = GetNode<OptionButton>("MaterialDropdown");
        _quantityInput = GetNode<SpinBox>("QuantityInput");
        _priceSourceSelector = GetNode<PriceSourceSelector>("PriceSourceSelector");
        _deleteButton = GetNode<Button>("DeleteButton");

        SetupQuantityInput();
        PopulateMaterialDropdown();

        _materialDropdown.ItemSelected += OnMaterialSelected;
        _quantityInput.ValueChanged += OnQuantityChanged;
        _priceSourceSelector.PriceSourceChanged += OnPriceSourceChanged;
        _deleteButton.Pressed += OnDeletePressed;
    }

    private void SetupQuantityInput()
    {
        if (_quantityInput == null) return;

        _quantityInput.MinValue = 0;
        _quantityInput.MaxValue = 999999;
        _quantityInput.Step = 0.01;
        _quantityInput.Value = 0;
    }

    private void PopulateMaterialDropdown()
    {
        if (_materialDropdown == null || _sessionManager?.Session?.GameData == null)
            return;

        _materialDropdown.Clear();

        var materials = _sessionManager.Session.GameData.Materials.Values
            .OrderBy(m => m.Name)
            .ToList();

        for (int i = 0; i < materials.Count; i++)
        {
            var material = materials[i];
            _materialDropdown.AddItem($"{material.Name} ({material.Id})", i);
            _materialDropdown.SetItemMetadata(i, material.Id);
        }
    }

    public void SetConsumption(WorkforceMaterialConsumption consumption)
    {
        SelectMaterial(consumption.MaterialId);

        if (_quantityInput != null)
            _quantityInput.Value = (double)consumption.QuantityPer100WorkersPer24Hours;

        _priceSourceSelector?.SetPriceSource(consumption.PriceSource);
    }

    public WorkforceMaterialConsumption GetConsumption()
    {
        if (_materialDropdown == null || _quantityInput == null || _priceSourceSelector == null)
            throw new InvalidOperationException("MaterialConsumptionEditor not initialized");

        var materialId = GetSelectedMaterialId();

        return new WorkforceMaterialConsumption
        {
            MaterialId = materialId,
            QuantityPer100WorkersPer24Hours = (decimal)_quantityInput.Value,
            PriceSource = _priceSourceSelector.GetPriceSource()
        };
    }

    private string GetSelectedMaterialId()
    {
        if (_materialDropdown == null || _materialDropdown.Selected < 0)
            return "";

        return _materialDropdown.GetItemMetadata(_materialDropdown.Selected).AsString();
    }

    private void SelectMaterial(string materialId)
    {
        if (_materialDropdown == null) return;

        for (int i = 0; i < _materialDropdown.ItemCount; i++)
        {
            if (_materialDropdown.GetItemMetadata(i).AsString() == materialId)
            {
                _materialDropdown.Select(i);
                return;
            }
        }
    }

    private void OnMaterialSelected(long index)
    {
        EmitConsumptionChanged();
    }

    private void OnQuantityChanged(double value)
    {
        EmitConsumptionChanged();
    }

    private void OnPriceSourceChanged(PriceSource priceSource)
    {
        EmitConsumptionChanged();
    }

    private void OnDeletePressed()
    {
        EmitSignal(SignalName.DeleteRequested);
    }

    private void EmitConsumptionChanged()
    {
        ConsumptionChanged?.Invoke(GetConsumption());
    }

    public override void _ExitTree()
    {
        if (_materialDropdown != null)
            _materialDropdown.ItemSelected -= OnMaterialSelected;
        if (_quantityInput != null)
            _quantityInput.ValueChanged -= OnQuantityChanged;
        if (_priceSourceSelector != null)
            _priceSourceSelector.PriceSourceChanged -= OnPriceSourceChanged;
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;
    }
}
