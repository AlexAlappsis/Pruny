using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class ProductionLineEditor : VBoxContainer
{
    public event Action<string, string, decimal>? CustomPriceChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private SessionManager? _sessionManager;

    private HBoxContainer? _headerContainer;
    private Label? _recipeLabel;
    private Control? _recipeSelectorContainer;
    private RecipeSelector? _recipeSelector;
    private Button? _deleteButton;

    private Label? _lineIdLabel;
    private LineEdit? _nameInput;
    private Button? _groupsButton;
    private PopupPanel? _groupsPopup;
    private VBoxContainer? _groupsCheckboxContainer;
    private List<string> _selectedGroupIds = new();

    private Button? _collapseToggleButton;
    private Control? _detailsContainer;

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
    private WorkforceEditor? _workforceEditor;

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
        _nameInput = GetNode<LineEdit>("NameContainer/NameInput");
        _groupsButton = GetNode<Button>("GroupsContainer/GroupsButton");

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
        SetupWorkforceEditor();
        SetupOutputOverridesEditor();
        SetupToggleButtons();
        SetupCollapseButton();
        SetupGroupsSelector();

        _deleteButton.Pressed += OnDeletePressed;

        _lineIdLabel.Text = $"ID: {_lineId}";

        if (_pendingLineToLoad != null)
        {
            var pendingLine = _pendingLineToLoad;
            _pendingLineToLoad = null;
            SetProductionLine(pendingLine);
        }
    }

    private void SetupCollapseButton()
    {
        _collapseToggleButton = new Button
        {
            Text = "▶ Expand All",
            CustomMinimumSize = new Vector2(0, 30)
        };
        _collapseToggleButton.Pressed += OnMainTogglePressed;

        var index = _lineIdLabel?.GetIndex() ?? 1;
        AddChild(_collapseToggleButton);
        MoveChild(_collapseToggleButton, (int)index + 1);

        _efficiencyContainer!.Visible = false;
        _inputPricesContainer!.Visible = false;
        _outputPricesContainer!.Visible = false;
        _workforceOverrideContainer!.Visible = false;
        _outputOverridesContainer!.Visible = false;
    }

    private void OnMainTogglePressed()
    {
        if (_detailsContainer == null)
        {
            _detailsContainer = GetNode<Control>("EfficiencyContainer")?.GetParent() as Control;
        }

        if (_detailsContainer != null)
        {
            var isVisible = _efficiencyContainer?.Visible ?? true;
            var newVisibility = !isVisible;

            _efficiencyContainer!.Visible = newVisibility;
            _inputPricesContainer!.Visible = newVisibility;
            _outputPricesContainer!.Visible = newVisibility;
            _workforceOverrideContainer!.Visible = newVisibility;
            _outputOverridesContainer!.Visible = newVisibility;

            if (_collapseToggleButton != null)
            {
                _collapseToggleButton.Text = newVisibility ? "▼ Collapse All" : "▶ Expand All";
            }
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

    private void SetupWorkforceEditor()
    {
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/WorkforceEditor.tscn");
        _workforceEditor = scene.Instantiate<WorkforceEditor>();
        _workforceContent?.AddChild(_workforceEditor);
    }

    private void SetupOutputOverridesEditor()
    {
        GD.Print("ProductionLineEditor: SetupOutputOverridesEditor called");
        var scene = GD.Load<PackedScene>("res://scenes/UI/Components/MaterialQuantityEditor.tscn");
        _outputOverridesEditor = scene.Instantiate<MaterialQuantityEditor>();
        _outputOverridesEditor.ItemsChanged += OnOutputOverridesChanged;
        _outputOverridesContent?.AddChild(_outputOverridesEditor);
        GD.Print("ProductionLineEditor: OutputOverridesEditor created and added");
    }

    private void SetupToggleButtons()
    {
        _efficiencyToggleButton!.Pressed += () => ToggleSection(_efficiencyContent!, _efficiencyToggleButton!);
        _inputPricesToggleButton!.Pressed += () => ToggleSection(_inputPricesContent!, _inputPricesToggleButton!);
        _outputPricesToggleButton!.Pressed += () => ToggleSection(_outputPricesContent!, _outputPricesToggleButton!);
        _workforceToggleButton!.Pressed += () => ToggleSection(_workforceContent!, _workforceToggleButton!);
        _outputOverridesToggleButton!.Pressed += () => ToggleSection(_outputOverridesContent!, _outputOverridesToggleButton!);
    }

    private void SetupGroupsSelector()
    {
        if (_groupsButton == null) return;

        _groupsButton.Pressed += OnGroupsButtonPressed;
        UpdateGroupsButtonText();

        _groupsPopup = new PopupPanel();
        _groupsPopup.Size = new Vector2I(300, 200);

        var popupContent = new VBoxContainer();
        _groupsPopup.AddChild(popupContent);

        var titleLabel = new Label { Text = "Select Groups:" };
        popupContent.AddChild(titleLabel);

        var scrollContainer = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(280, 150)
        };
        popupContent.AddChild(scrollContainer);

        _groupsCheckboxContainer = new VBoxContainer();
        scrollContainer.AddChild(_groupsCheckboxContainer);

        AddChild(_groupsPopup);
    }

    private void OnGroupsButtonPressed()
    {
        if (_groupsPopup == null || _groupsCheckboxContainer == null) return;

        RebuildGroupCheckboxes();

        var globalPos = _groupsButton!.GlobalPosition;
        _groupsPopup.Position = new Vector2I((int)globalPos.X, (int)(globalPos.Y + _groupsButton.Size.Y));
        _groupsPopup.Popup();
    }

    private void RebuildGroupCheckboxes()
    {
        if (_groupsCheckboxContainer == null) return;

        foreach (var child in _groupsCheckboxContainer.GetChildren())
            child.QueueFree();

        var groups = _sessionManager?.Session?.CurrentWorkspace?.ProductionLineGroups;
        if (groups == null || groups.Count == 0)
        {
            var noGroupsLabel = new Label { Text = "(No groups available)" };
            _groupsCheckboxContainer.AddChild(noGroupsLabel);
            return;
        }

        foreach (var group in groups)
        {
            var checkbox = new CheckBox
            {
                Text = group.Name,
                ButtonPressed = _selectedGroupIds.Contains(group.Id)
            };
            checkbox.SetMeta("group_id", group.Id);
            checkbox.Toggled += (toggled) => OnGroupCheckboxToggled(group.Id, toggled);
            _groupsCheckboxContainer.AddChild(checkbox);
        }
    }

    private void OnGroupCheckboxToggled(string groupId, bool toggled)
    {
        if (toggled && !_selectedGroupIds.Contains(groupId))
        {
            _selectedGroupIds.Add(groupId);
        }
        else if (!toggled && _selectedGroupIds.Contains(groupId))
        {
            _selectedGroupIds.Remove(groupId);
        }
        UpdateGroupsButtonText();
    }

    private void UpdateGroupsButtonText()
    {
        if (_groupsButton == null) return;

        if (_selectedGroupIds.Count == 0)
        {
            _groupsButton.Text = "Groups: None";
        }
        else
        {
            var groups = _sessionManager?.Session?.CurrentWorkspace?.ProductionLineGroups;
            if (groups == null)
            {
                _groupsButton.Text = $"Groups: {_selectedGroupIds.Count} selected";
            }
            else
            {
                var names = groups
                    .Where(g => _selectedGroupIds.Contains(g.Id))
                    .Select(g => g.Name)
                    .ToList();
                _groupsButton.Text = names.Count > 2
                    ? $"Groups: {names.Count} selected"
                    : $"Groups: {string.Join(", ", names)}";
            }
        }
    }

    public void SetGroupIds(List<string> groupIds)
    {
        _selectedGroupIds = new List<string>(groupIds);
        UpdateGroupsButtonText();
    }

    public List<string> GetGroupIds()
    {
        return new List<string>(_selectedGroupIds);
    }

    private void ToggleSection(Control content, Button button)
    {
        content.Visible = !content.Visible;
        button.Text = content.Visible ? "▼ " + button.Text.Substring(2) : "▶ " + button.Text.Substring(2);
    }

    private void OnRecipeSelected(string recipeId)
    {
        GD.Print($"ProductionLineEditor: OnRecipeSelected called with {recipeId}");

        if (_sessionManager?.Session?.GameData == null)
            return;

        if (!_sessionManager.Session.GameData.Recipes.TryGetValue(recipeId, out var recipe))
            return;

        GD.Print($"  Recipe found, has {recipe.Outputs.Count} outputs");
        GD.Print($"  _pendingLineToLoad is null: {_pendingLineToLoad == null}");

        if (_pendingLineToLoad != null)
        {
            ApplyPendingOutputOverrides();
        }

        RebuildInputPriceSelectors(recipe.Inputs);
        UpdateOutputPriceSelectors();
        UpdateWorkforceEditor(recipe);

        if (_pendingLineToLoad != null)
        {
            ApplyPendingWorkforceSettings();
            ApplyPendingPriceSources();
        }

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
    }

    private void UpdateWorkforceEditor(Recipe recipe)
    {
        if (_workforceEditor == null || _sessionManager?.Session?.GameData == null)
            return;

        if (!_sessionManager.Session.GameData.Buildings.TryGetValue(recipe.BuildingId, out var building))
            return;

        _workforceEditor.SetBuildingWorkforce(building.DefaultWorkforce);
    }

    private void ApplyPendingWorkforceSettings()
    {
        if (_pendingLineToLoad == null || _workforceEditor == null)
            return;

        _workforceEditor.SetWorkforceSettings(
            _pendingLineToLoad.WorkforceOverride,
            _pendingLineToLoad.WorkforceConfigMapping);
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
            selector.SetContext(_lineId, input.MaterialId);
            selector.CustomPriceValueChanged += (price) =>
            {
                CustomPriceChanged?.Invoke(_lineId, input.MaterialId, price);
            };
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
            selector.SetContext(_lineId, output.MaterialId);
            selector.CustomPriceValueChanged += (price) =>
            {
                CustomPriceChanged?.Invoke(_lineId, output.MaterialId, price);
            };
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

    public void SetCustomPriceForMaterial(string materialId, decimal price)
    {
        if (_inputPriceSelectors.TryGetValue(materialId, out var inputSelector))
        {
            inputSelector.SetCustomPrice(price);
        }

        if (_outputPriceSelectors.TryGetValue(materialId, out var outputSelector))
        {
            outputSelector.SetCustomPrice(price);
        }
    }

    public void SetProductionLine(ProductionLine line)
    {
        _lineId = line.Id;

        if (_lineIdLabel != null)
        {
            _lineIdLabel.Text = $"ID: {_lineId}";
        }

        if (_nameInput != null)
        {
            _nameInput.Text = line.Name ?? "";
        }

        _pendingLineToLoad = line;

        GD.Print($"ProductionLineEditor: SetProductionLine called for {_lineId}");
        GD.Print($"  OutputOverrides: {line.OutputOverrides?.Count ?? 0} items");
        if (line.OutputOverrides != null)
        {
            foreach (var item in line.OutputOverrides)
            {
                GD.Print($"    - {item.MaterialId}: {item.Quantity}");
            }
        }

        if (_recipeSelector != null)
        {
            _recipeSelector.SelectRecipe(line.RecipeId);
            _efficiencyEditor?.SetModifiers(line.AdditionalEfficiencyModifiers);
        }
    }

    private void ApplyPendingOutputOverrides()
    {
        if (_pendingLineToLoad == null)
        {
            GD.Print("ProductionLineEditor: ApplyPendingOutputOverrides - no pending line");
            return;
        }

        GD.Print($"ProductionLineEditor: ApplyPendingOutputOverrides called");
        GD.Print($"  OutputOverrides to apply: {_pendingLineToLoad.OutputOverrides?.Count ?? 0} items");

        GD.Print($"  _outputOverridesEditor is null: {_outputOverridesEditor == null}");
        if (_outputOverridesEditor != null)
        {
            _outputOverridesEditor.SetItems(_pendingLineToLoad.OutputOverrides);
            GD.Print($"  After SetItems, editor has: {_outputOverridesEditor.GetItems()?.Count ?? 0} items");
        }
    }

    private void ApplyPendingPriceSources()
    {
        if (_pendingLineToLoad == null)
        {
            GD.Print("ProductionLineEditor: ApplyPendingPriceSources - no pending line");
            return;
        }

        GD.Print($"ProductionLineEditor: ApplyPendingPriceSources called");

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

        var name = _nameInput?.Text;
        if (string.IsNullOrWhiteSpace(name))
            name = null;

        return new ProductionLine
        {
            Id = _lineId,
            Name = name,
            RecipeId = recipeId,
            WorkforceOverride = _workforceEditor?.GetWorkforceOverride(),
            WorkforceConfigMapping = _workforceEditor?.GetWorkforceConfigMapping(),
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
        if (_collapseToggleButton != null)
            _collapseToggleButton.Pressed -= OnMainTogglePressed;

        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;

        if (_recipeSelector != null)
            _recipeSelector.RecipeSelected -= OnRecipeSelected;

        if (_groupsButton != null)
            _groupsButton.Pressed -= OnGroupsButtonPressed;
    }
}
