# Phase 3: ViewModel Layer

## Objective

Create the DTOs and ViewModels needed for the Details page UI.

## Prerequisites

- Phase 1 complete (DataElementAudit entity exists)
- Phase 2 can proceed in parallel if ViewModels are stubbed

## Tasks

### 3.1 Create `DataElementAuditViewModel`

**File**: `/src/NetSqlDataDicV2.Web/Models/ViewModels/DataElementAuditViewModel.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementAuditViewModel
{
    public int DataElementAuditId { get; set; }
    public int DataElementId { get; set; }
    public int SyncHistoryId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangeTime { get; set; }

    /// <summary>
    /// Human-readable description of the change
    /// </summary>
    public string ChangeDescription => ChangeType switch
    {
        "Added" => "Column added",
        "Deleted" => "Column removed",
        "Restored" => "Column restored",
        "Modified" => $"{PropertyName}: {OldValue ?? "(null)"} → {NewValue ?? "(null)"}",
        _ => ChangeType
    };

    /// <summary>
    /// Bootstrap badge CSS class based on change type
    /// </summary>
    public string ChangeTypeBadgeClass => ChangeType switch
    {
        "Added" => "bg-success",
        "Deleted" => "bg-danger",
        "Restored" => "bg-info",
        "Modified" => "bg-warning text-dark",
        _ => "bg-secondary"
    };
}
```

**Properties**:

| Property | Purpose |
|----------|---------|
| DataElementAuditId | Primary key for reference |
| DataElementId | Link back to parent DataElement |
| SyncHistoryId | Link to sync operation |
| ChangeType | Type of change (Added/Modified/Deleted/Restored) |
| PropertyName | Which property changed (for Modified only) |
| OldValue/NewValue | Before/after values |
| ChangeTime | When change occurred |
| ChangeDescription | Computed display text |
| ChangeTypeBadgeClass | Computed Bootstrap class for styling |

---

### 3.2 Create `DataElementDetailsViewModel`

**File**: `/src/NetSqlDataDicV2.Web/Models/ViewModels/DataElementDetailsViewModel.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementDetailsViewModel
{
    /// <summary>
    /// The current state of the data element
    /// </summary>
    public DataElementViewModel DataElement { get; set; } = null!;

    /// <summary>
    /// Audit history ordered by most recent first
    /// </summary>
    public List<DataElementAuditViewModel> AuditHistory { get; set; } = new();

    /// <summary>
    /// Total number of changes recorded
    /// </summary>
    public int TotalChanges => AuditHistory.Count;

    /// <summary>
    /// When this column was first discovered (first "Added" record)
    /// </summary>
    public DateTime? FirstSeen => AuditHistory
        .Where(a => a.ChangeType == "Added")
        .MinBy(a => a.ChangeTime)?.ChangeTime;

    /// <summary>
    /// Most recent modification time (excludes Added/Deleted)
    /// </summary>
    public DateTime? LastModified => AuditHistory
        .Where(a => a.ChangeType == "Modified")
        .MaxBy(a => a.ChangeTime)?.ChangeTime;

    /// <summary>
    /// Count of modifications (property changes)
    /// </summary>
    public int ModificationCount => AuditHistory.Count(a => a.ChangeType == "Modified");

    /// <summary>
    /// Whether column has ever been deleted
    /// </summary>
    public bool WasEverDeleted => AuditHistory.Any(a => a.ChangeType == "Deleted");

    /// <summary>
    /// Whether column has been restored after deletion
    /// </summary>
    public bool WasRestored => AuditHistory.Any(a => a.ChangeType == "Restored");
}
```

**Computed Properties**:

| Property | Description |
|----------|-------------|
| TotalChanges | Total audit records |
| FirstSeen | Date column was first discovered |
| LastModified | Most recent property change |
| ModificationCount | Number of property changes |
| WasEverDeleted | Historical delete indicator |
| WasRestored | Was deleted then restored |

---

### 3.3 Verify Existing `DataElementViewModel`

**File**: `/src/NetSqlDataDicV2.Web/Models/ViewModels/DataElementViewModel.cs`

Ensure this existing ViewModel has all properties needed for the Details page:

Required properties:
- DataElementId
- DataElementName
- DataType
- DatabaseServer
- DatabaseName
- SchemaName
- TableName
- ColumnName
- IsNullable
- IsPrimaryKey
- ForeignKeyTo
- MaxLength
- Precision
- Scale
- DataPurpose
- Notes
- LastSyncTime
- CreateTime
- LastUpdateTime
- IsDeleted

If any are missing, they should be added.

---

## Verification

After completing Phase 3:

1. Build succeeds: `dotnet build`
2. ViewModels compile without errors
3. Computed properties work correctly (can unit test)

## Unit Test Suggestions

```csharp
[Fact]
public void ChangeTypeBadgeClass_ReturnsCorrectClass()
{
    var audit = new DataElementAuditViewModel { ChangeType = "Added" };
    Assert.Equal("bg-success", audit.ChangeTypeBadgeClass);

    audit.ChangeType = "Deleted";
    Assert.Equal("bg-danger", audit.ChangeTypeBadgeClass);
}

[Fact]
public void ChangeDescription_FormatsModifiedCorrectly()
{
    var audit = new DataElementAuditViewModel
    {
        ChangeType = "Modified",
        PropertyName = "DataType",
        OldValue = "VARCHAR(50)",
        NewValue = "VARCHAR(100)"
    };

    Assert.Equal("DataType: VARCHAR(50) → VARCHAR(100)", audit.ChangeDescription);
}
```

## Dependencies

- Existing `DataElementViewModel` must exist

## Next Phase

Phase 4: UI Layer - Controller actions and Views
