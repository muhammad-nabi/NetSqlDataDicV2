# Audit Trail Feature - Overview

## Purpose

Add property-level audit trail to track exactly what changed for each DataElement during database sync operations. This enables users to see the complete history of schema changes over time.

## Business Requirements

1. **Property-level tracking**: Record each property change (e.g., "DataType: VARCHAR(50) → VARCHAR(100)")
2. **Change types**: Track Added, Modified, Deleted, and Restored operations
3. **No noise**: Skip audit record creation when nothing changed during sync
4. **Retention**: Keep audit history forever (manual cleanup option for future)
5. **UI Access**: Details page accessible from Data Dictionary grid showing current state + audit history

## Current State

The existing sync system (`DatabaseSyncService.SyncDatabaseAsync`) already:
- Detects new columns (added)
- Detects property changes (updated)
- Soft-deletes removed columns
- Tracks aggregate counts in `SyncHistory` (ColumnsAdded, ColumnsUpdated, ColumnsRemoved)

**Gap**: No granular record of *what* changed for each column.

## Solution Summary

1. New `DataElementAudit` entity to store property-level changes
2. Modify sync service to create audit records during sync
3. New Details page to view column state and audit history

## Phase Breakdown

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Data Layer - Entity, Configuration, Migration | Complete |
| 2 | Service Layer - Sync modifications, audit queries | Complete |
| 3 | ViewModel Layer - DTOs and ViewModels | Complete |
| 4 | UI Layer - Controller actions and Views | Complete |

## Files Summary

### New Files (6)
- `Models/Entities/DataElementAudit.cs`
- `Data/Configurations/DataElementAuditConfiguration.cs`
- `Models/Dto/PropertyChange.cs`
- `Models/ViewModels/DataElementAuditViewModel.cs`
- `Models/ViewModels/DataElementDetailsViewModel.cs`
- `Views/DataDictionary/Details.cshtml`

### Modified Files (8)
- `Data/DataDictionaryDbContext.cs`
- `Services/DatabaseSyncService.cs`
- `Services/IDataDictionaryService.cs`
- `Services/DataDictionaryService.cs`
- `Controllers/DataDictionaryController.cs`
- `Views/DataDictionary/Index.cshtml`
- `Views/Shared/_Layout.cshtml` (added Deleted nav link)
- `Models/ViewModels/DataElementViewModel.cs` (added IsDeleted property)

### Additional Features (added during implementation)
- `Views/DataDictionary/Deleted.cshtml` - Separate view for soft-deleted columns
