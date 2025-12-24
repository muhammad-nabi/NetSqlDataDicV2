# Phase 2: Service Layer

## Status: Complete

## Objective

Create ViewModels and service methods for note operations.

## Files Created

| File | Description |
|------|-------------|
| `Models/ViewModels/DataElementNoteViewModel.cs` | Display model with computed properties |
| `Models/ViewModels/AddNoteViewModel.cs` | Input model with validation |

## Files Modified

| File | Change |
|------|--------|
| `Models/ViewModels/DataElementDetailsViewModel.cs` | Added `Notes`, `NoteCount`, `LastNoteDate` |
| `Services/IDataDictionaryService.cs` | Added `AddNoteAsync`, `GetNotesAsync` |
| `Services/DataDictionaryService.cs` | Implemented note operations |

## DataElementNoteViewModel

```csharp
public class DataElementNoteViewModel
{
    public int DataElementNoteId { get; set; }
    public int DataElementId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Computed
    public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");
    public string RelativeTime { get; }  // "5 days ago", "Just now", etc.
}
```

## Service Methods

```csharp
// Add a new note
Task<DataElementNoteViewModel> AddNoteAsync(int dataElementId, string noteText, CancellationToken ct);

// Get all notes for an element (newest first)
Task<List<DataElementNoteViewModel>> GetNotesAsync(int dataElementId, CancellationToken ct);
```

## Key Implementation Details

- `AddNoteAsync` validates DataElement exists (including deleted ones via `IgnoreQueryFilters`)
- `GetNotesAsync` returns notes ordered by `CreatedAt` descending
- `GetDetailsAsync` updated to include notes in result
