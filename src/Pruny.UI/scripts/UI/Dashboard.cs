using Godot;

namespace Pruny.UI;

public partial class Dashboard : CenterContainer
{
    private MainUI? _mainUI;

    public override void _Ready()
    {
        _mainUI = GetParent()?.GetParent() as MainUI;

        var workspacesButton = GetNode<Button>("VBoxContainer/WorkspacesButton");
        var productionLinesButton = GetNode<Button>("VBoxContainer/ProductionLinesButton");
        var calculationsButton = GetNode<Button>("VBoxContainer/CalculationsButton");
        var refreshMarketButton = GetNode<Button>("VBoxContainer/RefreshMarketButton");
        var settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");

        workspacesButton.Pressed += OnWorkspacesPressed;
        productionLinesButton.Pressed += OnProductionLinesPressed;
        calculationsButton.Pressed += OnCalculationsPressed;
        refreshMarketButton.Pressed += OnRefreshMarketPressed;
        settingsButton.Pressed += OnSettingsPressed;
    }

    private void OnWorkspacesPressed()
    {
        _mainUI?.LoadWorkspaceManager();
    }

    private void OnProductionLinesPressed()
    {
        GD.Print("Production Lines - Coming soon!");
    }

    private void OnCalculationsPressed()
    {
        GD.Print("Calculations View - Coming soon!");
    }

    private async void OnRefreshMarketPressed()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager?.Session == null)
        {
            GD.PrintErr("Dashboard: No session available");
            return;
        }

        try
        {
            GD.Print("Dashboard: Refreshing market data...");
            await sessionManager.Session.RefreshMarketDataFromApiAsync();
            GD.Print("Dashboard: Market data refresh complete");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Dashboard: Failed to refresh market data - {ex.Message}");
        }
    }

    private void OnSettingsPressed()
    {
        GD.Print("Settings - Coming soon!");
    }
}
