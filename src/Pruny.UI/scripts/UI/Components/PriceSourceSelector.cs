using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class PriceSourceSelector : GridContainer
{
    public event Action<PriceSource>? PriceSourceChanged;

    private OptionButton? _typeDropdown;
    private Control? _sourceInputContainer;
    private LineEdit? _customSourceInput;
    private HBoxContainer? _apiSourceContainer;
    private OptionButton? _exchangeDropdown;
    private OptionButton? _priceTypeDropdown;
    private OptionButton? _productionLineDropdown;
    private Button? _adjustmentsButton;
    private VBoxContainer? _adjustmentsContainer;
    private Button? _addAdjustmentButton;

    private SessionManager? _sessionManager;
    private PriceSource? _currentPriceSource;
    private List<AdjustmentEditor> _adjustmentEditors = new();

    public override void _Ready()
    {
        Columns = 3;

        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        _typeDropdown = GetNode<OptionButton>("TypeDropdown");
        _sourceInputContainer = GetNode<Control>("SourceInputContainer");
        _adjustmentsButton = GetNode<Button>("AdjustmentsButton");
        _adjustmentsContainer = GetNode<VBoxContainer>("AdjustmentsContainer");
        _addAdjustmentButton = GetNode<Button>("AdjustmentsContainer/AddAdjustmentButton");

        SetupSourceInputContainers();
        SetupTypeDropdown();
        SetupAdjustmentsButton();

        _typeDropdown.ItemSelected += OnTypeSelected;
    }

    private void SetupSourceInputContainers()
    {
        _customSourceInput = new LineEdit
        {
            PlaceholderText = "Custom price name",
            Visible = false
        };
        _sourceInputContainer?.AddChild(_customSourceInput);
        _customSourceInput.TextChanged += (_) => EmitPriceSourceChanged();

        _apiSourceContainer = new HBoxContainer { Visible = false };
        _sourceInputContainer?.AddChild(_apiSourceContainer);

        _exchangeDropdown = new OptionButton();
        _apiSourceContainer.AddChild(_exchangeDropdown);
        _exchangeDropdown.ItemSelected += (_) => EmitPriceSourceChanged();

        _priceTypeDropdown = new OptionButton();
        _priceTypeDropdown.AddItem("AVG", 0);
        _priceTypeDropdown.AddItem("ASK", 1);
        _priceTypeDropdown.AddItem("BID", 2);
        _apiSourceContainer.AddChild(_priceTypeDropdown);
        _priceTypeDropdown.ItemSelected += (_) => EmitPriceSourceChanged();

        _productionLineDropdown = new OptionButton
        {
            Visible = false
        };
        _sourceInputContainer?.AddChild(_productionLineDropdown);
        _productionLineDropdown.ItemSelected += (_) => EmitPriceSourceChanged();
    }

    private void SetupTypeDropdown()
    {
        _typeDropdown?.Clear();
        _typeDropdown?.AddItem("API Price", (int)PriceSourceType.Api);
        _typeDropdown?.AddItem("Production Line", (int)PriceSourceType.ProductionLine);
        _typeDropdown?.AddItem("Custom Price", (int)PriceSourceType.Custom);
        _typeDropdown?.Select(0);
        SetPriceSource(new PriceSource { Type = PriceSourceType.Api, SourceIdentifier = "CI1-ASK" });
    }

    private void SetupAdjustmentsButton()
    {
        if (_adjustmentsButton == null) return;

        _adjustmentsButton.Text = "Adjustments (0)";
        _adjustmentsButton.Pressed += OnAdjustmentsButtonPressed;

        if (_addAdjustmentButton != null)
            _addAdjustmentButton.Pressed += AddAdjustment;
    }

    public void SetPriceSource(PriceSource priceSource)
    {
        _currentPriceSource = priceSource;

        _typeDropdown?.Select((int)priceSource.Type);
        UpdateSourceInputVisibility(priceSource.Type);

        switch (priceSource.Type)
        {
            case PriceSourceType.Api:
                var parts = priceSource.SourceIdentifier.Split('-');
                if (parts.Length == 2)
                {
                    PopulateExchangeDropdown();
                    SelectExchangeCode(parts[0]);
                    SelectPriceType(parts[1]);
                }
                break;

            case PriceSourceType.ProductionLine:
                PopulateProductionLineDropdown();
                SelectProductionLine(priceSource.SourceIdentifier);
                break;

            case PriceSourceType.Custom:
                if (_customSourceInput != null)
                    _customSourceInput.Text = priceSource.SourceIdentifier;
                break;
        }

        RebuildAdjustments(priceSource.Adjustments);
    }

    public PriceSource GetPriceSource()
    {
        if (_typeDropdown == null)
            throw new InvalidOperationException("PriceSourceSelector not initialized");

        var type = (PriceSourceType)_typeDropdown.Selected;
        var sourceIdentifier = GetSourceIdentifier(type);

        return new PriceSource
        {
            Type = type,
            SourceIdentifier = sourceIdentifier,
            Adjustments = _adjustmentEditors.Select(e => e.GetAdjustment()).ToList()
        };
    }

    private string GetSourceIdentifier(PriceSourceType type)
    {
        return type switch
        {
            PriceSourceType.Api => BuildApiSourceIdentifier(),
            PriceSourceType.ProductionLine => GetProductionLineIdentifier(),
            PriceSourceType.Custom => _customSourceInput?.Text ?? "Custom",
            _ => throw new ArgumentException($"Unknown price source type: {type}")
        };
    }

    private string BuildApiSourceIdentifier()
    {
        var exchange = _exchangeDropdown?.GetItemText(_exchangeDropdown.Selected) ?? "IC1";
        var priceType = _priceTypeDropdown?.GetItemText(_priceTypeDropdown.Selected) ?? "AVG";
        return $"{exchange}-{priceType}";
    }

    private string GetProductionLineIdentifier()
    {
        if (_productionLineDropdown == null || _productionLineDropdown.Selected < 0)
            return "";

        return _productionLineDropdown.GetItemMetadata(_productionLineDropdown.Selected).AsString();
    }

    private void OnTypeSelected(long index)
    {
        var type = (PriceSourceType)index;
        UpdateSourceInputVisibility(type);

        if (type == PriceSourceType.Api)
            PopulateExchangeDropdown();
        else if (type == PriceSourceType.ProductionLine)
            PopulateProductionLineDropdown();

        EmitPriceSourceChanged();
    }

    private void UpdateSourceInputVisibility(PriceSourceType type)
    {
        if (_customSourceInput != null)
            _customSourceInput.Visible = type == PriceSourceType.Custom;

        if (_apiSourceContainer != null)
            _apiSourceContainer.Visible = type == PriceSourceType.Api;

        if (_productionLineDropdown != null)
            _productionLineDropdown.Visible = type == PriceSourceType.ProductionLine;
    }

    private void PopulateExchangeDropdown()
    {
        if (_exchangeDropdown == null || _sessionManager?.Session == null)
            return;

        _exchangeDropdown.Clear();

        var exchangeCodes = _sessionManager.Session.MarketDataAnalyzer.GetAvailableExchangeCodes();

        if (exchangeCodes.Count == 0)
        {
            _exchangeDropdown.AddItem("(No market data)", 0);
            _exchangeDropdown.Disabled = true;
            return;
        }

        _exchangeDropdown.Disabled = false;
        for (int i = 0; i < exchangeCodes.Count; i++)
        {
            _exchangeDropdown.AddItem(exchangeCodes[i], i);
        }
    }

    private void PopulateProductionLineDropdown()
    {
        if (_productionLineDropdown == null || _sessionManager?.Session?.CurrentWorkspace == null)
            return;

        _productionLineDropdown.Clear();

        var productionLines = _sessionManager.Session.CurrentWorkspace.ProductionLines;

        if (productionLines.Count == 0)
        {
            _productionLineDropdown.AddItem("(No production lines)", 0);
            _productionLineDropdown.Disabled = true;
            return;
        }

        _productionLineDropdown.Disabled = false;
        for (int i = 0; i < productionLines.Count; i++)
        {
            var line = productionLines[i];
            _productionLineDropdown.AddItem(line.Id, i);
            _productionLineDropdown.SetItemMetadata(i, line.Id);
        }
    }

    private void SelectExchangeCode(string exchangeCode)
    {
        if (_exchangeDropdown == null) return;

        for (int i = 0; i < _exchangeDropdown.ItemCount; i++)
        {
            if (_exchangeDropdown.GetItemText(i) == exchangeCode)
            {
                _exchangeDropdown.Select(i);
                return;
            }
        }
    }

    private void SelectPriceType(string priceType)
    {
        if (_priceTypeDropdown == null) return;

        for (int i = 0; i < _priceTypeDropdown.ItemCount; i++)
        {
            if (_priceTypeDropdown.GetItemText(i) == priceType)
            {
                _priceTypeDropdown.Select(i);
                return;
            }
        }
    }

    private void SelectProductionLine(string lineId)
    {
        if (_productionLineDropdown == null) return;

        for (int i = 0; i < _productionLineDropdown.ItemCount; i++)
        {
            if (_productionLineDropdown.GetItemMetadata(i).AsString() == lineId)
            {
                _productionLineDropdown.Select(i);
                return;
            }
        }
    }

    private void OnAdjustmentsButtonPressed()
    {
        if (_adjustmentsContainer == null) return;

        _adjustmentsContainer.Visible = !_adjustmentsContainer.Visible;
    }

    public void AddAdjustment()
    {
        var editor = CreateAdjustmentEditor();
        _adjustmentEditors.Add(editor);
        _adjustmentsContainer?.AddChild(editor);
        UpdateAdjustmentsButtonText();
        EmitPriceSourceChanged();
    }

    private AdjustmentEditor CreateAdjustmentEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/AdjustmentEditor.tscn");
        var editor = scene.Instantiate<AdjustmentEditor>();
        editor.AdjustmentChanged += (_) => EmitPriceSourceChanged();
        editor.DeleteRequested += () =>
        {
            _adjustmentEditors.Remove(editor);
            editor.QueueFree();
            UpdateAdjustmentsButtonText();
            EmitPriceSourceChanged();
        };
        return editor;
    }

    private void RebuildAdjustments(List<Adjustment> adjustments)
    {
        foreach (var editor in _adjustmentEditors)
            editor.QueueFree();
        _adjustmentEditors.Clear();

        foreach (var adjustment in adjustments)
        {
            var editor = CreateAdjustmentEditor();
            editor.SetAdjustment(adjustment);
            _adjustmentEditors.Add(editor);
            _adjustmentsContainer?.AddChild(editor);
        }

        UpdateAdjustmentsButtonText();
    }

    private void UpdateAdjustmentsButtonText()
    {
        if (_adjustmentsButton != null)
            _adjustmentsButton.Text = $"Adjustments ({_adjustmentEditors.Count})";
    }

    private void EmitPriceSourceChanged()
    {
        PriceSourceChanged?.Invoke(GetPriceSource());
    }

    public override void _ExitTree()
    {
        if (_typeDropdown != null)
            _typeDropdown.ItemSelected -= OnTypeSelected;
    }
}
