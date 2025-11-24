---
title: Change Log
updated: 2025-11-24
---

<!--
VALIDATION RULES:
- Required title, updated
-->

# Spec Change Log

> Append entries when you make meaningful changes to the architecture or invariants.
> This is for humans *and* agents to understand how the system has evolved.

## Entries

- 2025-11-24 – Completed initial living architecture specification for Pruny.
  - Defined system purpose: production cost calculator for Prosperous Universe game
  - Established three-layer architecture: Pruny.Core (calculation engine), Pruny.Library (orchestration), Pruny.UI (Godot wrapper)
  - Documented key flows: workspace loading, market price updates, production line configuration, unit cost calculation
  - Defined core domain entities: Material, Recipe, Building, ProductionLine, Workspace, PriceSource, WorkforceConfig
  - Identified technology stack: C# (.NET) for Core/Library, Godot 4.x for UI, JSON files for data persistence
  - Documented open questions: Godot file I/O integration, calculation algorithm approach, workspace file format, API polling strategy
