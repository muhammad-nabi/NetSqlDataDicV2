# Phase 2: Service Layer - Extract Constraints from EF Core

## Objective

Modify `EfModelService.ExtractColumnsFromContext()` to extract Precision, Scale, and IsPrimaryKey from EF Core metadata.

## File to Modify

`src/NetSqlDataDicV2.Web/Services/EfModelService.cs`

## Current State (lines 78-101)

```csharp
foreach (var property in entityType.GetProperties())
{
    if (property.IsShadowProperty() && string.IsNullOrEmpty(property.GetColumnName()))
        continue;

    var columnName = property.GetColumnName();

    if (string.IsNullOrEmpty(columnName))
        continue;

    columns.Add(new EfModelColumnDto
    {
        EntityName = entityType.ClrType.Name,
        PropertyName = property.Name,
        ColumnName = columnName,
        SchemaName = schemaName,
        TableName = tableName,
        ClrType = GetFriendlyTypeName(property.ClrType),
        IsNullable = property.IsNullable,
        MaxLength = property.GetMaxLength()
    });
}
```

## Changes

### Step 1: Get Primary Key before property loop (after line 76)

```csharp
// Get primary key properties for this entity
var primaryKey = entityType.FindPrimaryKey();
var primaryKeyPropertyNames = primaryKey?.Properties
    .Select(p => p.Name)
    .ToHashSet(StringComparer.OrdinalIgnoreCase)
    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

### Step 2: Extract constraints in property loop (update the Add call)

```csharp
columns.Add(new EfModelColumnDto
{
    EntityName = entityType.ClrType.Name,
    PropertyName = property.Name,
    ColumnName = columnName,
    SchemaName = schemaName,
    TableName = tableName,
    ClrType = GetFriendlyTypeName(property.ClrType),
    IsNullable = property.IsNullable,
    MaxLength = property.GetMaxLength(),
    // NEW: Extract additional constraints
    Precision = property.GetPrecision().HasValue
        ? (int?)property.GetPrecision().Value
        : null,
    Scale = property.GetScale().HasValue
        ? (int?)property.GetScale().Value
        : null,
    IsPrimaryKey = primaryKeyPropertyNames.Contains(property.Name)
});
```

## EF Core API Reference

| Method | Return Type | Description |
|--------|-------------|-------------|
| `property.GetPrecision()` | `byte?` | Total digits for decimal |
| `property.GetScale()` | `byte?` | Decimal places |
| `entityType.FindPrimaryKey()` | `IKey?` | Primary key definition |
| `key.Properties` | `IReadOnlyList<IProperty>` | Properties in the key |

## Type Conversion Note

EF Core returns `byte?` for Precision/Scale, but `DataElement` uses `int?`. The cast `(int?)byteValue` handles this safely.

## Verification

```bash
dotnet build
```

Add temporary logging to verify extraction:
```csharp
_logger.LogDebug("Property {Name}: Precision={P}, Scale={S}, IsPK={PK}",
    property.Name,
    property.GetPrecision(),
    property.GetScale(),
    primaryKeyPropertyNames.Contains(property.Name));
```
