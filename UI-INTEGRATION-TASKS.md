# Pruny UI Integration Task List

This document outlines the tasks needed to integrate the Pruny.Library WorkspaceSession orchestrator into the Godot UI layer.

---

## Overview: Happy Path Implementation

The UI needs to:

1. **Create session with dependencies:**
   ```csharp
   var calculationEngine = new CalculationEngine();
   var marketDataProvider = new PrUnPlannerClient(httpClient, options);
   var session = new WorkspaceSession(calculationEngine, marketDataProvider);
   ```

2. **Subscribe to events:**
   ```csharp
   session.MarketDataRequested += OnMarketDataRequested;
   session.SessionStateChanged += OnSessionStateChanged;
   session.PricesUpdated += OnPricesUpdated;
   session.CalculationError += OnCalculationError;
   ```

3. **Load data:**
   ```csharp
   var gameDataJson = File.ReadAllText("game-data.json");
   session.LoadGameData(gameDataJson);

   var workspaceJson = File.ReadAllText("my-workspace.workspace.json");
   session.LoadWorkspace(workspaceJson); // Fires MarketDataRequested
   ```

4. **Handle market data request:**
   ```csharp
   void OnMarketDataRequested(object? sender, MarketDataRequestedEventArgs e)
   {
       var filename = $"market-data-{e.Timestamp:yyyy-MM-ddTHH-mm-ss}.json";
       var marketDataJson = File.ReadAllText(filename);
       session.LoadMarketData(marketDataJson);
   }
   ```

---

## Task Breakdown

### Phase 1: Project Setup & Dependencies

- [ ] **1.1** Add Pruny.Library project reference to Pruny.UI (Godot project)
- [ ] **1.2** Add Pruny.MarketAPIFetch project reference to Pruny.UI
- [ ] **1.3** Add Pruny.Core project reference to Pruny.UI (if not already present)
- [ ] **1.4** Verify all projects build together with Godot

### Phase 2: Dependency Injection Setup

- [ ] **2.1** Create DI container setup in Godot (or manual instantiation pattern)
  - Consider: Use a singleton/autoload for dependency management
  - Or: Manual creation in main scene `_Ready()`

- [ ] **2.2** Instantiate CalculationEngine
  - Location: `src/Pruny.Core/Calculation/CalculationEngine.cs`
  - Check if additional configuration needed

- [ ] **2.3** Configure PrUnPlannerClient
  - Create `PrUnPlannerClientOptions` with API URL and key
  - Create `HttpClient` instance
  - Instantiate `PrUnPlannerClient`

- [ ] **2.4** Create WorkspaceSession instance
  - Pass CalculationEngine and PrUnPlannerClient to constructor
  - Store in accessible location (singleton, main scene, etc.)

### Phase 3: File I/O Layer

- [ ] **3.1** Define file storage structure
  - Game data location: `data/game-data.json` (or similar)
  - Workspace files location: `data/workspaces/` directory
  - Market data cache location: `data/market-cache/` directory

- [ ] **3.2** Implement game data file loading
  - Read `game-data.json` from disk
  - Handle file not found errors
  - Pass JSON string to `session.LoadGameData(json)`

- [ ] **3.3** Implement workspace file loading
  - List available workspace files
  - Read selected workspace file
  - Pass JSON string to `session.LoadWorkspace(json)`

- [ ] **3.4** Implement workspace file saving
  - Call `session.SaveWorkspaceToJson()` to get JSON
  - Write JSON to workspace file
  - Handle save conflicts/overwrite prompts

- [ ] **3.5** Implement market data file loading
  - Construct filename from timestamp: `market-data-{timestamp:yyyy-MM-ddTHH-mm-ss}.json`
  - Read file from market data cache directory
  - Pass JSON string to `session.LoadMarketData(json)`
  - Handle file not found (market data may not exist for that timestamp)

- [ ] **3.6** Implement market data file saving
  - Save `MarketPriceData` to timestamped file when fetched from API
  - Use `FetchedAt` timestamp for filename

### Phase 4: Event Handling & Godot Signals

