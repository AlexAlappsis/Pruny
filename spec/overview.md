---
title: Living Architecture Spec
updated: 2025-01-25
status: draft
---

<!--
VALIDATION RULES:
- Required fields: title, status, updated
- Status values: draft | active | deprecated
-->

# System Overview

> This is the living, narrative description of the system.
> It should stay small, opinionated, and up to date.
> Think 1–3 pages, not a book.

---

## 1. Purpose & Scope

**System name:** Pruny
**Owner(s):** KITD

### 1.1 Problem & context

Pruny solves the production cost calculation problem for players of **Prosperous Universe**, a logistics-focused game. Players need to accurately calculate unit costs for materials across complex production chains, accounting for production efficiency, workforce costs, market prices, and other modifiers.

Currently, players resort to unwieldy spreadsheets that are tedious to update and don't support real-time what-if scenarios. Pruny replaces this with a responsive desktop calculator that recalculates entire production chains instantly when any input changes.

**For whom:** Prosperous Universe players managing production operations
**Environment:** Single-user desktop application

### 1.2 Goals

- [x] Calculate accurate unit costs for materials across multi-tier production chains
- [x] Support real-time what-if scenarios with instant recalculation
- [x] Integrate live market pricing from Prosperous Universe API
- [x] Allow flexible price sourcing (API data, other production lines, custom values)
- [x] Account for workforce costs and percentage/flat modifiers for intangibles
- [x] Persist user configurations (production lines, pricing choices) to disk

### 1.3 Non-goals

- **Not a database system** – All data lives in memory, persisted as JSON files
- **Not a general-purpose optimizer** – May add later, but initial scope is calculation only
- **Not multi-user** – Single-user desktop app; no collaboration features
- **Not a full cost modeling suite** – Focused on production costs; detailed intangible cost calculators are future work

---

## 2. System Boundaries & External Context

### 2.1 External systems

| External system | Direction | Purpose / notes |
|-----------------|-----------|-----------------|
| PrUnPlanner API | in | Fetch current market prices for materials (sourced from Prosperous Universe game data) |
| Local file system | in/out | Load static game data (materials, recipes, buildings, workforce costs) and persist user workspaces (production lines, pricing choices, custom prices) |

### 2.2 User types

- **Production Manager** – The player sets up production lines, configures efficiencies and workforce, assigns price sources, and runs what-if calculations to optimize their operations

---

## 3. Architecture Overview

### 3.1 Modules / components

> This is a lightweight map, not a full-blown component spec.

| Module / area | Responsibility | Notes |
|---------------|----------------|-------|
| **Pruny.Core** | Calculation engine for unit costs, production chains, and dependencies | Pure logic, no I/O; takes in-memory data and computes costs |
| **Pruny.Library** | Orchestration layer for game data loading, API integration, and workspace management | Accepts/returns JSON strings for all file-based data; UI layer handles actual file I/O. Handles HTTP API calls internally. Exposes events for state changes (workspace modified, calculations complete, etc.). |
| **Pruny.UI (Godot)** | Desktop UI for configuring production lines, viewing calculations, and running what-if scenarios | Godot-based frontend; handles file I/O and passes JSON strings to/from Library; subscribes to Library events and wraps them as Godot signals |

### 3.2 Key flows

- **Load Workspace:** User opens or creates a workspace. Library loads static game data (materials, recipes, buildings, workforce costs) from JSON files, loads market prices based on workspace's MarketDataFetchedAt timestamp (supporting historical data analysis), and loads workspace config (production lines, pricing choices). All data goes into memory. Library builds PriceSourceRegistry from market data and workspace custom prices.

- **Update Market Prices:** User manually triggers API data refresh. Library fetches current market prices from PrUnPlanner API and persists them locally. Calculation engine recomputes affected production lines.

- **Configure Production Line:** User defines a production line by selecting recipe (which determines building type), optionally overriding workforce count from building default, and choosing price sources with optional adjustments for all inputs and outputs. For each material, user selects from available price sources: API sources (exchange-specific like "IC1-AVG", "IC1-ASK", "NC1-BID"), production line outputs, or custom prices (user-defined names like "Bulk Price"). Multiple custom price sources can be defined per material. Adjustments can be percentage or flat modifiers applied to any price source. Library validates against game data and persists to workspace file.

- **Calculate Unit Costs:** User triggers or auto-triggers calculation. Library provides PriceSourceRegistry to Core. Core engine walks production dependencies, resolves prices via registry (including workforce material costs, making workforce costs part of the price chain), applies price source adjustments, and computes unit costs across the entire chain. Additionally calculates profit metrics (per-unit, per-run, per-24-hours) based on output price sources. UI updates instantly to show results.

