# Notes: Core

## Decisions

### Whole Unit Rounding for Resource Extraction Buildings - 2026-01-05
Resource extraction buildings (COL, EXT, RIG) require special calculation handling because their outputs must be whole units and their duration adjusts proportionally. User inputs represent 24-hour total yield (from game data). The calculation divides by base runs/day to get per-run quantity, rounds that up to nearest whole number, then calculates additional time based on the delta: `additionalTime = baseDuration × (roundedPerRun - actualPerRun) / actualPerRun`. This ensures the final yield approximates the user's 24-hour input while maintaining whole unit outputs. Implementation is in `CalculationEngine.ApplyWholeUnitRounding()` and is triggered by buildings listed in `GameData.BuildingsRequiringWholeUnitRounding`.

## Patterns

## Constraints

## Open Questions