- [ ] **4.1** Wrap C# events as Godot signals
  - Create wrapper for `MarketDataRequested` → Godot signal
  - Create wrapper for `SessionStateChanged` → Godot signal
  - Create wrapper for `WorkspaceModified` → Godot signal
  - Create wrapper for `PricesUpdated` → Godot signal
  - Create wrapper for `CalculationError` → Godot signal

- [ ] **4.2** Implement MarketDataRequested handler
  - Subscribe to event
  - Extract timestamp from `MarketDataRequestedEventArgs`
  - Construct filename
  - Load file (if exists)
  - Call `session.LoadMarketData(json)`
  - If file doesn't exist: log warning, continue without market data

- [ ] **4.3** Implement SessionStateChanged handler
  - Subscribe to event
  - Update UI state based on `IsInitialized` and `IsCalculating` flags
  - Show loading indicators when calculating
  - Enable/disable UI elements based on initialization state

- [ ] **4.4** Implement WorkspaceModified handler
  - Subscribe to event
  - Update "dirty" indicator in UI (e.g., asterisk in title, unsaved changes warning)
  - Optionally: auto-save or prompt to save

- [ ] **4.5** Implement PricesUpdated handler
  - Subscribe to event
  - Update UI to reflect new prices
  - Show notification of successful price refresh

- [ ] **4.6** Implement CalculationError handler
  - Subscribe to event
  - Display error messages to user
  - Log errors for debugging

### Phase 5: UI Integration

- [ ] **5.1** Application startup flow
  - Load game data on startup
  - Show workspace selection screen
  - Allow "New Workspace" or "Load Workspace"

- [ ] **5.2** New workspace flow
  - Call `session.CreateNewWorkspace(name)`
  - Initialize UI with empty state
  - Allow user to start adding production lines

- [ ] **5.3** Load workspace flow
  - User selects workspace file
  - Load workspace JSON
  - Call `session.LoadWorkspace(json)`
  - Handle `MarketDataRequested` event
  - Wait for initialization complete
  - Update UI with workspace data

- [ ] **5.4** Save workspace flow
  - User clicks "Save" or triggers auto-save
  - Call `session.SaveWorkspaceToJson()`
  - Write JSON to file
  - Clear dirty flag

- [ ] **5.5** Production line CRUD operations
  - Create: Build `ProductionLine` object, call `session.UpdateProductionLine(line)`
  - Update: Modify line, call `session.UpdateProductionLine(line)`
  - Delete: Remove from workspace, call `session.WorkspaceManager.ApplyChanges(...)`
  - List: Read from `session.CurrentWorkspace.ProductionLines`

- [ ] **5.6** Custom price management
  - Add/Edit: Call `session.UpdateCustomPrice(materialId, sourceName, price)`
  - Delete: Call `session.WorkspaceManager.ApplyChanges(...)` to remove from dictionary
  - List: Read from `session.CurrentWorkspace.CustomPrices`

- [ ] **5.7** Market data refresh
  - User clicks "Refresh Market Data"
  - Call `session.RefreshMarketDataFromApiAsync()`
  - Show loading indicator
  - Save fetched data to timestamped file
  - Update workspace's `MarketDataFetchedAt`
  - Update UI with new prices

- [ ] **5.8** Calculation results display
  - Read from `session.Calculations` dictionary
  - Display unit costs for each production line
  - Show profit metrics (per-unit, per-run, per-24-hours)
  - Update in real-time when `SessionStateChanged` fires after calculations

- [ ] **5.9** What-if scenarios
  - User changes price source, adjustment, or workforce
  - Automatically triggers recalculation via `UpdateProductionLine`
  - UI updates instantly with new results

### Phase 6: Error Handling & Edge Cases

- [ ] **6.1** Handle missing game data file
  - Show error message
  - Provide option to download/locate game data
  - Prevent further operations until game data loaded

- [ ] **6.2** Handle corrupt workspace files
  - Catch `InvalidDataException` from `LoadWorkspace`
  - Show user-friendly error message
  - Offer to create new workspace or load different file

- [ ] **6.3** Handle missing market data files
  - When `MarketDataRequested` fires but file doesn't exist
  - Log warning, allow workspace to load without market data
  - Show UI indicator that market data is missing
  - Offer to refresh from API

