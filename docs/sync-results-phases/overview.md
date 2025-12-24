# Sync Results Feature

Shows detailed results after a Data Dictionary sync operation, including all changes (Added, Modified, Deleted, Restored) on a dedicated page.

## Problem Statement

After a sync operation completes:
- User sees a brief alert with summary statistics
- Page auto-reloads after 2 seconds
- No way to view detailed breakdown of what changed
- Must manually check individual columns to see audit history

## Solution

Create a **Sync Results page** (`/Sync/Results/{syncHistoryId}`) that:
1. Displays summary cards with change counts by type
2. Shows all audit records in a filterable DataTable grid
3. Links to column Details page for deeper investigation
4. Accessible via "View Results" button after sync or from history table

## User Requirements

| Requirement | Decision |
|-------------|----------|
| Post-sync navigation | Stay on sync page, show "View Results" link |
| Results layout | Summary cards at top + single filterable grid |
| History access | Add "View" button to each sync history row |

## Phase Overview

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (SyncResultsViewModel, SyncAuditItemViewModel) |
| 2 | Complete | Service layer (GetSyncResultsAsync method) |
| 3 | Complete | Controller layer (Results action) |
| 4 | Complete | UI layer (Results.cshtml with summary cards + DataTable) |
| 5 | Complete | Update sync page (View Results buttons) |

## Data Flow

```
User clicks "View" or completes sync
    |
    +---> GET /Sync/Results/{syncHistoryId}
    |
    +---> SyncController.Results(id)
    |         |
    |         +---> DatabaseSyncService.GetSyncResultsAsync(id)
    |                   |
    |                   +---> Query SyncHistory by ID
    |                   |
    |                   +---> Query DataElementAudits where SyncHistoryId = id
    |                   |         Include(DataElement) for column location
    |                   |
    |                   +---> Calculate counts by ChangeType
    |                   |
    |                   +---> Return SyncResultsViewModel
    |
    +---> Render Results.cshtml
              |
              +---> Summary cards (Added, Modified, Deleted, Restored, Total)
              |
              +---> Filter buttons (All, Added, Modified, Deleted, Restored)
              |
              +---> DataTable grid with all audit records
```

## Files Overview

| File | Phase | Change |
|------|-------|--------|
| `Models/ViewModels/SyncResultsViewModel.cs` | 1 | Create - ViewModel with audit items |
| `Services/IDatabaseSyncService.cs` | 2 | Modify - Add interface method |
| `Services/DatabaseSyncService.cs` | 2 | Modify - Add implementation |
| `Controllers/SyncController.cs` | 3 | Modify - Add Results action |
| `Views/Sync/Results.cshtml` | 4 | Create - Results page view |
| `Views/Sync/Index.cshtml` | 5 | Modify - Add View Results buttons |

## UI Layout

```
+------------------------------------------+
|  Sync Results                    [Back]  |
|  server/database | 2024-01-15 10:30:00   |
|  Duration: 3.2s                          |
+------------------------------------------+
|  [Added: 5] [Modified: 12] [Deleted: 2]  |
|  [Restored: 1] [Total: 20]               |
+------------------------------------------+
|  Changes                                 |
|  [All] [Added] [Modified] [Deleted] ... |
|  +-DataTable--------------------------+ |
|  | Type | Schema | Table | Column | ...||
|  +------------------------------------+ |
+------------------------------------------+
```

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Sync with 0 changes | Show "No changes detected" message |
| Failed sync | Show error alert, "No changes due to failure" |
| Invalid syncHistoryId | Return 404 NotFound |
| Large result sets | DataTables pagination (50 per page) |
| Running sync | No View button shown |
