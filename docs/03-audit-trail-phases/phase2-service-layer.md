# Phase 2: Service Layer

## Objective

Modify the sync service to create audit records during sync operations, and add methods to query audit history.

## Prerequisites

- Phase 1 complete (DataElementAudit entity and migration applied)

## Tasks

### 2.1 Create PropertyChange DTO

**File**: `/src/NetSqlDataDicV2.Web/Models/Dto/PropertyChange.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.Dto;

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
```

This DTO captures individual property changes returned by the updated `UpdateDataElement` method.

---

### 2.2 Modify `UpdateDataElement()` Method

**File**: `/src/NetSqlDataDicV2.Web/Services/DatabaseSyncService.cs`

**Current** (lines 279-293):
```csharp
private static bool UpdateDataElement(DataElement existing, SourceColumnDto col, string serverName, string databaseName)
{
    bool changed = false;
    var newDataType = FormatDataType(col);
    if (existing.DataType != newDataType) { existing.DataType = newDataType; changed = true; }
    // ... other properties
    return changed;
}
```

**New Implementation**:
```csharp
private static List<PropertyChange> UpdateDataElement(DataElement existing, SourceColumnDto col, string serverName, string databaseName)
{
    var changes = new List<PropertyChange>();

    var newDataType = FormatDataType(col);
    if (existing.DataType != newDataType)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.DataType),
            OldValue = existing.DataType,
            NewValue = newDataType
        });
        existing.DataType = newDataType;
    }

    if (existing.MaxLength != col.MaxLength)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.MaxLength),
            OldValue = existing.MaxLength?.ToString(),
            NewValue = col.MaxLength?.ToString()
        });
        existing.MaxLength = col.MaxLength;
    }

    if (existing.Precision != col.Precision)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.Precision),
            OldValue = existing.Precision?.ToString(),
            NewValue = col.Precision?.ToString()
        });
        existing.Precision = col.Precision;
    }

    if (existing.Scale != col.Scale)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.Scale),
            OldValue = existing.Scale?.ToString(),
            NewValue = col.Scale?.ToString()
        });
        existing.Scale = col.Scale;
    }

    if (existing.IsNullable != col.IsNullable)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.IsNullable),
            OldValue = existing.IsNullable.ToString(),
            NewValue = col.IsNullable.ToString()
        });
        existing.IsNullable = col.IsNullable;
    }

    if (existing.IsPrimaryKey != col.IsPrimaryKey)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.IsPrimaryKey),
            OldValue = existing.IsPrimaryKey.ToString(),
            NewValue = col.IsPrimaryKey.ToString()
        });
        existing.IsPrimaryKey = col.IsPrimaryKey;
    }

    if (existing.ForeignKeyTo != col.ForeignKeyTo)
    {
        changes.Add(new PropertyChange
        {
            PropertyName = nameof(DataElement.ForeignKeyTo),
            OldValue = existing.ForeignKeyTo,
            NewValue = col.ForeignKeyTo
        });
        existing.ForeignKeyTo = col.ForeignKeyTo;
    }

    return changes;
}
```

---

### 2.3 Modify `SyncDatabaseAsync()` to Create Audit Records

**File**: `/src/NetSqlDataDicV2.Web/Services/DatabaseSyncService.cs`

Modify the sync loop (lines 64-106) to create audit records:

