# Phase 2: Service Layer

## Status: Complete

## Objective

Modify ComparisonService to detect table-level vs column-level missing items and route them appropriately.

## Files Modified

| File | Change |
|------|--------|
| `Services/ComparisonService.cs` | Add table-level detection, populate SkippedTables |

## Current Logic (Before)

```csharp
// Lines 102-136 in CompareAsync()
foreach (var dict in columnEntries)
{
    var key = $"{dict.SchemaName}.{dict.TableName}.{dict.ColumnName}".ToUpperInvariant();
    processedKeys.Add(key);

    if (efLookup.TryGetValue(key, out var ef))
    {
        // Match or TypeMismatch
    }
    else
    {
        // ALL missing columns go here - no table-level distinction
        results.Add(new ComparisonItemViewModel
        {
            Status = ComparisonStatus.MissingInEfModel,
            Notes = "Column exists in database but not mapped in EF Core model."
        });
    }
}
```

## New Logic (After)

### Step 1: Build set of tables in EF model

Add after building `efLookup` (around line 97):

```csharp
// Build set of tables that exist in EF model (case-insensitive)
var efTableKeys = efColumns
    .Where(e => !string.IsNullOrEmpty(e.TableName))
    .Select(e => $"{e.SchemaName ?? "dbo"}.{e.TableName}".ToUpperInvariant())
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
```

### Step 2: Track skipped table column counts

Add before the main loop:

```csharp
// Track columns per skipped table
var skippedTableCounts = new Dictionary<string, (string Schema, string Table, int Count)>(
    StringComparer.OrdinalIgnoreCase);
```

### Step 3: Modify the comparison loop

Replace the `else` block (lines 125-136):

```csharp
foreach (var dict in columnEntries)
{
    var columnKey = $"{dict.SchemaName}.{dict.TableName}.{dict.ColumnName}".ToUpperInvariant();
    var tableKey = $"{dict.SchemaName}.{dict.TableName}".ToUpperInvariant();

    processedKeys.Add(columnKey);

    // Check if TABLE exists in EF model first
    if (!efTableKeys.Contains(tableKey))
    {
        // Entire table not in EF - add to skipped tables count
        if (skippedTableCounts.TryGetValue(tableKey, out var existing))
        {
            skippedTableCounts[tableKey] = (existing.Schema, existing.Table, existing.Count + 1);
        }
        else
        {
            skippedTableCounts[tableKey] = (dict.SchemaName, dict.TableName, 1);
        }
        continue; // Don't add to main comparison results
    }

    // Table IS in EF - proceed with column-level comparison
    if (efLookup.TryGetValue(columnKey, out var ef))
    {
        var isCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);
        results.Add(new ComparisonItemViewModel
        {
            // ... Match or TypeMismatch (unchanged)
        });
    }
    else
    {
        // Column missing but TABLE is in EF
        results.Add(new ComparisonItemViewModel
        {
            SchemaName = dict.SchemaName,
            TableName = dict.TableName,
            ColumnName = dict.ColumnName,
            DatabaseType = dict.DataType,
            Status = ComparisonStatus.MissingInEfModel,
            Notes = "Column exists in database but not mapped in EF Core model."
        });
    }
}
```

### Step 4: Build SkippedTables list

Add before creating the result (around line 161):

```csharp
// Build skipped tables list from counts
var skippedTables = skippedTableCounts.Values
    .Select(t => new SkippedTableViewModel
    {
        SchemaName = t.Schema,
        TableName = t.Table,
        ColumnCount = t.Count
    })
    .OrderBy(t => t.SchemaName)
    .ThenBy(t => t.TableName)
    .ToList();
```

### Step 5: Include in result

Update the result creation:

```csharp
var result = new ComparisonResultViewModel
{
    DatabaseServer = source.TargetServer,
    DatabaseName = source.TargetDatabase,
    ComparisonTime = DateTime.UtcNow,
    Items = results
        .OrderBy(r => r.Status)
        .ThenBy(r => r.SchemaName)
        .ThenBy(r => r.TableName)
        .ThenBy(r => r.ColumnName)
        .ToList(),
    SkippedTables = skippedTables  // NEW
};
```

### Step 6: Update logging

Update the log message (line 177-179):

```csharp
_logger.LogInformation(
    "Comparison complete (source {Source}): {Matches} matches, {MissingEf} missing in EF, " +
    "{MissingDb} missing in DB, {Mismatches} type mismatches, {SkippedTables} skipped tables ({SkippedColumns} columns)",
    source.Name,
    result.TotalMatches,
    result.TotalMissingInEf,
    result.TotalMissingInDb,
    result.TotalTypeMismatches,
    result.TotalSkippedTables,
    result.TotalSkippedColumns);
```

## Edge Cases Handled

| Edge Case | Behavior |
|-----------|----------|
| Table `dbo.AuditLog` not in DbContext | All its columns go to SkippedTables, none to main results |
| Table `dbo.Users` in DbContext but missing `MiddleName` column | `MiddleName` shows in main results as MissingInEfModel |
| Schema variation (`dbo.X` vs `audit.X`) | Treated as different tables (schema is part of key) |
| Empty Data Dictionary | No skipped tables, empty main results |
| Empty DbContext | All tables go to SkippedTables |

## Verification

After this phase:
- [ ] Project builds successfully
- [ ] Comparison with tables only in DbContext works (no skipped)
- [ ] Comparison with tables only in Data Dictionary works (all skipped)
- [ ] Mixed scenario correctly separates items
- [ ] Logging shows skipped table counts