- **What-If Scenario:** User changes a price source, adjustment, or workforce configuration. Calculation engine immediately recomputes affected production lines and downstream dependencies. UI reflects changes in real-time.

### 3.3 Data at a glance

- **Material** – Represents a game item; includes ID, name, base properties
- **Recipe** – Defines inputs/outputs for producing a material; includes building type, duration
- **Building** – Represents production facility; includes type, default workforce configuration (types and counts), upkeep costs
- **ProductionLine** – User-configured instance of a recipe; includes optional workforce override, price sources with adjustments for inputs/outputs, modifiers
- **Workspace** – User's entire configuration; includes multiple production lines, multiple custom price sources per material, pricing choices, workforce cost configurations per planet/grouping, and MarketDataFetchedAt timestamp for historical data support
- **PriceSource** – Identifies a specific price by type and source identifier; API sources use format `{ExchangeCode}-{AVG|ASK|BID}` (e.g., "IC1-AVG"), custom sources use user-defined names (e.g., "Bulk Price"), production line sources use line ID; can have percentage or flat adjustments applied
- **PriceSourceRegistry** – Core's price lookup system; stores all available prices indexed by (materialId, sourceIdentifier); built by Library from market data and workspace custom prices; handles price resolution and adjustment application
- **MarketPrice** – Individual market price entry from API; includes exchange code, ticker, ask/bid/average prices, supply/demand/traded volumes
- **WorkforceConfig** – Defines workforce material consumption for each workforce type (materials consumed per 100 workers per 24 hours with price sources); workforce costs are calculated dynamically as part of the price chain and update when material prices change
- **UnitCost** – Calculated result including cost per unit, workforce cost breakdown, input cost breakdown, efficiency, and profit metrics (per-unit, per-run, per-24-hours) based on output prices

### 3.4 Event model

**Design:** Library exposes standard C# events to notify subscribers of state changes. UI layer subscribes to these events and wraps them as Godot signals for UI consumption.

**Key events:**
- `WorkspaceModified` – Fired when any change requires workspace save (production line edits, custom prices, config changes)
- `ProductionLineRecalculated` – Fired when specific production line(s) complete recalculation; includes which lines changed
- `PricesUpdated` – Fired when market prices refresh from API or custom prices change
- `CalculationError` – Fired when calculation fails (circular dependencies, missing data, etc.)

**Rationale:**
- Library knows the full dependency graph and which production lines are affected by cascading calculations
- Enables reactive UI without polling
- Library remains UI-agnostic (C# events are framework-independent)
- Maps cleanly to Godot's signal system
- Supports future extensibility (logging, undo/redo, analytics)

---

## 4. Technology & Runtime

- **Language / framework:** C# (.NET) for Core and Library layers; Godot 4.x (C#) for UI wrapper
- **Data stores:** JSON files on local file system (no database)
  - Static game data: materials, recipes, buildings, workforce definitions
  - Cached API data: market prices from PrUnPlanner API
  - User workspaces: production lines, pricing choices, custom prices, workforce configs
- **Message buses / queues:** None (single-user, in-memory calculations)
- **Hosting / runtime environment:** Windows/macOS/Linux desktop via Godot runtime

---

## 5. Constraints, Risks & Tradeoffs

### 5.1 Constraints

- **Performance constraint:** All calculations must complete in near real-time (< 100ms) to support instant what-if scenarios. Achieved by keeping all data in memory.
- **No database:** All data must be loadable from JSON files and fit comfortably in memory. Expected dataset size is small enough for this approach.
- **Layer separation:** Core must remain pure logic with no I/O dependencies, allowing it to be reused across different UI frameworks or CLI tools.

### 5.2 Risks

- **Circular production dependencies:** If users create production chains with circular dependencies (A depends on B, B depends on A), the calculation engine must detect and handle this gracefully (error or iterative solver).
- **API availability:** PrUnPlanner API may be unavailable or change format. Need graceful fallback and versioning for cached data.
- **Memory growth:** If game data expands significantly (thousands of materials/recipes), in-memory approach may need optimization or restructuring.

### 5.3 Open questions

- **Calculation engine algorithm:** Should we use recursive dependency resolution, iterative graph traversal, or topological sort? Need to handle circular dependencies.
- **Workspace file format:** Should workspaces be single JSON file or directory of files? How to handle versioning as schema evolves?
- **API polling strategy:** Should we auto-refresh API data on a schedule, or always manual? If manual, how to warn when data is stale?

---

## 6. Links

- [/spec/invariants.json](invariants.json)
- [/spec/glossary.md](glossary.md)
- [/spec/change-log.md](change-log.md)
