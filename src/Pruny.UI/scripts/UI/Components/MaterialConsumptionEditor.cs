using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class MaterialConsumptionEditor : GridContainer
{
    public event Action<WorkforceMaterialConsumption>? ConsumptionChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private SearchableDropdown? _materialDropdown;
    private SpinBox? _quantityInput;
    private PriceSourceSelector? _priceSourceSelector;
    private Button? _deleteButton;

    private SessionManager? _sessionManager;
    private WorkforceMaterialConsumption? _pendingConsumption;
    private string? _workforceConfigId;

    public override void _Ready()
    {
        Columns = 4;

        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        _materialDropdown = GetNode<SearchableDropdown>("MaterialDropdown");
        _quantityInput = GetNode<SpinBox>("QuantityInput");
        _priceSourceSelector = GetNode<PriceSourceSelector>("PriceSourceSelector");
        _deleteButton = GetNode<Button>("DeleteButton");

        SetupQuantityInput();
        PopulateMaterialDropdown();

        _materialDropdown.ItemSelected += OnMaterialSelected;
        _quantityInput.ValueChanged += OnQuantityChanged;
        _priceSourceSelector.PriceSourceChanged += OnPriceSourceChanged;
        _deleteButton.Pressed += OnDeletePressed;

        if (_pendingConsumption != null)
        {
            var consumption = _pendingConsumption;
            _pendingConsumption = null;
            SetConsumption(consumption);
        }
    }

    private void SetupQuantityInput()
    {
        if (_quantityInput == null) return;

        _quantityInput.MinValue = 0;
        _quantityInput.MaxValue = 999999;
        _quantityInput.Step = 0.01;
        _quantityInput.Value = 0;
    }

    public void SetWorkforceConfigId(string workforceConfigId)
    {
        _workforceConfigId = workforceConfigId;
        UpdatePriceSourceContext();
    }

    private void PopulateMaterialDropdown()
    {
        if (_materialDropdown == null || _sessionManager?.Session?.GameData == null)
            return;

        var materials = _sessionManager.Session.GameData.Materials.Values
            .OrderBy(m => m.Id)
            .Select(m => new DropdownItem
            {
                Value = m.Id,
                DisplayText = $"{m.Id} ({m.Name})"
            })
            .ToList();

        _materialDropdown.SetItems(materials);
    }

    private void UpdatePriceSourceContext()
    {
        if (_priceSourceSelector == null || string.IsNullOrEmpty(_workforceConfigId))
            return;

        var materialId = GetSelectedMaterialId();
        if (!string.IsNullOrEmpty(materialId))
        {
            _priceSourceSelector.SetContext(_workforceConfigId, materialId);
        }
    }

    public void SetConsumption(WorkforceMaterialConsumption consumption)
    {
        if (_materialDropdown == null || _quantityInput == null || _priceSourceSelector == null)
        {
            _pendingConsumption = consumption;
            return;
        }

        SelectMaterial(consumption.MaterialId);
        _quantityInput.Value = (double)consumption.QuantityPer100WorkersPer24Hours;
        _priceSourceSelector.SetPriceSource(consumption.PriceSource);
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
        return _materialDropdown?.GetSelectedValue() ?? "";
    }

    private void SelectMaterial(string materialId)
    {
        _materialDropdown?.SetSelectedValue(materialId);
    }

    private void OnMaterialSelected(string materialId)
    {
        UpdatePriceSourceContext();
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
