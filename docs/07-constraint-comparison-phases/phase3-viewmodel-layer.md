# Phase 3: ViewModel Layer - Add ConstraintMismatch Status

## Objective

Add new comparison status and data structures to represent constraint mismatches.

## File to Modify

`src/NetSqlDataDicV2.Web/Models/ViewModels/ComparisonResultViewModel.cs`

## Changes

### 3a. Add ConstraintMismatch to enum (line 68-74)

```csharp
public enum ComparisonStatus
{
    Match,
    MissingInEfModel,
    MissingInDatabase,
    TypeMismatch,
    ConstraintMismatch  // NEW
}
```

### 3b. Add ConstraintMismatchDetail class (after line 75)

```csharp
/// <summary>
/// Represents a single constraint difference between EF model and Data Dictionary.
/// </summary>
public class ConstraintMismatchDetail
{
    public string ConstraintName { get; set; } = string.Empty;
    public string? DatabaseValue { get; set; }
    public string? EfValue { get; set; }

    public string DisplayText =>
        $"{ConstraintName}: DB={DatabaseValue ?? "null"} vs EF={EfValue ?? "null"}";
}
```

### 3c. Update ComparisonItemViewModel (line 37-66)

Add property and computed summary:

```csharp
public class ComparisonItemViewModel
{
    // Existing properties...
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? DatabaseType { get; set; }
    public string? EfClrType { get; set; }
    public string? EfEntityName { get; set; }
    public string? EfPropertyName { get; set; }
    public ComparisonStatus Status { get; set; }
    public string? Notes { get; set; }

    // NEW: Constraint mismatch details
    public List<ConstraintMismatchDetail> ConstraintMismatches { get; set; } = new();

    // NEW: Summary for display
    public string? ConstraintMismatchSummary => ConstraintMismatches.Count > 0
        ? string.Join("; ", ConstraintMismatches.Select(c => c.DisplayText))
        : null;

    public string StatusDisplay => Status switch
    {
        ComparisonStatus.Match => "Match",
        ComparisonStatus.MissingInEfModel => "Missing in EF Model",
        ComparisonStatus.MissingInDatabase => "Missing in Database",
        ComparisonStatus.TypeMismatch => "Type Mismatch",
        ComparisonStatus.ConstraintMismatch => "Constraint Mismatch",  // NEW
        _ => "Unknown"
    };

    public string StatusBadgeClass => Status switch
    {
        ComparisonStatus.Match => "bg-success",
        ComparisonStatus.MissingInEfModel => "bg-warning",
        ComparisonStatus.MissingInDatabase => "bg-danger",
        ComparisonStatus.TypeMismatch => "bg-info",
        ComparisonStatus.ConstraintMismatch => "bg-purple",  // NEW
        _ => "bg-secondary"
    };
}
```

### 3d. Update ComparisonResultViewModel (line 3-23)

Add count property:

```csharp
public class ComparisonResultViewModel
{
    // Existing properties...
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime ComparisonTime { get; set; } = DateTime.UtcNow;
    public List<ComparisonItemViewModel> Items { get; set; } = new();
    public List<SkippedTableViewModel> SkippedTables { get; set; } = new();

    public int TotalItems => Items.Count;
    public int TotalMatches => Items.Count(i => i.Status == ComparisonStatus.Match);
    public int TotalMissingInEf => Items.Count(i => i.Status == ComparisonStatus.MissingInEfModel);
    public int TotalMissingInDb => Items.Count(i => i.Status == ComparisonStatus.MissingInDatabase);
    public int TotalTypeMismatches => Items.Count(i => i.Status == ComparisonStatus.TypeMismatch);
    // NEW
    public int TotalConstraintMismatches => Items.Count(i => i.Status == ComparisonStatus.ConstraintMismatch);
    public int TotalSkippedTables => SkippedTables.Count;
    public int TotalSkippedColumns => SkippedTables.Sum(t => t.ColumnCount);
}
```

## CSS Note

The `bg-purple` class doesn't exist in Bootstrap. Will be added in Phase 6 (UI):

```css
.bg-purple { background-color: #6f42c1 !important; color: white; }
```

## Verification

```bash
dotnet build
```
