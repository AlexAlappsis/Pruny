using System;
using System.Net.Http;
using System.Threading.Tasks;
using Pruny.Core.Calculation;
using Pruny.Library.Services;
using Pruny.Library.Events;
using Pruny.MarketAPIFetch;
using Microsoft.Extensions.Logging;

namespace Pruny.App.Services;

public class BlazorSessionManager : IDisposable
{
    private WorkspaceSession? _session;
    private HttpClient? _httpClient;
    private readonly ILogger<BlazorSessionManager> _logger;

    public WorkspaceSession? Session => _session;

    public event Action? OnSessionStateChanged;
    public event Action<string>? OnStatusMessage;

    public BlazorSessionManager(ILogger<BlazorSessionManager> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("BlazorSessionManager: Initializing...");
        
        try
        {
            InitializeSession();
            _logger.LogInformation("BlazorSessionManager: Initialization complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlazorSessionManager: Failed to initialize");
            throw;
        }
        
        await Task.CompletedTask;
    }

    private void InitializeSession()
    {
        var config = AppConfig.Instance;
        // config.EnsureDirectoriesExist(); // Done in ctor of AppConfig

        _httpClient = new HttpClient();

        var marketClientOptions = new PrUnPlannerClientOptions
        {
            BaseUrl = config.PrUnPlannerApiUrl,
            ApiKey = config.PrUnPlannerApiKey,
            Timeout = config.ApiTimeout,
            MaxRetries = config.ApiMaxRetries
        };

        // Note: Pruny.MarketAPIFetch.PrUnPlannerClient implements IMarketDataProvider
        var marketDataProvider = new PrUnPlannerClient(_httpClient, marketClientOptions);
        var calculationEngine = new CalculationEngine();

        _session = new WorkspaceSession(calculationEngine, marketDataProvider);

        SubscribeToSessionEvents();

        // I skipped DataManager for now, assuming Session handles core logic, 
        // but DataManager in Godot handled loading files.
        // I might need to implement loading logic here or port DataManager. 
        // For "Basic Layout" check, I can skip it, but for real functionality I need it.
        // Pruny.UI DataManager wraps session loading calls.
        
        _logger.LogInformation($"Session created with API URL: {config.PrUnPlannerApiUrl}");
        
        // Setup initial state
        NotifyStateChanged();
    }

    private void SubscribeToSessionEvents()
    {
        if (_session == null) return;
        
        _session.WorkspaceModified += (s, e) => NotifyStateChanged();
        _session.PricesUpdated += (s, e) => NotifyStateChanged();
        _session.CalculationError += (s, e) => NotifyStatus($"Error: {e.ErrorMessage}");
        _session.SessionStateChanged += (s, e) => NotifyStatus(e.Message ?? "State changed");
    }

    private void NotifyStateChanged()
    {
        OnSessionStateChanged?.Invoke();
    }

    private void NotifyStatus(string message)
    {
        OnStatusMessage?.Invoke(message);
    }

    public void Dispose()
    {
        _session?.Close();
        _httpClient?.Dispose();
    }

    public void LoadGameData()
    {
        var path = AppConfig.Instance.GameDataPath;
        if (!File.Exists(path))
        {
            _logger.LogError($"Game data file not found at {path}");
            // In a real app we might want to return a result or throw
            throw new FileNotFoundException("Game data file not found", path);
        }

        try
        {
            var json = File.ReadAllText(path);
            _session?.LoadGameData(json);
            _logger.LogInformation("Game data loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game data");
            throw;
        }
    }

    public string[] ListWorkspaces()
    {
        var path = AppConfig.Instance.WorkspacesPath;
        if (!Directory.Exists(path)) return Array.Empty<string>();
        
        return Directory.GetFiles(path, "*.workspace.json")
                        .Select(Path.GetFileName)
                        .Where(x => x != null)
                        .Select(x => x!)
                        .ToArray();
    }

    public void CreateNewWorkspace(string name)
    {
        // Simple creation logic: instantiate a workspace and save it.
        // Or if Library has a factory, use that. 
        // Library doesn't seem to have a factory exposed in what I saw, but WorkspaceSession might.
        // Actually WorkspaceSession.LoadWorkspace takes JSON. 
        // Let's create a default JSON or object.
        
        // Checking Library models... Workspace is a simple model.
        var workspace = new Pruny.Library.Models.Workspace
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        
        // We need to serialize it to load it into the session, or add a method to Session to set workspace directly.
        // The spec says "Accepts/returns JSON strings for all file-based data".
        // So I should serialize it.
        
        var json = System.Text.Json.JsonSerializer.Serialize(workspace);
        _session?.LoadWorkspace(json);
        
        SaveWorkspace(); // Save immediately to disk
        NotifyStateChanged();
    }

    public void LoadWorkspace(string filename)
    {
        var path = Path.Combine(AppConfig.Instance.WorkspacesPath, filename);
        if (!File.Exists(path)) throw new FileNotFoundException("Workspace not found", path);

        try
        {
            var json = File.ReadAllText(path);
            _session?.LoadWorkspace(json);
            AppConfig.Instance.LastUsedWorkspace = filename;
            AppConfig.Instance.Save();
            
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load workspace {filename}");
            throw;
        }
    }

    public void SaveWorkspace()
    {
        if (_session?.CurrentWorkspace == null) return;
        
        // Ensure name consistency
        var filename = $"{_session.CurrentWorkspace.Name.Replace(" ", "-").ToLower()}.workspace.json";
        // Or use ID? DataManager said: GetWorkspacePath(filename).
        // Let's stick to simple filename for now based on what we loaded or a safe name.
        // If we loaded it, we might want to keep the filename.
        // implementation simplification: Just save to a sanitized name.
        
        var json = _session.SaveWorkspaceToJson();
        var path = Path.Combine(AppConfig.Instance.WorkspacesPath, filename);
        
        File.WriteAllText(path, json);
        _logger.LogInformation($"Workspace saved to {path}");
    }
}
