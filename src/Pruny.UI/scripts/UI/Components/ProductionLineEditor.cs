using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class ProductionLineEditor : VBoxContainer
{
    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private SessionManager? _sessionManager;

    private HBoxContainer? _headerContainer;
    private Label? _recipeLabel;
    private Control? _recipeSelectorContainer;
    private RecipeSelector? _recipeSelector;
    private Button? _deleteButton;

    private Label? _lineIdLabel;

    private VBoxContainer? _efficiencyContainer;
    private Button? _efficiencyToggleButton;
    private VBoxContainer? _efficiencyContent;
    private EfficiencyModifiersEditor? _efficiencyEditor;

    private VBoxContainer? _inputPricesContainer;
    private Button? _inputPricesToggleButton;
    private VBoxContainer? _inputPricesContent;

    private VBoxContainer? _outputPricesContainer;
    private Button? _outputPricesToggleButton;
    private VBoxContainer? _outputPricesContent;

    private VBoxContainer? _workforceOverrideContainer;
    private Button? _workforceToggleButton;
    private VBoxContainer? _workforceContent;

    private VBoxContainer? _outputOverridesContainer;
    private Button? _outputOverridesToggleButton;
    private VBoxContainer? _outputOverridesContent;
    private MaterialQuantityEditor? _outputOverridesEditor;

    private string _lineId = Guid.NewGuid().ToString();
    private Dictionary<string, PriceSourceSelector> _inputPriceSelectors = new();
    private Dictionary<string, PriceSourceSelector> _outputPriceSelectors = new();

    private ProductionLine? _pendingLineToLoad;

    public override void _Ready()
    {
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        _headerContainer = GetNode<HBoxContainer>("HeaderContainer");
        _recipeLabel = GetNode<Label>("HeaderContainer/RecipeLabel");
        _recipeSelectorContainer = GetNode<Control>("HeaderContainer/RecipeSelectorContainer");
        _deleteButton = GetNode<Button>("HeaderContainer/DeleteButton");

        _lineIdLabel = GetNode<Label>("LineIdLabel");

        _efficiencyContainer = GetNode<VBoxContainer>("EfficiencyContainer");
        _efficiencyToggleButton = GetNode<Button>("EfficiencyContainer/ToggleButton");
        _efficiencyContent = GetNode<VBoxContainer>("EfficiencyContainer/Content");

        _inputPricesContainer = GetNode<VBoxContainer>("InputPricesContainer");
        _inputPricesToggleButton = GetNode<Button>("InputPricesContainer/ToggleButton");
        _inputPricesContent = GetNode<VBoxContainer>("InputPricesContainer/Content");

        _outputPricesContainer = GetNode<VBoxContainer>("OutputPricesContainer");
        _outputPricesToggleButton = GetNode<Button>("OutputPricesContainer/ToggleButton");
        _outputPricesContent = GetNode<VBoxContainer>("OutputPricesContainer/Content");

        _workforceOverrideContainer = GetNode<VBoxContainer>("WorkforceOverrideContainer");
        _workforceToggleButton = GetNode<Button>("WorkforceOverrideContainer/ToggleButton");
        _workforceContent = GetNode<VBoxContainer>("WorkforceOverrideContainer/Content");

        _outputOverridesContainer = GetNode<VBoxContainer>("OutputOverridesContainer");
        _outputOverridesToggleButton = GetNode<Button>("OutputOverridesContainer/ToggleButton");
        _outputOverridesContent = GetNode<VBoxContainer>("OutputOverridesContainer/Content");

        SetupRecipeSelector();
        SetupEfficiencyEditor();
        SetupOutputOverridesEditor();
        SetupToggleButtons();

        _deleteButton.Pressed += OnDeletePressed;

        _lineIdLabel.Text = $"ID: {_lineId}";

        if (_pendingLineToLoad != null)
        {
            var pendingLine = _pendingLineToLoad;
            _pendingLineToLoad = null;
            SetProductionLine(pendingLine);
        }
    }

    private void SetupRecipeSelector()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/RecipeSelector.tscn");
        _recipeSelector = scene.Instantiate<RecipeSelector>();
        _recipeSelectorContainer?.AddChild(_recipeSelector);

        _recipeSelector.RecipeSelected += OnRecipeSelected;
    }

    private void SetupEfficiencyEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/EfficiencyModifiersEditor.tscn");
        _efficiencyEditor = scene.Instantiate<EfficiencyModifiersEditor>();
        _efficiencyContent?.AddChild(_efficiencyEditor);
    }

    private void SetupOutputOverridesEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/MaterialQuantityEditor.tscn");
        _outputOverridesEditor = scene.Instantiate<MaterialQuantityEditor>();
        _outputOverridesEditor.ItemsChanged += OnOutputOverridesChanged;
        _outputOverridesContent?.AddChild(_outputOverridesEditor);
    }

    private void SetupToggleButtons()
    {
        _efficiencyToggleButton!.Pressed += () => ToggleSection(_efficiencyContent!, _efficiencyToggleButton!);
        _inputPricesToggleButton!.Pressed += () => ToggleSection(_inputPricesContent!, _inputPricesToggleButton!);
        _outputPricesToggleButton!.Pressed += () => ToggleSection(_outputPricesContent!, _outputPricesToggleButton!);
        _workforceToggleButton!.Pressed += () => ToggleSection(_workforceContent!, _workforceToggleButton!);
        _outputOverridesToggleButton!.Pressed += () => ToggleSection(_outputOverridesContent!, _outputOverridesToggleButton!);
    }

    private void ToggleSection(Control content, Button button)
    {
        content.Visible = !content.Visible;
        button.Text = content.Visible ? "▼ " + button.Text.Substring(2) : "▶ " + button.Text.Substring(2);
    }

    private void OnRecipeSelected(string recipeId)
    {
        if (_sessionManager?.Session?.GameData == null)
            return;

        if (!_sessionManager.Session.GameData.Recipes.TryGetValue(recipeId, out var recipe))
            return;

        RebuildInputPriceSelectors(recipe.Inputs);
        UpdateOutputPriceSelectors();

        if (recipe.Outputs.Count == 0)
        {
            _outputOverridesToggleButton?.AddThemeColorOverride("font_color", new Color(1, 1, 0.3f));
            if (_outputOverridesContent != null && !_outputOverridesContent.Visible)
            {
                ToggleSection(_outputOverridesContent, _outputOverridesToggleButton!);
            }
        }
        else
        {
            _outputOverridesToggleButton?.RemoveThemeColorOverride("font_color");
        }

        if (_pendingLineToLoad != null)
        {
            ApplyPendingLineData();
        }
    }

    private void OnOutputOverridesChanged()
    {
        UpdateOutputPriceSelectors();
    }

    private void UpdateOutputPriceSelectors()
    {
        var effectiveOutputs = GetEffectiveOutputs();
        RebuildOutputPriceSelectors(effectiveOutputs);
    }

    private List<RecipeItem> GetEffectiveOutputs()
    {
        var overrides = _outputOverridesEditor?.GetItems();
        if (overrides != null && overrides.Count > 0)
            return overrides;

        var recipeId = _recipeSelector?.GetSelectedRecipeId();
        if (string.IsNullOrEmpty(recipeId) || _sessionManager?.Session?.GameData == null)
            return new List<RecipeItem>();

        if (_sessionManager.Session.GameData.Recipes.TryGetValue(recipeId, out var recipe))
            return recipe.Outputs;

        return new List<RecipeItem>();
    }

    private void RebuildInputPriceSelectors(List<RecipeItem> inputs)
    {
        foreach (var selector in _inputPriceSelectors.Values)
            selector.QueueFree();
        _inputPriceSelectors.Clear();

        foreach (var input in inputs)
        {
            var materialName = GetMaterialName(input.MaterialId);
            var label = new Label { Text = $"{materialName}:" };
            _inputPricesContent?.AddChild(label);

            var scene = GD.Load<PackedScene>("res://scenes/UI/Components/PriceSourceSelector.tscn");
            var selector = scene.Instantiate<PriceSourceSelector>();
            _inputPricesContent?.AddChild(selector);

            _inputPriceSelectors[input.MaterialId] = selector;
        }
    }

    private void RebuildOutputPriceSelectors(List<RecipeItem> outputs)
    {
        foreach (var selector in _outputPriceSelectors.Values)
            selector.QueueFree();
        _outputPriceSelectors.Clear();

        foreach (var output in outputs)
        {
            var materialName = GetMaterialName(output.MaterialId);
            var label = new Label { Text = $"{materialName}:" };
            _outputPricesContent?.AddChild(label);

            var scene = GD.Load<PackedScene>("res://scenes/UI/Components/PriceSourceSelector.tscn");
            var selector = scene.Instantiate<PriceSourceSelector>();
            _outputPricesContent?.AddChild(selector);

            _outputPriceSelectors[output.MaterialId] = selector;
        }
    }

    private string GetMaterialName(string materialId)
    {
        if (_sessionManager?.Session?.GameData?.Materials.TryGetValue(materialId, out var material) == true)
            return material.Name;
        return materialId;
    }

    public void SetProductionLine(ProductionLine line)
    {
        _lineId = line.Id;

        if (_lineIdLabel != null)
        {
            _lineIdLabel.Text = $"ID: {_lineId}";
        }

        _pendingLineToLoad = line;

        if (_recipeSelector != null)
        {
            _recipeSelector.SelectRecipe(line.RecipeId);
            _efficiencyEditor?.SetModifiers(line.AdditionalEfficiencyModifiers);
        }
    }

    private void ApplyPendingLineData()
    {
        if (_pendingLineToLoad == null)
            return;

        foreach (var kvp in _pendingLineToLoad.InputPriceSources)
        {
            if (_inputPriceSelectors.TryGetValue(kvp.Key, out var selector))
                selector.SetPriceSource(kvp.Value);
        }

        foreach (var kvp in _pendingLineToLoad.OutputPriceSources)
        {
            if (_outputPriceSelectors.TryGetValue(kvp.Key, out var selector))
                selector.SetPriceSource(kvp.Value);
        }

        _outputOverridesEditor?.SetItems(_pendingLineToLoad.OutputOverrides);

        _pendingLineToLoad = null;
    }

    public ProductionLine GetProductionLine()
    {
        if (_recipeSelector == null)
            throw new InvalidOperationException("ProductionLineEditor not initialized");

        var recipeId = _recipeSelector.GetSelectedRecipeId();
        if (string.IsNullOrEmpty(recipeId))
            throw new InvalidOperationException("No recipe selected");

        var inputPriceSources = new Dictionary<string, PriceSource>();
        foreach (var kvp in _inputPriceSelectors)
            inputPriceSources[kvp.Key] = kvp.Value.GetPriceSource();

        var outputPriceSources = new Dictionary<string, PriceSource>();
        foreach (var kvp in _outputPriceSelectors)
            outputPriceSources[kvp.Key] = kvp.Value.GetPriceSource();

        var outputOverrides = _outputOverridesEditor?.GetItems();
        if (outputOverrides?.Count == 0)
            outputOverrides = null;

        return new ProductionLine
        {
            Id = _lineId,
            RecipeId = recipeId,
            WorkforceOverride = null,
            OutputOverrides = outputOverrides,
            InputPriceSources = inputPriceSources,
            OutputPriceSources = outputPriceSources,
            AdditionalEfficiencyModifiers = _efficiencyEditor?.GetModifiers() ?? new List<decimal>()
        };
    }

    private void OnDeletePressed()
    {
        EmitSignal(SignalName.DeleteRequested);
    }

    public override void _ExitTree()
    {
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;

        if (_recipeSelector != null)
            _recipeSelector.RecipeSelected -= OnRecipeSelected;
    }
}
