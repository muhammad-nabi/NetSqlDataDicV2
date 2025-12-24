# Phase 1: Data Layer

## Status: Complete

## Objective

Create the database infrastructure for storing multiple notes per DataElement.

## Files Created

| File | Description |
|------|-------------|
| `Models/Entities/DataElementNote.cs` | Entity with FK to DataElement |
| `Data/Configurations/DataElementNoteConfiguration.cs` | EF configuration with indexes |
| `Migrations/[timestamp]_AddDataElementNotes.cs` | Migration with data migration SQL |

## Files Modified

| File | Change |
|------|--------|
| `Models/Entities/DataElement.cs` | Added `DataElementNotes` navigation property |
| `Data/DataDictionaryDbContext.cs` | Added `DataElementNotes` DbSet |

## DataElementNote Entity

```csharp
public class DataElementNote
{
    public int DataElementNoteId { get; set; }
    public int DataElementId { get; set; }
    public DataElement DataElement { get; set; } = null!;
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Configuration

- Table: `DataElementNotes`
- NoteText: Required, max 2000 characters
- FK to DataElement with cascade delete
- Indexes on `DataElementId` and `CreatedAt`

## Migration

Includes data migration SQL to copy existing Notes from DataElements:

```sql
INSERT INTO DataElementNotes (DataElementId, NoteText, CreatedAt)
SELECT DataElementId, Notes, COALESCE(LastUpdateTime, GETUTCDATE())
FROM DataElements
WHERE Notes IS NOT NULL AND Notes <> ''
```
