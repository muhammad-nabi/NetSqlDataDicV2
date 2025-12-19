# Skipped Tables Feature

Shows tables that exist in Data Dictionary but are completely absent from DbContext (likely excluded for security reasons) in a separate UI section.

## Problem Statement

When comparing Data Dictionary with a DbContext:
- Some tables may be intentionally excluded from DbContext for security reasons
- Currently, all columns from these tables show as "Missing in EF Model"
- This mixes intentional exclusions with actual mapping gaps

## Solution

Separate the comparison results into two grids:
1. **Main Comparison Grid** - Only tables that exist in DbContext (column-level comparison)
2. **Skipped Tables Grid** - Tables in Data Dictionary but completely absent from DbContext

## Phase Overview

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (SkippedTableViewModel, result model update) |
| 2 | Complete | Service layer (table-level detection logic) |
| 3 | Complete | Controller layer (JSON response update) |
| 4 | Complete | UI layer (skipped tables section, DataTable) |

## Key Behavior

| Scenario | Result |
|----------|--------|
| Entire table missing from DbContext | Goes to "Skipped Tables" grid |
| Table in DbContext, column missing | Main grid as "MissingInEfModel" |
| Table in DbContext, not in DB | Main grid as "MissingInDatabase" |
| No tables skipped | Skipped section hidden |
| All tables skipped | Main grid empty, skipped section shows all |

## Data Flow

```
ComparisonService.CompareAsync()
    |
    +---> Build efTableKeys (set of tables in DbContext)
    |
    +---> For each Data Dictionary column:
    |         |
    |         +---> Is table in efTableKeys?
    |                   |
    |                   +-- NO --> Add to SkippedTables (count by table)
    |                   |
    |                   +-- YES --> Column-level comparison (existing logic)
    |
    +---> Return ComparisonResultViewModel with Items + SkippedTables
```

## Files Modified

| File | Phase | Change |
|------|-------|--------|
| `Models/ViewModels/ComparisonResultViewModel.cs` | 1 | Add SkippedTableViewModel, SkippedTables property |
| `Services/ComparisonService.cs` | 2 | Table-level detection, routing logic |
| `Controllers/ComparisonController.cs` | 3 | Include skipped tables in response |
| `Views/Comparison/Index.cshtml` | 4 | Skipped tables section, JavaScript |

## UI Layout (After Implementation)

```
+------------------------------------------+
|  Comparison Configuration    | Summary   |
|  [Source selector]          | Matches: N |
|  [Run Comparison]           | -EF: N     |
|                             | -DB: N     |
|                             | Mismatch: N|
|                             | Skipped: N |
+------------------------------------------+
|  Comparison Results (main grid)          |
|  [Filter buttons: All, Match, -EF, etc.] |
|  +-DataTable----------------------------+|
|  | Status | Table | Column | DB | EF    ||
|  +--------------------------------------+|
+------------------------------------------+
|  Tables Not in DbContext (collapsible)   |
|  N tables, M columns total               |
|  +-DataTable----------------------------+|
|  | Schema | Table Name | Columns        ||
|  +--------------------------------------+|
+------------------------------------------+
```