- [ ] **6.4** Handle API fetch failures
  - Catch exceptions from `RefreshMarketDataFromApiAsync`
  - Show error message (network error, invalid API key, etc.)
  - Allow retry

- [ ] **6.5** Handle calculation errors
  - Display errors from `CalculationError` event
  - Show which production line caused the error
  - Allow user to fix configuration and retry

- [ ] **6.6** Handle missing price sources
  - When calculation needs a price that doesn't exist
  - UI should highlight missing prices
  - Guide user to add custom price or refresh market data

### Phase 7: Configuration & Settings

- [ ] **7.1** Create application settings/configuration
  - PrUnPlanner API URL and key
  - File storage paths (game data, workspaces, market cache)
  - Auto-save preferences
  - Default timeout, retry settings for API

- [ ] **7.2** Settings UI
  - Allow user to configure API settings
  - Allow user to change file storage locations
  - Validate settings before saving

### Phase 8: Testing & Polish

- [ ] **8.1** Manual testing: Full happy path
  - Load game data
  - Create new workspace
  - Add production lines
  - Refresh market data
  - View calculations
  - Save workspace
  - Load workspace
  - Verify market data loads with correct timestamp

- [ ] **8.2** Manual testing: Error scenarios
  - Missing game data
  - Corrupt workspace file
  - Missing market data file
  - API failures
  - Calculation errors

- [ ] **8.3** Performance testing
  - Verify calculations complete in < 100ms (per Invariant #5)
  - Test with large workspaces (many production lines)
  - Monitor memory usage

- [ ] **8.4** UI/UX polish
  - Loading indicators
  - Error messages are clear and actionable
  - Dirty state indicators
  - Auto-save notifications
  - Keyboard shortcuts

---

## Implementation Notes

### File Naming Conventions

**Workspace files:**
- Format: `{workspace-name}.workspace.json`
- Example: `my-factory.workspace.json`

**Market data files:**
- Format: `market-data-{timestamp:yyyy-MM-ddTHH-mm-ss}.json`
- Example: `market-data-2025-11-25T14-30-00.json`
- Timestamp comes from `MarketPriceData.FetchedAt`

### Suggested File Structure

```
data/
  game-data.json
  workspaces/
    my-factory.workspace.json
    test-setup.workspace.json
  market-cache/
    market-data-2025-11-25T14-30-00.json
    market-data-2025-11-24T10-15-00.json
```

### Event Flow Diagram

```
UI: LoadGameData(json)
  → Session: GameData loaded
  → Session: Fires SessionStateChanged(IsInitialized=false)

UI: LoadWorkspace(json)
  → Session: Workspace loaded
  → Session: Fires MarketDataRequested(timestamp)
  → UI: Handler loads market data file
  → UI: Calls LoadMarketData(json)
  → Session: Market data loaded
  → Session: Fires PricesUpdated
  → Session: Builds price registry
  → Session: Runs calculations
  → Session: Fires SessionStateChanged(IsInitialized=true)
  → UI: Updates all displays with calculation results
```

### Critical Paths to Get Right

1. **Event subscription order:** Subscribe to events BEFORE calling Load methods
2. **File I/O error handling:** Always catch and handle file not found, access denied, etc.
3. **Dirty state management:** Ensure all modifications trigger dirty flag
4. **Market data timestamp matching:** Filename format must match exactly
5. **JSON serialization options:** Use compatible options between Library and UI (PascalCase vs camelCase)

---

## Questions to Resolve

- [ ] Should workspaces auto-save on every change, or require explicit save?
- [ ] How to handle multiple users/sessions accessing same workspace file?
- [ ] Should market data files be automatically cleaned up (delete old files)?
- [ ] How should the UI handle upgrading old workspace file versions?
- [ ] Should there be a "read-only" mode for viewing workspaces without editing?

---

## Reference Documentation

- **Spec:** `spec/overview.md` - System architecture and flows
- **Invariants:** `spec/invariants.json` - Hard constraints to follow
- **Glossary:** `spec/glossary.md` - Terminology and naming conventions
- **WorkspaceSession API:** `src/Pruny.Library/Services/WorkspaceSession.cs`
- **Event Args:** `src/Pruny.Library/Events/`
- **Test Examples:** `tests/Pruny.Library.Tests/WorkspaceSessionTests.cs`
