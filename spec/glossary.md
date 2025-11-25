---
title: Glossary
updated: 2025-11-25
---

<!--
VALIDATION RULES:
- Required title, updated
-->

# System Glossary

> Define the core vocabulary of the system.
> Keep this tight and focused on terms that matter for humans and agents.

| Term | Definition | Notes / Related |
|------|------------|-----------------|
| `Adjustment` | A percentage or flat modifier applied to a price source | Can be applied to any input or output price |
| `Building` | A production facility type with default workforce configuration and upkeep costs | Examples: Refinery, Farm, Electronics Factory |
| `Material` | A game item/resource in Prosperous Universe | Examples: steel, fuel, components, polymers |
| `PriceSource` | The origin of a material's price | Can be API data, another ProductionLine's unit cost, or custom value |
| `ProductionLine` | User-configured instance of a Recipe with specific efficiency, workforce, and pricing | Represents an actual production setup |
| `Recipe` | A production formula defining inputs, outputs, building type, and duration | Determines what can be produced and how |
| `UnitCost` | The calculated cost to produce one unit of a material | Includes all inputs, workforce costs, and modifiers |
| `Workspace` | A saved configuration containing multiple production lines and pricing choices | Each workspace is a separate scenario |
| `WorkforceConfig` | Defines workforce material costs for a grouping of production lines | Typically organized by planet or region |

## Domain-specific concepts

### Production & Calculation

- **Production Chain:** A series of dependent ProductionLines where outputs of one line become inputs to another. The calculation engine walks these dependencies to compute final UnitCosts.

- **Workforce Cost:** The cost of consumable materials (food, water, etc.) required for workers per production cycle. Based on building's workforce configuration and per-line overrides.

- **Efficiency:** The production effectiveness of a ProductionLine, affected by workforce count as well as other factors. Fewer workers than building default reduces efficiency.

- **Circular Dependency:** When production chains contain cycles (A depends on B, B depends on A). Must be detected and handled gracefully by the calculation engine.

### Data Sources

- **Static Game Data:** Materials, Recipes, Buildings, and workforce definitions loaded from JSON files. Sourced from Prosperous Universe game data.

- **Cached API Data:** Market prices fetched from PrUnPlanner API and persisted locally. Updated manually by user, not on every workspace load.

- **Custom Price:** A user-defined price value for a material, used as an alternative to API or calculated prices.

### System Modules

- **Pruny.Core:** Pure calculation engine with no I/O dependencies. Takes in-memory data and computes unit costs.

- **Pruny.Library:** Orchestration layer for workspace management, price source building, and calculation coordination. Defines IMarketDataProvider interface. Wraps Core.

- **Pruny.MarketAPIFetch:** HTTP client for fetching market prices from PrUnPlanner API. Implements IMarketDataProvider interface.

- **Pruny.UI:** Godot-based desktop UI for configuring production lines and viewing calculations. Handles dependency injection setup and binds to Library layer.

## Naming conventions

- **Material IDs:** Use game-provided string identifiers (e.g., "FE" for iron, "H2O" for water)
- **Workspace files:** JSON format with `.workspace.json` extension
- **Schema versioning:** All workspace files include a `version` field at root level
- **Price adjustments:** Expressed as decimal multipliers (1.15 = +15%) or flat amounts with currency suffix
