using FluentAssertions;
using Pruny.Library.Events;
using Pruny.Library.Models;
using Pruny.Library.Services;

namespace Pruny.Library.Tests;

public class WorkspaceSessionTests
{
    [Fact]
    public void Constructor_CreatesSession_WithDefaultState()
    {
        var session = new WorkspaceSession();

        session.IsInitialized.Should().BeFalse();
        session.IsCalculating.Should().BeFalse();
        session.GameData.Should().BeNull();
        session.CurrentWorkspace.Should().BeNull();
        session.MarketData.Should().BeNull();
        session.Calculations.Should().BeEmpty();
        session.IsDirty.Should().BeFalse();
        session.WorkspaceManager.Should().NotBeNull();
    }

    [Fact]
    public void LoadGameData_WithValidJson_LoadsGameData()
    {
        var session = new WorkspaceSession();
        SessionStateChangedEventArgs? stateChangedArgs = null;
        session.SessionStateChanged += (_, args) => stateChangedArgs = args;

        var gameDataJson = """
        {
          "Materials": {
            "FE": { "Id": "FE", "Name": "Iron" }
          },
          "Recipes": {},
          "Buildings": {}
        }
        """;

        session.LoadGameData(gameDataJson);

        session.GameData.Should().NotBeNull();
        session.GameData!.Materials.Should().ContainKey("FE");
        session.IsInitialized.Should().BeFalse();
        stateChangedArgs.Should().NotBeNull();
        stateChangedArgs!.IsInitialized.Should().BeFalse();
        stateChangedArgs.Message.Should().Be("Game data loaded");
    }

    [Fact]
    public void LoadGameData_WithInvalidJson_ThrowsAndResetsState()
    {
        var session = new WorkspaceSession();

        var act = () => session.LoadGameData("invalid json");

        act.Should().Throw<InvalidDataException>();
        session.GameData.Should().BeNull();
    }

    [Fact]
    public void LoadWorkspace_WithoutGameData_ThrowsInvalidOperationException()
    {
        var session = new WorkspaceSession();
        var workspaceJson = """
        {
          "version": 1,
          "id": "ws-1",
          "name": "Test",
          "createdAt": "2024-01-01T00:00:00Z",
          "lastModifiedAt": "2024-01-01T00:00:00Z",
          "productionLines": [],
          "customPrices": {}
        }
        """;

        var act = () => session.LoadWorkspace(workspaceJson);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Game data must be loaded*");
    }

