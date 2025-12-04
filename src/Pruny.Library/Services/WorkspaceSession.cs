namespace Pruny.Library.Services;

using System.Text.Json;
using Pruny.Core.Calculation;
using Pruny.Core.Models;
using Pruny.Library.Events;
using Pruny.Library.Models;

public class WorkspaceSession
{
    private readonly ICalculationEngine? _calculationEngine;
    private readonly IMarketDataProvider? _marketDataProvider;
    private readonly PriceSourceBuilder _priceSourceBuilder;

    private GameData? _gameData;
    private MarketPriceData? _marketData;
    private PriceSourceRegistry? _priceRegistry;
    private Dictionary<string, UnitCost> _calculations = new();
    private bool _isCalculating;

    public WorkspaceSession(ICalculationEngine? calculationEngine = null, IMarketDataProvider? marketDataProvider = null)
    {
        _calculationEngine = calculationEngine;
        _marketDataProvider = marketDataProvider;
        _priceSourceBuilder = new PriceSourceBuilder();
        WorkspaceManager = new WorkspaceManager();

        WorkspaceManager.WorkspaceModified += (sender, args) => WorkspaceModified?.Invoke(this, args);
    }

    public event EventHandler<MarketDataRequestedEventArgs>? MarketDataRequested;
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;
    public event EventHandler<WorkspaceModifiedEventArgs>? WorkspaceModified;
    public event EventHandler<PricesUpdatedEventArgs>? PricesUpdated;
    public event EventHandler<CalculationErrorEventArgs>? CalculationError;

    public WorkspaceManager WorkspaceManager { get; private set; }

    public bool IsInitialized => _gameData != null && WorkspaceManager.CurrentWorkspace != null;

    public bool IsCalculating => _isCalculating;

    public GameData? GameData => _gameData;

    public Workspace? CurrentWorkspace => WorkspaceManager.CurrentWorkspace;

    public MarketPriceData? MarketData => _marketData;

    public IReadOnlyDictionary<string, UnitCost> Calculations => _calculations;

    public bool IsDirty => WorkspaceManager.IsDirty;

    public MarketDataAnalyzer MarketDataAnalyzer => new MarketDataAnalyzer(_marketData);

