# Phase 1: Data Layer - Extend EfModelColumnDto

## Objective

Add constraint properties to `EfModelColumnDto` to hold values extracted from EF Core metadata.

## File to Modify

`src/NetSqlDataDicV2.Web/Models/Dto/EfModelColumnDto.cs`

## Current State

```csharp
public class EfModelColumnDto
{
    public string EntityName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public string? SchemaName { get; set; }
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
}
```

## Changes

Add three new properties to match `DataElement` entity:

```csharp
public class EfModelColumnDto
{
    // Existing properties...
    public string EntityName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public string? SchemaName { get; set; }
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }

    // NEW: Additional constraint properties
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsPrimaryKey { get; set; }
}
```

## Property Alignment

| Property | DataElement | EfModelColumnDto | Notes |
|----------|-------------|------------------|-------|
| MaxLength | int? | int? | Already exists |
| IsNullable | bool | bool | Already exists |
| Precision | int? | int? (NEW) | For decimal types |
| Scale | int? | int? (NEW) | For decimal types |
| IsPrimaryKey | bool | bool (NEW) | Primary key flag |

## Verification

After changes, run:
```bash
dotnet build
```

No other files depend on new properties yet - they're just added for Phase 2.
