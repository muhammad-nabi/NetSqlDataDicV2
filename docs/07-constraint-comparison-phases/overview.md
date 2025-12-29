# Constraint Comparison Feature

## Problem Statement

The EF Model comparison with Data Dictionary currently only checks type compatibility (e.g., `VARCHAR` maps to `string`). It ignores Fluent API configurations such as:

- `entity.Property(e => e.Name).HasMaxLength(60)`
- `entity.Property(e => e.Price).HasPrecision(18, 2)`
- `entity.Property(e => e.Email).IsRequired()`

This means columns that match in type but differ in constraints (MaxLength, Precision, Scale, Nullability, Primary Key) are incorrectly reported as "Match".

## Solution

Add constraint comparison that detects and displays mismatches for:
- **MaxLength** - String length constraints
- **IsNullable** - Required vs optional
- **Precision** - Decimal total digits
- **Scale** - Decimal decimal places
- **IsPrimaryKey** - Primary key designation

## Feature Requirements

1. Extract all constraint metadata from EF Core model via `IProperty` interface
2. Compare extracted constraints against Data Dictionary values
3. Display new "Constraint Mismatch" status (distinct from "Type Mismatch")
4. Show specific constraint differences in comparison results

## Data Flow

```
DbContext.Model (IModel)
    |
IEntityType.GetProperties() -> IProperty
    |
property.GetMaxLength()
property.GetPrecision()
property.GetScale()
property.IsNullable
entityType.FindPrimaryKey()
    |
EfModelColumnDto (with constraints)
    |
ComparisonService.CompareConstraints()
    |
ComparisonItemViewModel (with ConstraintMismatch status)
    |
UI displays constraint differences
```

## Phase Summary

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer - Extend EfModelColumnDto |
| 2 | Complete | Service layer - Extract constraints from EF metadata |
| 3 | Complete | ViewModel layer - Add ConstraintMismatch status |
| 4 | Complete | Comparison logic - Add constraint comparison |
| 5 | Complete | Controller layer - Update JSON response |
| 6 | Complete | UI layer - Display constraint mismatches |

## Key Design Decisions

1. **Status Priority**: TypeMismatch > ConstraintMismatch > Match
   - If type is incompatible, don't check constraints (meaningless comparison)

2. **Null Handling**:
   - EF `MaxLength = null` with DB `MaxLength = -1` (MAX) treated as match
   - EF without Precision/Scale uses convention defaults (may differ from DB)

3. **Special Type Handling**:
   - **Unicode strings (NVARCHAR, NCHAR, NTEXT)**: SQL Server stores `max_length` in bytes (2 per character); divide by 2 for accurate comparison with EF Core's character count
   - **MONEY/SMALLMONEY types**: Skip precision/scale comparison since these have fixed precision (19,4 / 10,4) that cannot be configured in EF Core Fluent API

4. **Visual Distinction**: Purple badge color for constraint mismatches
