# Phase 1: Data Layer

## Status: Pending

## Objective

Create ViewModel for skipped tables and extend the comparison result model.

## Files Modified

| File | Change |
|------|--------|
| `Models/ViewModels/ComparisonResultViewModel.cs` | Add SkippedTableViewModel class and properties |

## SkippedTableViewModel

Add new class to represent a table that exists in Data Dictionary but not in DbContext:

```csharp
/// <summary>
/// Represents a table that exists in the Data Dictionary but has no
/// corresponding entity in the DbContext (entirely skipped from comparison).
/// </summary>
public class SkippedTableViewModel
{
    /// <summary>
    /// Database schema name (e.g., "dbo").
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Table name without schema prefix.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Number of columns in this table (from Data Dictionary).
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// Fully qualified table name for display: schema.tablename
    /// </summary>
    public string FullTableName => $"{SchemaName}.{TableName}";
}
```

## ComparisonResultViewModel Updates

Add new properties to existing class:

```csharp
public class ComparisonResultViewModel
{
    // ... existing properties ...

    /// <summary>
    /// Tables in Data Dictionary that have zero coverage in DbContext.
    /// These tables exist in the database but no entity maps to them.
    /// </summary>
    public List<SkippedTableViewModel> SkippedTables { get; set; } = new();

    /// <summary>
    /// Count of skipped tables.
    /// </summary>
    public int TotalSkippedTables => SkippedTables.Count;

    /// <summary>
    /// Total columns across all skipped tables.
    /// </summary>
    public int TotalSkippedColumns => SkippedTables.Sum(t => t.ColumnCount);
}
```

## Design Notes

1. **SkippedTableViewModel is simple** - Only needs schema, table name, and column count
2. **No column details** - We don't list individual columns for skipped tables (table-level only)
3. **Computed properties** - TotalSkippedTables and TotalSkippedColumns are computed from the list
4. **FullTableName** - Convenience property for display (`dbo.AuditLogs`)

## Verification

After this phase:
- [ ] Project builds successfully
- [ ] No breaking changes to existing ComparisonResultViewModel consumers
- [ ] New properties have XML documentation
