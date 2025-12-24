# Phase 2: Service Layer

## Status: Pending

## Objective

Add service method to query audit records by SyncHistoryId and return populated SyncResultsViewModel.

## Files to Modify

| File | Change |
|------|--------|
| `Services/IDatabaseSyncService.cs` | Add interface method signature |
| `Services/DatabaseSyncService.cs` | Add implementation |

## Interface Addition

Add to `IDatabaseSyncService.cs`:

```csharp
/// <summary>
/// Gets detailed sync results including all audit records for a specific sync operation.
/// </summary>
/// <param name="syncHistoryId">The sync history ID to get results for.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>SyncResultsViewModel or null if not found.</returns>
Task<SyncResultsViewModel?> GetSyncResultsAsync(
    int syncHistoryId,
    CancellationToken cancellationToken = default);
```

## Implementation

Add to `DatabaseSyncService.cs`:

```csharp
public async Task<SyncResultsViewModel?> GetSyncResultsAsync(
    int syncHistoryId,
    CancellationToken cancellationToken = default)
{
    // Get sync history record
    var syncHistory = await _context.SyncHistory
        .AsNoTracking()
        .FirstOrDefaultAsync(h => h.SyncHistoryId == syncHistoryId, cancellationToken);

    if (syncHistory == null)
    {
        return null;
    }

    // Get all audit records for this sync with DataElement navigation
    var auditItems = await _context.DataElementAudits
        .AsNoTracking()
        .Include(a => a.DataElement)
        .Where(a => a.SyncHistoryId == syncHistoryId)
        .OrderByDescending(a => a.ChangeTime)
        .ThenBy(a => a.DataElement.SchemaName)
        .ThenBy(a => a.DataElement.TableName)
        .ThenBy(a => a.DataElement.ColumnName)
        .Select(a => new SyncAuditItemViewModel
        {
            DataElementAuditId = a.DataElementAuditId,
            DataElementId = a.DataElementId,
            ChangeType = a.ChangeType,
            PropertyName = a.PropertyName,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            ChangeTime = a.ChangeTime,
            SchemaName = a.DataElement.SchemaName,
            TableName = a.DataElement.TableName,
            ColumnName = a.DataElement.ColumnName ?? string.Empty
        })
        .ToListAsync(cancellationToken);

    // Calculate counts from audit records
    var addedCount = auditItems.Count(a => a.ChangeType == "Added");
    var modifiedCount = auditItems.Count(a => a.ChangeType == "Modified");
    var deletedCount = auditItems.Count(a => a.ChangeType == "Deleted");
    var restoredCount = auditItems.Count(a => a.ChangeType == "Restored");

    return new SyncResultsViewModel
    {
        SyncHistoryId = syncHistory.SyncHistoryId,
        DatabaseServer = syncHistory.DatabaseServer,
        DatabaseName = syncHistory.DatabaseName,
        SyncStartTime = syncHistory.SyncStartTime,
        SyncEndTime = syncHistory.SyncEndTime,
        Status = syncHistory.Status,
        ErrorMessage = syncHistory.ErrorMessage,
        TablesProcessed = syncHistory.TablesProcessed ?? 0,
        ColumnsProcessed = syncHistory.ColumnsProcessed ?? 0,
        AddedCount = addedCount,
        ModifiedCount = modifiedCount,
        DeletedCount = deletedCount,
        RestoredCount = restoredCount,
        AuditItems = auditItems
    };
}
```

## Required Using Statements

Ensure these are at the top of `DatabaseSyncService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Models.ViewModels;
```

## Design Notes

1. **Two queries** - First query gets SyncHistory, second gets audit records with Include
2. **AsNoTracking** - Read-only queries don't need change tracking
3. **Include DataElement** - Gets column location (Schema/Table/Column) in single query
4. **Ordering** - ChangeTime descending (most recent first), then by table/column for grouping
5. **Count calculation** - Uses LINQ on in-memory list (accurate breakdown by type)
6. **Null handling** - Returns null if SyncHistoryId not found

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Invalid syncHistoryId | Return `null` |
| Failed sync (no audit records) | Return ViewModel with empty `AuditItems` |
| Soft-deleted DataElements | Include still works (navigation not query-filtered) |
| Running sync | Returns results so far (SyncEndTime = null) |

## Query Performance

The audit records query:
- Uses index on `SyncHistoryId` (configured in DataElementAuditConfiguration)
- Include eliminates N+1 queries for DataElement
- Select projection avoids loading full entities

## Verification

After this phase:
- [ ] Project builds successfully
- [ ] Method returns null for non-existent syncHistoryId
- [ ] Method returns populated ViewModel for valid syncHistoryId
- [ ] Audit items include correct Schema/Table/Column values
- [ ] Change counts match actual audit record counts
