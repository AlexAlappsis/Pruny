using Godot;

namespace Pruny.UI;

public partial class Dashboard : CenterContainer
{
    private MainUI? _mainUI;

    public override void _Ready()
    {
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;

        if (_mainUI == null)
        {
            GD.PrintErr("Dashboard: Could not find MainUI in parent chain");
        }
        else
        {
            GD.Print("Dashboard: Successfully found MainUI");
        }

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
        if (sessionManager?.Session == null || sessionManager.DataManager == null)
        {
            GD.PrintErr("Dashboard: No session or DataManager available");
            Dialogs.ErrorDialog.Show(this, "No Session",
                "Cannot refresh market data because no session is available.",
                null);
            return;
        }

        try
        {
            GD.Print("Dashboard: Refreshing market data...");
            await sessionManager.Session.RefreshMarketDataFromApiAsync();
            GD.Print("Dashboard: Market data refresh complete");

            if (sessionManager.Session.MarketData != null)
            {
                sessionManager.DataManager.SaveMarketData(sessionManager.Session.MarketData);
                GD.Print("Dashboard: Market data saved to disk");
            }
        }
        catch (HttpRequestException ex)
        {
            GD.PrintErr($"Dashboard: Network error while refreshing market data - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Network Error",
                "Failed to connect to the market data API. Please check your internet connection.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            GD.PrintErr($"Dashboard: Authentication error - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Authentication Failed",
                "Failed to authenticate with the market data API. Please check your API key in settings.",
                ex.Message);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Dashboard: Failed to refresh market data - {ex.Message}");
            Dialogs.ErrorDialog.Show(this, "Market Data Refresh Failed",
                "An unexpected error occurred while refreshing market data.",
                $"{ex.GetType().Name}: {ex.Message} \n, {ex.InnerException?.Message}");
        }
    }

    private void OnSettingsPressed()
    {
        _mainUI?.LoadSettings();
    }
}