    public void LoadGameData(string gameDataJson)
    {
        if (string.IsNullOrWhiteSpace(gameDataJson))
            throw new ArgumentException("Game data JSON must not be empty.", nameof(gameDataJson));

        try
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true));

            var gameData = JsonSerializer.Deserialize<GameData>(gameDataJson, options);
            if (gameData == null)
                throw new InvalidDataException("Game data JSON could not be deserialized.");

            if (gameData.Materials == null || gameData.Recipes == null || gameData.Buildings == null)
                throw new InvalidDataException("Game data is missing required fields (Materials, Recipes, Buildings).");

            _gameData = gameData;

            FireSessionStateChanged(IsInitialized, false, "Game data loaded");
        }
        catch (JsonException ex)
        {
            Reset();
            throw new InvalidDataException("Game data JSON is invalid.", ex);
        }
        catch (Exception)
        {
            Reset();
            throw;
        }
    }

    public void LoadWorkspace(string workspaceJson)
    {
        if (_gameData == null)
            throw new InvalidOperationException("Game data must be loaded before loading a workspace.");

        var workspace = WorkspaceManager.LoadWorkspace(workspaceJson);

        if (workspace.MarketDataFetchedAt.HasValue)
        {
            MarketDataRequested?.Invoke(this, new MarketDataRequestedEventArgs
            {
                Timestamp = workspace.MarketDataFetchedAt,
                WorkspaceId = workspace.Id
            });
        }
        else
        {
            BuildPriceRegistryAndCalculate();
        }

        FireSessionStateChanged(IsInitialized, false, "Workspace loaded");
    }

    public void LoadMarketData(string marketDataJson)
    {
        if (string.IsNullOrWhiteSpace(marketDataJson))
            throw new ArgumentException("Market data JSON must not be empty.", nameof(marketDataJson));

        try
        {
            var marketData = JsonSerializer.Deserialize<MarketPriceData>(marketDataJson);
            if (marketData == null)
                throw new InvalidDataException("Market data JSON could not be deserialized.");

            _marketData = marketData;

            PricesUpdated?.Invoke(this, new PricesUpdatedEventArgs
            {
                MaterialCount = marketData.Prices.Count,
                Timestamp = marketData.FetchedAt,
                Source = marketData.Source
            });

            BuildPriceRegistryAndCalculate();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Market data JSON is invalid.", ex);
        }
    }

    public async Task RefreshMarketDataFromApiAsync(CancellationToken cancellationToken = default)
    {
        if (_marketDataProvider == null)
            throw new InvalidOperationException("No IMarketDataProvider is configured for this session.");

        if (WorkspaceManager.CurrentWorkspace == null)
            throw new InvalidOperationException("No workspace is loaded.");

        var marketData = await _marketDataProvider.FetchMarketPricesAsync(cancellationToken);
        _marketData = marketData;

        WorkspaceManager.ApplyChanges(
            ws => ws.MarketDataFetchedAt = marketData.FetchedAt,
            "Market data refreshed from API");

        PricesUpdated?.Invoke(this, new PricesUpdatedEventArgs
        {
            MaterialCount = marketData.Prices.Count,
            Timestamp = marketData.FetchedAt,
            Source = marketData.Source
        });

        BuildPriceRegistryAndCalculate();
    }

    public void RecalculateAll()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Load game data and workspace first.");

        if (_calculationEngine == null)
            throw new InvalidOperationException("No ICalculationEngine is configured for this session.");

        PerformCalculation();
    }

    public void RecalculateProductionLine(string lineId)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Load game data and workspace first.");

        if (_calculationEngine == null)
            throw new InvalidOperationException("No ICalculationEngine is configured for this session.");

        PerformCalculation();
    }

    public Workspace CreateNewWorkspace(string name, string? id = null)
    {
        if (_gameData == null)
            throw new InvalidOperationException("Game data must be loaded before creating a workspace.");

        var workspace = WorkspaceManager.CreateNewWorkspace(name, id);

        BuildPriceRegistryAndCalculate();

        FireSessionStateChanged(IsInitialized, false, "New workspace created");

        return workspace;
    }

    public string SaveWorkspaceToJson()
    {
        return WorkspaceManager.SaveWorkspaceToJson();
    }

    public void UpdateProductionLine(ProductionLine line)
    {
        if (WorkspaceManager.CurrentWorkspace == null)
            throw new InvalidOperationException("No workspace is loaded.");

        WorkspaceManager.ApplyChanges(ws =>
        {
            var existingIndex = ws.ProductionLines.FindIndex(pl => pl.Id == line.Id);
            if (existingIndex >= 0)
                ws.ProductionLines[existingIndex] = line;
            else
                ws.ProductionLines.Add(line);
        }, $"Production line '{line.Id}' updated");

        if (_calculationEngine != null && _priceRegistry != null)
            PerformCalculation();
    }

    public void UpdateCustomPrice(string materialId, string sourceName, decimal price)
    {
        if (WorkspaceManager.CurrentWorkspace == null)
            throw new InvalidOperationException("No workspace is loaded.");

        WorkspaceManager.ApplyChanges(ws =>
        {
            if (!ws.CustomPrices.ContainsKey(materialId))
                ws.CustomPrices[materialId] = new();
            ws.CustomPrices[materialId][sourceName] = price;
        }, $"Custom price for '{materialId}' ({sourceName}) updated");

        BuildPriceRegistryAndCalculate();
    }

    public void Close()
    {
        Reset();
        FireSessionStateChanged(false, false, "Session closed");
    }

    private void BuildPriceRegistryAndCalculate()
    {
        if (_gameData == null || WorkspaceManager.CurrentWorkspace == null)
            return;

        var marketData = _marketData ?? new MarketPriceData
        {
            Prices = new List<MarketPrice>(),
            FetchedAt = DateTimeOffset.UtcNow,
            Source = "Empty"
        };

        _priceRegistry = _priceSourceBuilder.BuildRegistry(marketData, WorkspaceManager.CurrentWorkspace);

        if (_calculationEngine != null)
            PerformCalculation();
    }

    private void PerformCalculation()
    {
        if (_gameData == null || WorkspaceManager.CurrentWorkspace == null || _priceRegistry == null || _calculationEngine == null)
            return;

        _isCalculating = true;
        FireSessionStateChanged(IsInitialized, true, "Calculating");

        try
        {
            var workforceConfigs = WorkspaceManager.CurrentWorkspace.WorkforceConfigs ?? new();

            var result = _calculationEngine.CalculateUnitCosts(
                WorkspaceManager.CurrentWorkspace.ProductionLines,
                _gameData.Recipes,
                _gameData.Buildings,
                workforceConfigs,
                _priceRegistry);

            _calculations = result.UnitCosts;

            foreach (var error in result.Errors)
            {
                CalculationError?.Invoke(this, new CalculationErrorEventArgs
                {
                    ErrorMessage = error
                });
            }

            if (!result.IsSuccess)
            {
                FireSessionStateChanged(IsInitialized, false, "Calculation completed with errors");
            }
            else
            {
                FireSessionStateChanged(IsInitialized, false, "Calculation completed");
            }
        }
        catch (Exception ex)
        {
            CalculationError?.Invoke(this, new CalculationErrorEventArgs
            {
                ErrorMessage = "Calculation failed",
                Exception = ex
            });

            FireSessionStateChanged(IsInitialized, false, "Calculation failed");
        }
        finally
        {
            _isCalculating = false;
        }
    }

    private void FireSessionStateChanged(bool isInitialized, bool isCalculating, string? message)
    {
        SessionStateChanged?.Invoke(this, new SessionStateChangedEventArgs
        {
            IsInitialized = isInitialized,
            IsCalculating = isCalculating,
            Message = message
        });
    }

    private void Reset()
    {
        _gameData = null;
        _marketData = null;
        _priceRegistry = null;
        _calculations.Clear();
        _isCalculating = false;

        WorkspaceManager = new WorkspaceManager();
        WorkspaceManager.WorkspaceModified += (sender, args) => WorkspaceModified?.Invoke(this, args);
    }
}
