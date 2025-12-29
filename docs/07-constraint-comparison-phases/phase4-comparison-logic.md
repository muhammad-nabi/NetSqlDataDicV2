# Phase 4: Comparison Logic - Add Constraint Comparison

## Objective

Add constraint comparison logic to `ComparisonService` that detects and reports mismatches.

## File to Modify

`src/NetSqlDataDicV2.Web/Services/ComparisonService.cs`

## Changes

### 4a. Add helper methods (after line 250)

```csharp
private static bool IsStringType(string? sqlType)
{
    if (string.IsNullOrEmpty(sqlType)) return false;
    var upper = sqlType.ToUpperInvariant();
    return upper.Contains("CHAR") || upper.Contains("TEXT");
}

private static bool IsUnicodeStringType(string? sqlType)
{
    if (string.IsNullOrEmpty(sqlType)) return false;
    var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
    return baseSqlType is "NVARCHAR" or "NCHAR" or "NTEXT";
}

private static bool IsDecimalType(string? sqlType)
{
    if (string.IsNullOrEmpty(sqlType)) return false;
    var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
    return baseSqlType is "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY";
}

private static bool IsMoneyType(string? sqlType)
{
    if (string.IsNullOrEmpty(sqlType)) return false;
    var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
    return baseSqlType is "MONEY" or "SMALLMONEY";
}
```

### 4b. Add CompareConstraints method (after helper methods)

```csharp
/// <summary>
/// Compares constraints between Data Dictionary and EF model.
/// Returns list of mismatches (empty if all match).
/// </summary>
private List<ConstraintMismatchDetail> CompareConstraints(
    DataElement dict,
    EfModelColumnDto ef)
{
    var mismatches = new List<ConstraintMismatchDetail>();

    // Compare MaxLength (only for string types)
    if (IsStringType(dict.DataType))
    {
        var efMaxLength = ef.MaxLength;
        var dbMaxLength = dict.MaxLength;

        // For Unicode types (NVARCHAR, NCHAR, NTEXT), SQL Server stores max_length in bytes
        // (2 bytes per character), while EF Core uses character count
        if (IsUnicodeStringType(dict.DataType) && dbMaxLength.HasValue && dbMaxLength > 0)
        {
            dbMaxLength = dbMaxLength / 2;
        }

        // EF null with DB -1 (MAX) is considered a match
        bool isMaxLengthMatch = efMaxLength == dbMaxLength
            || (efMaxLength == null && dbMaxLength == -1);

        if (!isMaxLengthMatch)
        {
            mismatches.Add(new ConstraintMismatchDetail
            {
                ConstraintName = "MaxLength",
                DatabaseValue = dbMaxLength == -1 ? "MAX" : dbMaxLength?.ToString() ?? "null",
                EfValue = efMaxLength?.ToString() ?? "MAX"
            });
        }
    }

    // Compare IsNullable
    if (dict.IsNullable != ef.IsNullable)
    {
        mismatches.Add(new ConstraintMismatchDetail
        {
            ConstraintName = "IsNullable",
            DatabaseValue = dict.IsNullable.ToString(),
            EfValue = ef.IsNullable.ToString()
        });
    }

    // Compare Precision/Scale (only for decimal/numeric types, NOT money types)
    // MONEY and SMALLMONEY have fixed precision/scale that can't be configured in EF Core
    if (IsDecimalType(dict.DataType) && !IsMoneyType(dict.DataType))
    {
        if (dict.Precision.HasValue || ef.Precision.HasValue)
        {
            if (dict.Precision != ef.Precision)
            {
                mismatches.Add(new ConstraintMismatchDetail
                {
                    ConstraintName = "Precision",
                    DatabaseValue = dict.Precision?.ToString() ?? "null",
                    EfValue = ef.Precision?.ToString() ?? "default"
                });
            }
        }

        if (dict.Scale.HasValue || ef.Scale.HasValue)
        {
            if (dict.Scale != ef.Scale)
            {
                mismatches.Add(new ConstraintMismatchDetail
                {
                    ConstraintName = "Scale",
                    DatabaseValue = dict.Scale?.ToString() ?? "null",
                    EfValue = ef.Scale?.ToString() ?? "default"
                });
            }
        }
    }

    // Compare IsPrimaryKey
    if (dict.IsPrimaryKey != ef.IsPrimaryKey)
    {
        mismatches.Add(new ConstraintMismatchDetail
        {
            ConstraintName = "IsPrimaryKey",
            DatabaseValue = dict.IsPrimaryKey.ToString(),
            EfValue = ef.IsPrimaryKey.ToString()
        });
    }

    return mismatches;
}
```

### 4c. Update CompareAsync loop (replace lines 136-151)

**Before:**
```csharp
if (efLookup.TryGetValue(columnKey, out var ef))
{
    var isCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);

    results.Add(new ComparisonItemViewModel
    {
        SchemaName = dict.SchemaName,
        TableName = dict.TableName,
        ColumnName = dict.ColumnName,
        DatabaseType = dict.DataType,
        EfClrType = ef.ClrType,
        EfEntityName = ef.EntityName,
        EfPropertyName = ef.PropertyName,
        Status = isCompatible ? ComparisonStatus.Match : ComparisonStatus.TypeMismatch,
        Notes = isCompatible ? null : $"DB type '{dict.DataType}' may not match CLR type '{ef.ClrType}'"
    });
}
```

**After:**
```csharp
if (efLookup.TryGetValue(columnKey, out var ef))
{
    var isTypeCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);
    var constraintMismatches = new List<ConstraintMismatchDetail>();

    // Only check constraints if type is compatible
    if (isTypeCompatible)
    {
        constraintMismatches = CompareConstraints(dict, ef);
    }

    // Determine status (TypeMismatch takes precedence)
    ComparisonStatus status;
    string? notes = null;

    if (!isTypeCompatible)
    {
        status = ComparisonStatus.TypeMismatch;
        notes = $"DB type '{dict.DataType}' may not match CLR type '{ef.ClrType}'";
    }
    else if (constraintMismatches.Count > 0)
    {
        status = ComparisonStatus.ConstraintMismatch;
        notes = string.Join("; ", constraintMismatches.Select(c => c.DisplayText));
    }
    else
    {
        status = ComparisonStatus.Match;
    }

    results.Add(new ComparisonItemViewModel
    {
        SchemaName = dict.SchemaName,
        TableName = dict.TableName,
        ColumnName = dict.ColumnName,
        DatabaseType = dict.DataType,
        EfClrType = ef.ClrType,
        EfEntityName = ef.EntityName,
        EfPropertyName = ef.PropertyName,
        Status = status,
        Notes = notes,
        ConstraintMismatches = constraintMismatches
    });
}
```

## Edge Case Handling

| Case | Handling |
|------|----------|
| EF MaxLength null, DB -1 | Treat as match (both mean MAX) |
| Unicode string MaxLength (NVARCHAR) | DB stores bytes (2 per char), divide by 2 for comparison |
| MONEY/SMALLMONEY precision/scale | Skip comparison (fixed precision can't be configured in EF) |
| EF Precision null | Compare only if DB has value |
| Type mismatch | Skip constraint check (meaningless) |
| Multiple mismatches | All reported in list |

## Verification

```bash
dotnet build
dotnet test
```
