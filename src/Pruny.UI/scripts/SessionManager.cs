using Godot;
using Pruny.Core.Calculation;
using Pruny.Library.Events;
using Pruny.Library.Services;
using Pruny.MarketAPIFetch;
using System.Net.Http;

namespace Pruny.UI;

public partial class SessionManager : Node
{
    private WorkspaceSession? _session;
    private System.Net.Http.HttpClient? _httpClient;
    private DataManager? _dataManager;

    [Signal]
    public delegate void MarketDataRequestedSignalEventHandler(string timestamp, string workspaceId);

    [Signal]
    public delegate void SessionStateChangedSignalEventHandler(bool isInitialized, bool isCalculating, string message);

    [Signal]
    public delegate void WorkspaceModifiedSignalEventHandler(string changeDescription);

    [Signal]
    public delegate void PricesUpdatedSignalEventHandler(int materialCount, string timestamp, string source);

    [Signal]
    public delegate void CalculationErrorSignalEventHandler(string errorMessage);

    public WorkspaceSession? Session => _session;
    public DataManager? DataManager => _dataManager;

    public override void _Ready()
    {
        GD.Print("SessionManager: Initializing...");

        try
        {
            InitializeSession();
            GD.Print("SessionManager: Initialization complete");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SessionManager: Failed to initialize - {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }

    public override void _ExitTree()
    {
        CleanupSession();
    }

    private void InitializeSession()
    {
        var config = AppConfig.Instance;
        config.EnsureDirectoriesExist();

        _httpClient = new System.Net.Http.HttpClient();

        var marketClientOptions = new PrUnPlannerClientOptions
        {
            BaseUrl = config.PrUnPlannerApiUrl,
            ApiKey = config.PrUnPlannerApiKey,
            Timeout = config.ApiTimeout,
            MaxRetries = config.ApiMaxRetries
        };

        var marketDataProvider = new PrUnPlannerClient(_httpClient, marketClientOptions);
        var calculationEngine = new CalculationEngine();

        _session = new WorkspaceSession(calculationEngine, marketDataProvider);

        SubscribeToSessionEvents();

        _dataManager = new DataManager(_session);

        GD.Print($"SessionManager: Session created with API URL: {config.PrUnPlannerApiUrl}");

        LoadGameDataOnStartup();
    }

    private void LoadGameDataOnStartup()
    {
        try
        {
            _dataManager?.LoadGameData();
            GD.Print("SessionManager: Game data loaded on startup");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SessionManager: Failed to load game data on startup - {ex.Message}");
        }
    }

    private void SubscribeToSessionEvents()
    {
        if (_session == null)
            return;

        _session.MarketDataRequested += OnMarketDataRequested;
        _session.SessionStateChanged += OnSessionStateChanged;
        _session.WorkspaceModified += OnWorkspaceModified;
        _session.PricesUpdated += OnPricesUpdated;
        _session.CalculationError += OnCalculationError;

        GD.Print("SessionManager: Subscribed to all session events");
    }

    private void UnsubscribeFromSessionEvents()
    {
        if (_session == null)
            return;

        _session.MarketDataRequested -= OnMarketDataRequested;
        _session.SessionStateChanged -= OnSessionStateChanged;
        _session.WorkspaceModified -= OnWorkspaceModified;
        _session.PricesUpdated -= OnPricesUpdated;
        _session.CalculationError -= OnCalculationError;
    }

    private void CleanupSession()
    {
        UnsubscribeFromSessionEvents();

        _dataManager?.Cleanup();
        _dataManager = null;

        _session?.Close();
        _session = null;

        _httpClient?.Dispose();
        _httpClient = null;

        GD.Print("SessionManager: Cleanup complete");
    }

    private void OnMarketDataRequested(object? sender, MarketDataRequestedEventArgs e)
    {
        var timestampStr = e.Timestamp?.ToString("O") ?? "";
        GD.Print($"SessionManager: Market data requested - Timestamp: {timestampStr}, WorkspaceId: {e.WorkspaceId}");
        EmitSignal(SignalName.MarketDataRequestedSignal, timestampStr, e.WorkspaceId ?? "");
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        GD.Print($"SessionManager: State changed - Initialized: {e.IsInitialized}, Calculating: {e.IsCalculating}, Message: {e.Message}");
        EmitSignal(SignalName.SessionStateChangedSignal, e.IsInitialized, e.IsCalculating, e.Message ?? "");
    }

    private void OnWorkspaceModified(object? sender, WorkspaceModifiedEventArgs e)
    {
        GD.Print($"SessionManager: Workspace modified - {e.Reason}");
        EmitSignal(SignalName.WorkspaceModifiedSignal, e.Reason ?? "");
    }

    private void OnPricesUpdated(object? sender, PricesUpdatedEventArgs e)
    {
        var timestampStr = e.Timestamp.ToString("O");
        GD.Print($"SessionManager: Prices updated - Count: {e.MaterialCount}, Source: {e.Source}");
        EmitSignal(SignalName.PricesUpdatedSignal, e.MaterialCount, timestampStr, e.Source ?? "");
    }

    private void OnCalculationError(object? sender, CalculationErrorEventArgs e)
    {
        var errorMsg = e.ErrorMessage ?? "Unknown error";
        GD.PrintErr($"SessionManager: Calculation error - {errorMsg}");
        if (e.Exception != null)
        {
            GD.PrintErr($"  Exception: {e.Exception.Message}");
        }
        EmitSignal(SignalName.CalculationErrorSignal, errorMsg);
    }
}
