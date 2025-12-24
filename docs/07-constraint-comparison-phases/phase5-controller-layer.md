# Phase 5: Controller Layer - Update JSON Response

## Objective

Add constraint mismatch count to the comparison API response.

## File to Modify

`src/NetSqlDataDicV2.Web/Controllers/ComparisonController.cs`

## Current State (CompareSource method, around line 55-67)

```csharp
return Json(new
{
    success = true,
    totalItems = result.TotalItems,
    totalMatches = result.TotalMatches,
    totalMissingInEf = result.TotalMissingInEf,
    totalMissingInDb = result.TotalMissingInDb,
    totalTypeMismatches = result.TotalTypeMismatches,
    items = result.Items,
    skippedTables = result.SkippedTables,
    totalSkippedTables = result.TotalSkippedTables,
    totalSkippedColumns = result.TotalSkippedColumns
});
```

## Changes

Add `totalConstraintMismatches` to the response:

```csharp
return Json(new
{
    success = true,
    totalItems = result.TotalItems,
    totalMatches = result.TotalMatches,
    totalMissingInEf = result.TotalMissingInEf,
    totalMissingInDb = result.TotalMissingInDb,
    totalTypeMismatches = result.TotalTypeMismatches,
    totalConstraintMismatches = result.TotalConstraintMismatches,  // NEW
    items = result.Items,
    skippedTables = result.SkippedTables,
    totalSkippedTables = result.TotalSkippedTables,
    totalSkippedColumns = result.TotalSkippedColumns
});
```

## JSON Response Schema (Updated)

```json
{
    "success": true,
    "totalItems": 150,
    "totalMatches": 120,
    "totalMissingInEf": 10,
    "totalMissingInDb": 5,
    "totalTypeMismatches": 8,
    "totalConstraintMismatches": 7,
    "items": [
        {
            "schemaName": "dbo",
            "tableName": "Users",
            "columnName": "Email",
            "databaseType": "NVARCHAR(100)",
            "efClrType": "string",
            "efEntityName": "User",
            "efPropertyName": "Email",
            "status": 4,
            "notes": "MaxLength: DB=100 vs EF=50",
            "constraintMismatches": [
                {
                    "constraintName": "MaxLength",
                    "databaseValue": "100",
                    "efValue": "50",
                    "displayText": "MaxLength: DB=100 vs EF=50"
                }
            ],
            "constraintMismatchSummary": "MaxLength: DB=100 vs EF=50"
        }
    ],
    "skippedTables": [],
    "totalSkippedTables": 0,
    "totalSkippedColumns": 0
}
```

## Status Values

| Value | Name |
|-------|------|
| 0 | Match |
| 1 | MissingInEfModel |
| 2 | MissingInDatabase |
| 3 | TypeMismatch |
| 4 | ConstraintMismatch (NEW) |

## Verification

```bash
dotnet build
```

Test via browser DevTools:
1. Run comparison
2. Check Network tab for response
3. Verify `totalConstraintMismatches` field exists
