# Notes Feature

Allows multiple timestamped notes per DataElement (database column).

## Phase Overview

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (DataElementNote entity, configuration, migration) |
| 2 | Complete | Service layer (ViewModels, service methods) |
| 3 | Complete | UI layer (Details page notes card, Add Note modal) |
| 4 | Complete | Cleanup (grid note count, documentation) |

## Key Features

- **Append-only notes** - maintains full history, no delete capability
- **Notes displayed newest-first** on Details page
- **Grid shows note count** badge linking to Details page
- **2000 character limit** per note
- **Migrated existing Notes** from legacy single-value field

## Data Model

```
DataElement (1) -----> (*) DataElementNote
```

- `DataElementNote` stores individual notes with `CreatedAt` timestamp
- Cascade delete: when DataElement is deleted, all notes are removed
- Legacy `Notes` field preserved for rollback safety

## UI Flow

1. **Grid View**: Shows note count badge for each column
2. **Click badge**: Navigates to Details page
3. **Details Page**: Shows all notes chronologically (newest first)
4. **Add Note**: Modal with character counter, saves via AJAX

## Files Created/Modified

See individual phase documentation for details.