    [Fact]
    public void LoadWorkspace_WithGameDataAndNoMarketDataTimestamp_LoadsWorkspace()
    {
        var session = CreateSessionWithGameData();
        SessionStateChangedEventArgs? stateChangedArgs = null;
        session.SessionStateChanged += (_, args) => stateChangedArgs = args;

        var workspaceJson = """
        {
          "version": 1,
          "id": "ws-1",
          "name": "Test",
          "createdAt": "2024-01-01T00:00:00Z",
          "lastModifiedAt": "2024-01-01T00:00:00Z",
          "productionLines": [],
          "customPrices": {}
        }
        """;

        session.LoadWorkspace(workspaceJson);

        session.CurrentWorkspace.Should().NotBeNull();
        session.CurrentWorkspace!.Name.Should().Be("Test");
        session.IsInitialized.Should().BeTrue();
        stateChangedArgs.Should().NotBeNull();
        stateChangedArgs!.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void LoadWorkspace_WithMarketDataTimestamp_FiresMarketDataRequestedEvent()
    {
        var session = CreateSessionWithGameData();
        MarketDataRequestedEventArgs? requestedArgs = null;
        session.MarketDataRequested += (_, args) => requestedArgs = args;

        var timestamp = DateTimeOffset.Parse("2025-11-25T14:30:00Z");
        var workspaceJson = $$"""
        {
          "version": 1,
          "id": "ws-1",
          "name": "Test",
          "createdAt": "2024-01-01T00:00:00Z",
          "lastModifiedAt": "2024-01-01T00:00:00Z",
          "marketDataFetchedAt": "{{timestamp:O}}",
          "productionLines": [],
          "customPrices": {}
        }
        """;

        session.LoadWorkspace(workspaceJson);

        requestedArgs.Should().NotBeNull();
        requestedArgs!.Timestamp.Should().Be(timestamp);
        requestedArgs.WorkspaceId.Should().Be("ws-1");
    }

    [Fact]
    public void LoadMarketData_WithValidJson_LoadsDataAndFiresEvent()
    {
        var session = CreateSessionWithGameData();
        PricesUpdatedEventArgs? pricesUpdatedArgs = null;
        session.PricesUpdated += (_, args) => pricesUpdatedArgs = args;

        var marketDataJson = """
        {
          "Prices": [
            {
              "Ticker": "FE",
              "ExchangeCode": "IC1",
              "Average": 100.0,
              "Ask": 105.0,
              "Bid": 95.0
            }
          ],
          "FetchedAt": "2025-11-25T14:30:00Z",
          "Source": "PrUnPlanner API"
        }
        """;

        session.LoadMarketData(marketDataJson);

        session.MarketData.Should().NotBeNull();
        session.MarketData!.Prices.Should().HaveCount(1);
        pricesUpdatedArgs.Should().NotBeNull();
        pricesUpdatedArgs!.MaterialCount.Should().Be(1);
        pricesUpdatedArgs.Source.Should().Be("PrUnPlanner API");
    }

    [Fact]
    public void CreateNewWorkspace_WithGameData_CreatesAndInitializesSession()
    {
        var session = CreateSessionWithGameData();
        SessionStateChangedEventArgs? stateChangedArgs = null;
        session.SessionStateChanged += (_, args) => stateChangedArgs = args;

        var workspace = session.CreateNewWorkspace("My Workspace");

        workspace.Should().NotBeNull();
        workspace.Name.Should().Be("My Workspace");
        session.CurrentWorkspace.Should().BeSameAs(workspace);
        session.IsInitialized.Should().BeTrue();
        stateChangedArgs.Should().NotBeNull();
        stateChangedArgs!.Message.Should().Be("New workspace created");
    }

    [Fact]
    public void CreateNewWorkspace_WithoutGameData_ThrowsInvalidOperationException()
    {
        var session = new WorkspaceSession();

        var act = () => session.CreateNewWorkspace("Test");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Game data must be loaded*");
    }

    [Fact]
    public void Close_ResetsSessionState()
    {
        var session = CreateSessionWithGameData();
        session.CreateNewWorkspace("Test");
        SessionStateChangedEventArgs? stateChangedArgs = null;
        session.SessionStateChanged += (_, args) => stateChangedArgs = args;

        session.Close();

        session.IsInitialized.Should().BeFalse();
        session.GameData.Should().BeNull();
        session.CurrentWorkspace.Should().BeNull();
        session.MarketData.Should().BeNull();
        session.Calculations.Should().BeEmpty();
        stateChangedArgs.Should().NotBeNull();
        stateChangedArgs!.IsInitialized.Should().BeFalse();
        stateChangedArgs.Message.Should().Be("Session closed");
    }

    [Fact]
    public void WorkspaceModified_Event_IsForwardedFromWorkspaceManager()
    {
        var session = CreateSessionWithGameData();
        session.CreateNewWorkspace("Test");
        WorkspaceModifiedEventArgs? modifiedArgs = null;
        session.WorkspaceModified += (_, args) => modifiedArgs = args;

        session.WorkspaceManager.MarkDirty("Test modification");

        modifiedArgs.Should().NotBeNull();
        modifiedArgs!.Reason.Should().Be("Test modification");
    }

    private static WorkspaceSession CreateSessionWithGameData()
    {
        var session = new WorkspaceSession();
        var gameDataJson = """
        {
          "Materials": {
            "FE": { "Id": "FE", "Name": "Iron" }
          },
          "Recipes": {},
          "Buildings": {}
        }
        """;
        session.LoadGameData(gameDataJson);
        return session;
    }
}
