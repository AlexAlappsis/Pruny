using Godot;
using Pruny.Core.Models;

namespace Pruny.UI;

public partial class CalculationsView : CenterContainer
{
    private MainUI? _mainUI;
    private SessionManager? _sessionManager;

    private VBoxContainer? _mainContainer;
    private Label? _titleLabel;
    private ScrollContainer? _scrollContainer;
    private VBoxContainer? _calculationsContainer;
    private Button? _backButton;
    private Label? _statusLabel;

    public override void _Ready()
    {
        GD.Print("CalculationsView: _Ready called");

        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        if (_mainUI == null)
        {
            GD.PrintErr("CalculationsView: Could not find MainUI in parent chain");
        }
        else
        {
            GD.Print("CalculationsView: Found MainUI");
        }

        if (_sessionManager?.Session == null)
        {
            GD.PrintErr("CalculationsView: SessionManager or Session not available");
            return;
        }

        GD.Print("CalculationsView: SessionManager and Session found");

        SetupUI();
        LoadCalculations();
    }

    private void SetupUI()
    {
        _mainContainer = GetNode<VBoxContainer>("MainContainer");
        _titleLabel = GetNode<Label>("MainContainer/TitleLabel");
        _scrollContainer = GetNode<ScrollContainer>("MainContainer/ScrollContainer");
        _calculationsContainer = GetNode<VBoxContainer>("MainContainer/ScrollContainer/CalculationsContainer");
        _backButton = GetNode<Button>("MainContainer/BackButton");
        _statusLabel = GetNode<Label>("MainContainer/StatusLabel");

        _backButton.Pressed += OnBackPressed;
    }

    private void LoadCalculations()
    {
        GD.Print("CalculationsView: LoadCalculations started");

        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            GD.Print("CalculationsView: No workspace loaded");
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        ClearCalculations();

        var calculations = _sessionManager.Session.Calculations;
        var gameData = _sessionManager.Session.GameData;
        var workspace = _sessionManager.Session.CurrentWorkspace;

        GD.Print($"CalculationsView: Found {calculations.Count} calculations");

        if (calculations.Count == 0)
        {
            SetStatus("No calculations available. Add production lines to see calculations.", new Color(1, 1, 0.3f));
            return;
        }

        int itemsAdded = 0;
        foreach (var (materialId, unitCost) in calculations)
        {
            GD.Print($"CalculationsView: Processing materialId: {materialId}, productionLineId: {unitCost.ProductionLineId}");

            var productionLine = workspace.ProductionLines.FirstOrDefault(pl => pl.Id == unitCost.ProductionLineId);
            if (productionLine == null)
            {
                GD.Print($"CalculationsView: Production line not found for {unitCost.ProductionLineId}");
                continue;
            }

            if (gameData?.Recipes.TryGetValue(productionLine.RecipeId, out var recipe) != true)
            {
                GD.Print($"CalculationsView: Recipe not found: {productionLine.RecipeId}");
                continue;
            }

            if (gameData?.Materials.TryGetValue(unitCost.MaterialId, out var material) != true)
            {
                GD.Print($"CalculationsView: Material not found: {unitCost.MaterialId}");
                continue;
            }

            Building? building = null;
            gameData?.Buildings.TryGetValue(recipe.BuildingId, out building);

            GD.Print($"CalculationsView: Creating item for {material.Name}");
            var calculationItem = CreateCalculationItem(unitCost.ProductionLineId, unitCost, material!, recipe!, building);
            _calculationsContainer?.AddChild(calculationItem);
            itemsAdded++;
            GD.Print($"CalculationsView: Item added, total: {itemsAdded}");
        }

        GD.Print($"CalculationsView: Total items added: {itemsAdded}");
        SetStatus("");
    }

