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
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        if (_mainUI == null)
        {
            GD.PrintErr("CalculationsView: Could not find MainUI in parent chain");
        }

        if (_sessionManager?.Session == null)
        {
            GD.PrintErr("CalculationsView: SessionManager or Session not available");
            return;
        }

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
        if (_sessionManager?.Session?.CurrentWorkspace == null)
        {
            SetStatus("No workspace loaded", new Color(1, 0.3f, 0.3f));
            return;
        }

        ClearCalculations();

        var calculations = _sessionManager.Session.Calculations;
        var gameData = _sessionManager.Session.GameData;
        var workspace = _sessionManager.Session.CurrentWorkspace;

        LoadWorkforceConfigs();

        if (calculations.Count == 0)
        {
            SetStatus("No calculations available. Add production lines to see calculations.", new Color(1, 1, 0.3f));
            return;
        }

        var groups = workspace.ProductionLineGroups ?? new List<ProductionLineGroup>();
        var allGroupedLineIds = groups.SelectMany(g => g.ProductionLineIds).ToHashSet();

        foreach (var group in groups)
        {
            var groupSection = CreateGroupSection(group.Name, group.ProductionLineIds, calculations, gameData, workspace);
            if (groupSection != null)
            {
                _calculationsContainer?.AddChild(groupSection);
            }
        }

        var ungroupedLineIds = workspace.ProductionLines
            .Where(pl => !allGroupedLineIds.Contains(pl.Id))
            .Select(pl => pl.Id)
            .ToList();

        if (ungroupedLineIds.Count > 0)
        {
            var ungroupedSection = CreateGroupSection("Ungrouped", ungroupedLineIds, calculations, gameData, workspace);
            if (ungroupedSection != null)
            {
                _calculationsContainer?.AddChild(ungroupedSection);
            }
        }

        SetStatus("");
    }

    private Control? CreateGroupSection(
        string groupName,
        List<string> productionLineIds,
        IReadOnlyDictionary<string, ProductionLineCalculation> calculations,
        Pruny.Library.Models.GameData? gameData,
        Pruny.Library.Models.Workspace workspace)
    {
        var matchingCalculations = new List<(ProductionLine line, ProductionLineCalculation calc, Core.Models.Material material, Recipe recipe, Building? building)>();

        foreach (var lineId in productionLineIds)
        {
            if (!calculations.TryGetValue(lineId, out var unitCost))
                continue;

            var productionLine = workspace.ProductionLines.FirstOrDefault(pl => pl.Id == lineId);
            if (productionLine == null)
                continue;

            if (gameData?.Recipes.TryGetValue(productionLine.RecipeId, out var recipe) != true)
                continue;

            if (gameData?.Materials.TryGetValue(unitCost.MaterialId, out var material) != true)
                continue;

            Building? building = null;
            gameData!.Buildings.TryGetValue(recipe.BuildingId, out building);

            matchingCalculations.Add((productionLine, unitCost, material!, recipe!, building));
        }

        if (matchingCalculations.Count == 0)
            return null;

        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 0);

        var headerRow = new HBoxContainer();

        var expandButton = new Button
        {
            Text = "▶",
            CustomMinimumSize = new Vector2(30, 0)
        };
        headerRow.AddChild(expandButton);

        var groupLabel = new Label
        {
            Text = $"=== {groupName} ({matchingCalculations.Count}) ===",
        };
        groupLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1.0f, 0.5f));
        headerRow.AddChild(groupLabel);

        container.AddChild(headerRow);

        var contentContainer = new VBoxContainer();
        contentContainer.Visible = false;

        foreach (var (line, calc, material, recipe, building) in matchingCalculations)
        {
            var calculationItem = CreateCalculationItem(line, calc, material, recipe, building);
            contentContainer.AddChild(calculationItem);
        }

        container.AddChild(contentContainer);

        expandButton.Pressed += () =>
        {
            contentContainer.Visible = !contentContainer.Visible;
            expandButton.Text = contentContainer.Visible ? "▼" : "▶";
        };

        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 10);
        container.AddChild(spacer);

        return container;
    }

    private Control CreateCalculationItem(ProductionLine productionLine, ProductionLineCalculation unitCost, Core.Models.Material material, Recipe recipe, Building? building)
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

        var lineNameLabel = new Label();
        lineNameLabel.Text = string.IsNullOrWhiteSpace(productionLine.Name)
            ? productionLine.Id
            : productionLine.Name;
        lineNameLabel.CustomMinimumSize = new Vector2(150, 0);
        summaryBox.AddChild(lineNameLabel);

        var materialLabel = new Label();
        materialLabel.Text = material.Name;
        materialLabel.CustomMinimumSize = new Vector2(150, 0);
        summaryBox.AddChild(materialLabel);

        var recipeLabel = new Label();
        recipeLabel.Text = $"{recipe.Id} ({building?.Id ?? "Unknown"})";
        recipeLabel.CustomMinimumSize = new Vector2(200, 0);
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

        var lineDisplayName = string.IsNullOrWhiteSpace(productionLine.Name)
            ? productionLine.Id
            : $"{productionLine.Name} ({productionLine.Id})";
        detailsBox.AddChild(CreateDetailLabel($"Production Line: {lineDisplayName}"));
        detailsBox.AddChild(CreateDetailLabel($"Material: {material.Name} ({material.Id})"));
        detailsBox.AddChild(CreateDetailLabel($"Recipe: {recipe.Id}"));
        detailsBox.AddChild(CreateDetailLabel($"Building: {building?.Id ?? "Unknown"}"));
        detailsBox.AddChild(CreateDetailLabel(""));

        AddProductionMetrics(detailsBox, productionLine, recipe, unitCost);

        detailsBox.AddChild(CreateDetailLabel(""));
        detailsBox.AddChild(CreateDetailLabel("Cost Breakdown:"));
        detailsBox.AddChild(CreateDetailLabel($"  Input Costs: {unitCost.InputCosts:F4}"));
        detailsBox.AddChild(CreateDetailLabel($"  Workforce Cost: {unitCost.WorkforceCost:F4}"));

        AddWorkforceDetails(detailsBox, productionLine, building);

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

    private void LoadWorkforceConfigs()
    {
        if (_sessionManager?.Session?.CurrentWorkspace == null)
            return;

        var workforceConfigs = _sessionManager.Session.CurrentWorkspace.WorkforceConfigs;
        if (workforceConfigs == null || workforceConfigs.Count == 0)
            return;

        var sectionTitle = new Label();
        sectionTitle.Text = "=== Workforce Configurations ===";
        sectionTitle.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 0.5f));
        _calculationsContainer?.AddChild(sectionTitle);

        var spacer1 = new Control();
        spacer1.CustomMinimumSize = new Vector2(0, 5);
        _calculationsContainer?.AddChild(spacer1);

        foreach (var (configName, config) in workforceConfigs)
        {
            var workforceItem = CreateWorkforceConfigItem(config);
            _calculationsContainer?.AddChild(workforceItem);
        }

        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, 10);
        _calculationsContainer?.AddChild(spacer2);

        var productionLinesTitle = new Label();
        productionLinesTitle.Text = "=== Production Lines ===";
        productionLinesTitle.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 0.5f));
        _calculationsContainer?.AddChild(productionLinesTitle);

        var spacer3 = new Control();
        spacer3.CustomMinimumSize = new Vector2(0, 5);
        _calculationsContainer?.AddChild(spacer3);
    }

    private Control CreateWorkforceConfigItem(WorkforceTypeConfig config)
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

        var nameLabel = new Label();
        nameLabel.Text = config.Name;
        nameLabel.CustomMinimumSize = new Vector2(250, 0);
        summaryBox.AddChild(nameLabel);

        var typeLabel = new Label();
        typeLabel.Text = config.WorkforceType.ToString();
        typeLabel.CustomMinimumSize = new Vector2(150, 0);
        summaryBox.AddChild(typeLabel);

        var costPerWorkerLabel = new Label();
        var costPerMinute = CalculateWorkforceConfigCost(config);
        var costPerDay = costPerMinute * 60 * 24;
        costPerWorkerLabel.Text = $"Cost: {costPerDay:F2}/day per worker";
        costPerWorkerLabel.CustomMinimumSize = new Vector2(250, 0);
        summaryBox.AddChild(costPerWorkerLabel);

        var detailsContainer = new VBoxContainer();
        detailsContainer.Visible = false;
        detailsContainer.AddThemeConstantOverride("separation", 5);

        var detailsPanel = new PanelContainer();
        var detailsBox = new VBoxContainer();
        detailsPanel.AddChild(detailsBox);

        detailsBox.AddChild(CreateDetailLabel($"Configuration: {config.Name}"));
        detailsBox.AddChild(CreateDetailLabel($"Workforce Type: {config.WorkforceType}"));
        detailsBox.AddChild(CreateDetailLabel(""));
        detailsBox.AddChild(CreateDetailLabel("Material Consumption (per 100 workers per 24 hours):"));

        foreach (var consumption in config.MaterialConsumption)
        {
            var material = _sessionManager?.Session?.GameData?.Materials.GetValueOrDefault(consumption.MaterialId);
            var materialName = material?.Name ?? consumption.MaterialId;

            var price = ResolveWorkforceMaterialPrice(consumption);
            var totalCostPer100WorkersPer24Hours = consumption.QuantityPer100WorkersPer24Hours * price;

            detailsBox.AddChild(CreateDetailLabel($"  {materialName}: {consumption.QuantityPer100WorkersPer24Hours:F2} units @ {price:F4} = {totalCostPer100WorkersPer24Hours:F4}"));
        }

        detailsBox.AddChild(CreateDetailLabel(""));
        detailsBox.AddChild(CreateDetailLabel($"Cost per worker per minute: {costPerMinute:F6}"));
        detailsBox.AddChild(CreateDetailLabel($"Cost per worker per day: {costPerDay:F4}"));

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

    private decimal CalculateWorkforceConfigCost(WorkforceTypeConfig config)
    {
        decimal totalCostPer100WorkersPer24Hours = 0;

        foreach (var consumption in config.MaterialConsumption)
        {
            var price = ResolveWorkforceMaterialPrice(consumption);
            totalCostPer100WorkersPer24Hours += consumption.QuantityPer100WorkersPer24Hours * price;
        }

        return totalCostPer100WorkersPer24Hours / 100m / (24m * 60m);
    }

    private decimal ResolveWorkforceMaterialPrice(WorkforceMaterialConsumption consumption)
    {
        if (_sessionManager?.Session?.PriceRegistry == null)
        {
            return 0;
        }

        var priceRegistry = _sessionManager.Session.PriceRegistry;

        try
        {
            var price = priceRegistry.GetPrice(consumption.MaterialId, consumption.PriceSource);
            return price;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CalculationsView: Error getting price for {consumption.MaterialId}: {ex.Message}");
            return 0;
        }
    }

    private void AddProductionMetrics(VBoxContainer detailsBox, ProductionLine productionLine, Recipe recipe, ProductionLineCalculation unitCost)
    {
        var runsPerDay = (24m * 60m) / unitCost.AdjustedDurationMinutes;

        detailsBox.AddChild(CreateDetailLabel("Production Metrics:"));
        detailsBox.AddChild(CreateDetailLabel($"  Base Duration: {FormatDuration(recipe.DurationMinutes)}"));
        detailsBox.AddChild(CreateDetailLabel($"  Adjusted Duration: {FormatDuration(unitCost.AdjustedDurationMinutes)} (with {unitCost.OverallEfficiency:P2} efficiency)"));
        detailsBox.AddChild(CreateDetailLabel($"  Runs per day: {runsPerDay:F2}"));
        detailsBox.AddChild(CreateDetailLabel(""));

        detailsBox.AddChild(CreateDetailLabel("Yields:"));

        var material = _sessionManager?.Session?.GameData?.Materials.GetValueOrDefault(unitCost.MaterialId);
        var materialName = material?.Name ?? unitCost.MaterialId;
        var yieldPerDay = unitCost.OutputQuantity * runsPerDay;

        detailsBox.AddChild(CreateDetailLabel($"  {materialName}: {unitCost.OutputQuantity:F2} per run, {yieldPerDay:F2} per day"));
    }

    private string FormatDuration(decimal minutes)
    {
        var hours = (int)(minutes / 60m);
        var remainingMinutes = minutes % 60m;

        if (hours > 0)
        {
            return $"{hours}:{remainingMinutes:00.00}";
        }
        else
        {
            return $"0:{remainingMinutes:00.00}";
        }
    }

    private void AddWorkforceDetails(VBoxContainer detailsBox, ProductionLine productionLine, Building? building)
    {
        if (building == null || _sessionManager?.Session?.CurrentWorkspace == null)
            return;

        var workforce = productionLine.WorkforceOverride ?? building.DefaultWorkforce;
        if (workforce.Count == 0)
            return;

        var workforceConfigs = _sessionManager.Session.CurrentWorkspace.WorkforceConfigs;
        var workforceConfigMapping = productionLine.WorkforceConfigMapping;

        detailsBox.AddChild(CreateDetailLabel("    Workforce:"));

        foreach (var worker in workforce)
        {
            string configName = "Not configured";

            if (workforceConfigMapping != null &&
                workforceConfigMapping.TryGetValue(worker.WorkforceType, out var mappedConfigName))
            {
                configName = mappedConfigName;
            }
            else
            {
                var fallbackConfig = workforceConfigs.Values
                    .FirstOrDefault(c => c.WorkforceType == worker.WorkforceType);
                if (fallbackConfig != null)
                {
                    configName = $"{fallbackConfig.Name} (fallback)";
                }
            }

            detailsBox.AddChild(CreateDetailLabel($"      {worker.Count}x {worker.WorkforceType} → {configName}"));
        }
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
