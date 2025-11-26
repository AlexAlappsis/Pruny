using Godot;
using Pruny.Library.Events;
using Pruny.Library.Models;
using Pruny.Library.Services;
using System.Text.Json;

namespace Pruny.UI;

public class DataManager
{
    private readonly WorkspaceSession _session;
    private readonly AppConfig _config;

    public DataManager(WorkspaceSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _config = AppConfig.Instance;

        _session.MarketDataRequested += OnMarketDataRequested;
    }

    public void LoadGameData()
    {
        var gameDataJson = FileIOManager.LoadTextFile(_config.GameDataPath);

        if (gameDataJson == null)
        {
            GD.PrintErr($"DataManager: Game data file not found at {_config.GameDataPath}");
            throw new FileNotFoundException($"Game data file not found: {_config.GameDataPath}");
        }

        try
        {
            _session.LoadGameData(gameDataJson);
            GD.Print("DataManager: Game data loaded successfully");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataManager: Failed to load game data - {ex.Message}");
            throw;
        }
    }

    public void LoadWorkspace(string filename)
    {
        var workspacePath = GetWorkspacePath(filename);
        var workspaceJson = FileIOManager.LoadTextFile(workspacePath);

        if (workspaceJson == null)
        {
            throw new FileNotFoundException($"Workspace file not found: {workspacePath}");
        }

        try
        {
            _session.LoadWorkspace(workspaceJson);
            GD.Print($"DataManager: Workspace loaded successfully - {filename}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataManager: Failed to load workspace - {ex.Message}");
            throw;
        }
    }

    public void SaveWorkspace(string filename)
    {
        try
        {
            var workspaceJson = _session.SaveWorkspaceToJson();
            var workspacePath = GetWorkspacePath(filename);

            FileIOManager.SaveTextFile(workspacePath, workspaceJson);
            GD.Print($"DataManager: Workspace saved successfully - {filename}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataManager: Failed to save workspace - {ex.Message}");
            throw;
        }
    }

    public string[] ListWorkspaces()
    {
        var workspaces = FileIOManager.ListFiles(_config.WorkspacesPath, "*.workspace.json");
        return workspaces;
    }

    public void SaveMarketData(MarketPriceData data)
    {
        try
        {
            var timestamp = data.FetchedAt.ToLocalTime();
            var filename = $"market-data-{timestamp:yyyy-MM-ddTHH-mm-ss}.json";
            var marketDataPath = GetMarketDataPath(filename);

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            FileIOManager.SaveTextFile(marketDataPath, json);
            GD.Print($"DataManager: Market data saved - {filename}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataManager: Failed to save market data - {ex.Message}");
            throw;
        }
    }

    private void OnMarketDataRequested(object? sender, MarketDataRequestedEventArgs e)
    {
        if (!e.Timestamp.HasValue)
        {
            GD.Print("DataManager: No market data timestamp specified, skipping market data load");
            return;
        }

        try
        {
            var timestamp = e.Timestamp.Value.ToLocalTime();
            var filename = $"market-data-{timestamp:yyyy-MM-ddTHH-mm-ss}.json";
            var marketDataPath = GetMarketDataPath(filename);

            var marketDataJson = FileIOManager.LoadTextFile(marketDataPath);

            if (marketDataJson == null)
            {
                GD.PrintErr($"DataManager: Market data file not found - {filename}");
                GD.PrintErr($"  Workspace will load without market data");
                return;
            }

            _session.LoadMarketData(marketDataJson);
            GD.Print($"DataManager: Market data loaded successfully - {filename}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataManager: Failed to load market data - {ex.Message}");
            GD.PrintErr($"  Workspace will continue without market data");
        }
    }

    private string GetWorkspacePath(string filename)
    {
        if (!filename.EndsWith(".workspace.json"))
        {
            filename += ".workspace.json";
        }

        return _config.WorkspacesPath.PathJoin(filename);
    }

    private string GetMarketDataPath(string filename)
    {
        return _config.MarketCachePath.PathJoin(filename);
    }

    public void Cleanup()
    {
        _session.MarketDataRequested -= OnMarketDataRequested;
    }
}
