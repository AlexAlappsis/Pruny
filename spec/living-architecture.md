---
title: Living Architecture Spec
updated: 2025-11-24
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
| **Pruny.Library** | Orchestration layer for game data loading, API integration, and workspace management | Wraps Core; handles file system I/O, API calls, workspace persistence |
| **Pruny.UI (Godot)** | Desktop UI for configuring production lines, viewing calculations, and running what-if scenarios | Godot-based frontend; binds to Library layer |

### 3.2 Key flows

- **Load Workspace:** User opens or creates a workspace. Library loads static game data (materials, recipes, buildings, workforce costs) from JSON files, loads previously fetched market prices from PrUnPlanner API (persisted locally), and loads workspace config (production lines, pricing choices). All data goes into memory.

- **Update Market Prices:** User manually triggers API data refresh. Library fetches current market prices from PrUnPlanner API and persists them locally. Calculation engine recomputes affected production lines.

- **Configure Production Line:** User defines a production line by selecting recipe (which determines building type), optionally overriding workforce count from building default, and choosing price sources with optional adjustments for all inputs and outputs (API, other production line, or custom value). Adjustments can be percentage or flat modifiers applied to any price source. Library validates against game data and persists to workspace file.

- **Calculate Unit Costs:** User triggers or auto-triggers calculation. Core engine walks production dependencies, applies workforce costs (based on building's workforce configuration and per-line overrides), applies price source adjustments to inputs/outputs, and computes unit costs across the entire chain. UI updates instantly to show results.

- **What-If Scenario:** User changes a price source, adjustment, or workforce configuration. Calculation engine immediately recomputes affected production lines and downstream dependencies. UI reflects changes in real-time.

### 3.3 Data at a glance

- **Material** – Represents a game item; includes ID, name, base properties
- **Recipe** – Defines inputs/outputs for producing a material; includes building type, duration
- **Building** – Represents production facility; includes type, default workforce configuration (types and counts), upkeep costs
- **ProductionLine** – User-configured instance of a recipe; includes optional workforce override, price sources with adjustments for inputs/outputs, modifiers
- **Workspace** – User's entire configuration; includes multiple production lines, custom prices, pricing choices, workforce cost configurations per planet/grouping
- **PriceSource** – Where a material's price comes from; can be API (with cached local data), another production line's unit cost, or custom value; can have percentage or flat adjustments applied
- **WorkforceConfig** – Defines workforce material costs for a grouping of production lines (e.g., per planet); used to calculate per-line workforce costs based on worker counts

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

- **Godot file handling integration:** Godot has its own file system API (e.g., `FileAccess`, resource paths). Need to determine whether Pruny.Library uses standard .NET file I/O (which may require path translation in Godot) or whether the UI layer passes file paths/streams to Library. Could affect portability if Library becomes Godot-dependent.
- **Circular production dependencies:** If users create production chains with circular dependencies (A depends on B, B depends on A), the calculation engine must detect and handle this gracefully (error or iterative solver).
- **API availability:** PrUnPlanner API may be unavailable or change format. Need graceful fallback and versioning for cached data.
- **Memory growth:** If game data expands significantly (thousands of materials/recipes), in-memory approach may need optimization or restructuring.

### 5.3 Open questions

- **Godot file I/O strategy:** Should Library use standard .NET file APIs (and handle Godot path translation in UI), or should UI pass file handles/streams to Library? What's the cleanest separation?
- **Calculation engine algorithm:** Should we use recursive dependency resolution, iterative graph traversal, or topological sort? Need to handle circular dependencies.
- **Workspace file format:** Should workspaces be single JSON file or directory of files? How to handle versioning as schema evolves?
- **API polling strategy:** Should we auto-refresh API data on a schedule, or always manual? If manual, how to warn when data is stale?

---

## 6. Links

- [/spec/invariants.json](invariants.json)
- [/spec/glossary.md](glossary.md)
- [/spec/change-log.md](change-log.md)
