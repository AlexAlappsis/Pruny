---
title: Change Log
updated: 2025-11-25
---

<!--
VALIDATION RULES:
- Required title, updated
-->

# Spec Change Log

> Append entries when you make meaningful changes to the architecture or invariants.
> This is for humans *and* agents to understand how the system has evolved.

## Entries

- 2025-11-24 – Completed initial overview specification for Pruny.
  - Defined system purpose: production cost calculator for Prosperous Universe game
  - Established three-layer architecture: Pruny.Core (calculation engine), Pruny.Library (orchestration), Pruny.UI (Godot wrapper)
  - Documented key flows: workspace loading, market price updates, production line configuration, unit cost calculation
  - Defined core domain entities: Material, Recipe, Building, ProductionLine, Workspace, PriceSource, WorkforceConfig
  - Identified technology stack: C# (.NET) for Core/Library, Godot 4.x for UI, JSON files for data persistence
  - Documented open questions: Godot file I/O integration, calculation algorithm approach, workspace file format, API polling strategy

- 2025-11-24 – Added system invariants and glossary.
  - Created 9 invariants covering architecture (layer separation, no I/O in Core, no Godot in Library), performance (< 100ms calculations, in-memory data), data (circular dependency detection, JSON format, workspace versioning), and domain rules (universal price adjustments)
  - Established glossary with 9 core terms, domain concepts (production chains, workforce costs, efficiency), data sources (static game data, cached API data), and naming conventions

- 2025-11-24 - Added profit calculation and clarified workforce costs.
 - Added clarification on workforce costs
 - Added UnitCost calculation, including profit per-unit, per-run,  per-24-hours

- 2025-11-25 – Resolved Godot file I/O strategy.
  - Library will accept/return JSON strings for all file-based data (game data, workspaces, cached API data)
  - UI layer (Godot, tests) handles actual file I/O operations
  - Library remains UI-agnostic while still handling HTTP API calls internally

- 2025-11-25 – Added event system architecture for Library-to-UI communication.
  - Library exposes C# events for workspace changes, production line recalculations, price updates, and errors
  - UI layer subscribes to events and wraps them as Godot signals
  - Event-driven design enables reactive UI without polling and supports cascading calculation notifications

- 2025-11-25 – Updated price source details and historical API data in workspace
  - Workspace will include MarketDataFetchedAt to allow workspaces to use historical API data
  - PriceSourceRegistry in Core to manage multiple price sources per material
  - PriceSourceRegistry used in calculations to find correct price