```csharp
int added = 0, updated = 0, removed = 0;
var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var auditRecords = new List<DataElementAudit>();

// Upsert discovered columns
foreach (var col in sourceColumns)
{
    var key = $"{col.SchemaName}.{col.TableName}.{col.ColumnName}".ToUpperInvariant();
    processedKeys.Add(key);

    if (existingLookup.TryGetValue(key, out var existing))
    {
        var wasDeleted = existing.IsDeleted;
        var changes = UpdateDataElement(existing, col, serverName, databaseName);

        if (wasDeleted)
        {
            // Column was soft-deleted, now restored
            existing.IsDeleted = false;
            existing.LastSyncTime = DateTime.UtcNow;
            existing.LastUpdateTime = DateTime.UtcNow;

            auditRecords.Add(new DataElementAudit
            {
                DataElementId = existing.DataElementId,
                SyncHistoryId = syncHistory.SyncHistoryId,
                ChangeType = "Restored",
                PropertyName = null,
                OldValue = "Deleted",
                NewValue = "Active",
                ChangeTime = DateTime.UtcNow
            });

            // Also record any property changes during restoration
            foreach (var change in changes)
            {
                auditRecords.Add(new DataElementAudit
                {
                    DataElementId = existing.DataElementId,
                    SyncHistoryId = syncHistory.SyncHistoryId,
                    ChangeType = "Modified",
                    PropertyName = change.PropertyName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangeTime = DateTime.UtcNow
                });
            }

            updated++;
        }
        else if (changes.Count > 0)
        {
            // Properties changed
            existing.LastSyncTime = DateTime.UtcNow;
            existing.LastUpdateTime = DateTime.UtcNow;

            foreach (var change in changes)
            {
                auditRecords.Add(new DataElementAudit
                {
                    DataElementId = existing.DataElementId,
                    SyncHistoryId = syncHistory.SyncHistoryId,
                    ChangeType = "Modified",
                    PropertyName = change.PropertyName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangeTime = DateTime.UtcNow
                });
            }

            updated++;
        }
        // else: no changes, no audit record (requirement: skip if no change)
    }
    else
    {
        // Add new column
        var newElement = CreateDataElement(col, serverName, databaseName);
        _context.DataElements.Add(newElement);

        // Create audit record using navigation property (ID assigned after SaveChanges)
        auditRecords.Add(new DataElementAudit
        {
            DataElement = newElement,
            SyncHistoryId = syncHistory.SyncHistoryId,
            ChangeType = "Added",
            PropertyName = null,
            OldValue = null,
            NewValue = FormatDataType(col),
            ChangeTime = DateTime.UtcNow
        });

        added++;
    }
}

// Soft-delete columns no longer in source
foreach (var existing in existingEntries.Where(e => e.ColumnName != null && !e.IsDeleted))
{
    var key = $"{existing.SchemaName}.{existing.TableName}.{existing.ColumnName}".ToUpperInvariant();
    if (!processedKeys.Contains(key))
    {
        existing.IsDeleted = true;
        existing.LastUpdateTime = DateTime.UtcNow;

        auditRecords.Add(new DataElementAudit
        {
            DataElementId = existing.DataElementId,
            SyncHistoryId = syncHistory.SyncHistoryId,
            ChangeType = "Deleted",
            PropertyName = null,
            OldValue = "Active",
            NewValue = "Deleted",
            ChangeTime = DateTime.UtcNow
        });

        removed++;
    }
}

// Add all audit records
_context.DataElementAudits.AddRange(auditRecords);

await _context.SaveChangesAsync(cancellationToken);
```

**Key Points**:
- For new elements, use navigation property `DataElement = newElement` instead of `DataElementId` (EF will resolve the FK after SaveChanges)
- No audit record created if no changes detected (requirement)
- Restored columns get both a "Restored" record AND any "Modified" records for property changes

---

### 2.4 Add Audit Query Methods to Service Interface

**File**: `/src/NetSqlDataDicV2.Web/Services/IDataDictionaryService.cs`

Add new method signatures:

```csharp
Task<List<DataElementAuditViewModel>> GetAuditHistoryAsync(int dataElementId, CancellationToken cancellationToken = default);
```

---

### 2.5 Implement Audit Query in Service

**File**: `/src/NetSqlDataDicV2.Web/Services/DataDictionaryService.cs`

Add implementation:

```csharp
public async Task<List<DataElementAuditViewModel>> GetAuditHistoryAsync(
    int dataElementId,
    CancellationToken cancellationToken = default)
{
    return await _context.DataElementAudits
        .AsNoTracking()
        .Where(a => a.DataElementId == dataElementId)
        .OrderByDescending(a => a.ChangeTime)
        .Select(a => new DataElementAuditViewModel
        {
            DataElementAuditId = a.DataElementAuditId,
            DataElementId = a.DataElementId,
            SyncHistoryId = a.SyncHistoryId,
            ChangeType = a.ChangeType,
            PropertyName = a.PropertyName,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            ChangeTime = a.ChangeTime
        })
        .ToListAsync(cancellationToken);
}
```

---

## Verification

After completing Phase 2:

1. Build succeeds: `dotnet build`
2. Run a sync operation
3. Verify audit records created in database:
   ```sql
   SELECT * FROM DataElementAudits ORDER BY ChangeTime DESC
   ```
4. First sync should show all "Added" records
5. Second sync (with no changes) should create no new audit records
6. Modify a column in source DB, sync again, verify "Modified" records

## Edge Cases Handled

| Scenario | Audit Records Created |
|----------|----------------------|
| First sync (all new) | One "Added" per column |
| No changes | None |
| Property changed | One "Modified" per changed property |
| Column removed | One "Deleted" |
| Column restored | One "Restored" + any "Modified" for property changes |
| Multiple properties changed | Multiple "Modified" records (one per property) |

## Dependencies

- Phase 1 complete
- `DataElementAuditViewModel` (created in Phase 3, but can stub for testing)

## Next Phase

Phase 3: ViewModel Layer - Create DTOs and ViewModels for UI