    private Control CreateCalculationItem(string lineId, UnitCost unitCost, Core.Models.Material material, Recipe recipe, Building? building)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 0);
        container.CustomMinimumSize = new Vector2(0, 40);

        var summaryPanel = new PanelContainer();
        summaryPanel.CustomMinimumSize = new Vector2(0, 40);
        var summaryBox = new HBoxContainer();
        summaryPanel.AddChild(summaryBox);

        var expandButton = new Button();
        expandButton.Text = "▶";
        expandButton.CustomMinimumSize = new Vector2(30, 0);
        summaryBox.AddChild(expandButton);

        var materialLabel = new Label();
        materialLabel.Text = material.Name;
        materialLabel.CustomMinimumSize = new Vector2(200, 0);
        summaryBox.AddChild(materialLabel);

        var recipeLabel = new Label();
        recipeLabel.Text = $"{recipe.Id} ({building?.Id ?? "Unknown"})";
        recipeLabel.CustomMinimumSize = new Vector2(250, 0);
        summaryBox.AddChild(recipeLabel);

        var costLabel = new Label();
        costLabel.Text = $"Cost: {unitCost.CostPerUnit:F2}";
        costLabel.CustomMinimumSize = new Vector2(150, 0);
        summaryBox.AddChild(costLabel);

        var priceLabel = new Label();
        if (unitCost.OutputPrice.HasValue)
        {
            priceLabel.Text = $"Price: {unitCost.OutputPrice.Value:F2}";
            priceLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 1.0f));
        }
        else
        {
            priceLabel.Text = "Price: N/A";
            priceLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        }
        priceLabel.CustomMinimumSize = new Vector2(150, 0);
        summaryBox.AddChild(priceLabel);

        var profitLabel = new Label();
        if (unitCost.ProfitPer24Hours.HasValue)
        {
            var profit = unitCost.ProfitPer24Hours.Value;
            profitLabel.Text = $"Profit/24h: {profit:F2}";
            profitLabel.AddThemeColorOverride("font_color", profit >= 0
                ? new Color(0.3f, 1.0f, 0.3f)
                : new Color(1.0f, 0.3f, 0.3f));
        }
        else
        {
            profitLabel.Text = "Profit/24h: N/A";
            profitLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        }
        profitLabel.CustomMinimumSize = new Vector2(200, 0);
        summaryBox.AddChild(profitLabel);

        var detailsContainer = new VBoxContainer();
        detailsContainer.Visible = false;
        detailsContainer.AddThemeConstantOverride("separation", 5);

        var detailsPanel = new PanelContainer();
        var detailsBox = new VBoxContainer();
        detailsPanel.AddChild(detailsBox);

        detailsBox.AddChild(CreateDetailLabel($"Production Line ID: {lineId}"));
        detailsBox.AddChild(CreateDetailLabel($"Material: {material.Name} ({material.Id})"));
        detailsBox.AddChild(CreateDetailLabel($"Recipe: {recipe.Id}"));
        detailsBox.AddChild(CreateDetailLabel($"Building: {building?.Id ?? "Unknown"}"));
        detailsBox.AddChild(CreateDetailLabel(""));
        detailsBox.AddChild(CreateDetailLabel("Cost Breakdown:"));
        detailsBox.AddChild(CreateDetailLabel($"  Input Costs: {unitCost.InputCosts:F4}"));
        detailsBox.AddChild(CreateDetailLabel($"  Workforce Cost: {unitCost.WorkforceCost:F4}"));
        detailsBox.AddChild(CreateDetailLabel($"  Total Cost Per Unit: {unitCost.CostPerUnit:F4}"));
        detailsBox.AddChild(CreateDetailLabel($"  Overall Efficiency: {unitCost.OverallEfficiency:P2}"));
        detailsBox.AddChild(CreateDetailLabel(""));
        detailsBox.AddChild(CreateDetailLabel("Profit Metrics:"));

        if (unitCost.OutputPrice.HasValue)
        {
            detailsBox.AddChild(CreateDetailLabel($"  Output Price: {unitCost.OutputPrice.Value:F4}"));
            detailsBox.AddChild(CreateDetailLabel($"  Profit Per Unit: {unitCost.ProfitPerUnit:F4}"));
            detailsBox.AddChild(CreateDetailLabel($"  Profit Per Run: {unitCost.ProfitPerRun:F4}"));
            detailsBox.AddChild(CreateDetailLabel($"  Profit Per 24 Hours: {unitCost.ProfitPer24Hours:F4}"));
        }
        else
        {
            detailsBox.AddChild(CreateDetailLabel("  Output price not set"));
        }

        detailsContainer.AddChild(detailsPanel);

        expandButton.Pressed += () =>
        {
            detailsContainer.Visible = !detailsContainer.Visible;
            expandButton.Text = detailsContainer.Visible ? "▼" : "▶";
        };

        container.AddChild(summaryPanel);
        container.AddChild(detailsContainer);

        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 10);
        container.AddChild(spacer);

        return container;
    }

    private Label CreateDetailLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        return label;
    }

    private void ClearCalculations()
    {
        if (_calculationsContainer == null)
            return;

        foreach (var child in _calculationsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnBackPressed()
    {
        _mainUI?.LoadDashboard();
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
        if (_backButton != null)
            _backButton.Pressed -= OnBackPressed;
    }
}
