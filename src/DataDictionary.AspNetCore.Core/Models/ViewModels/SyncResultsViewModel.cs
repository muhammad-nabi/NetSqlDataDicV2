namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

/// <summary>
/// ViewModel for the Sync Results page showing all audit records for a sync operation.
/// </summary>
public class SyncResultsViewModel
{
    // Sync history information
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime SyncStartTime { get; set; }
    public DateTime? SyncEndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    // Summary counts (from SyncHistory)
    public int TablesProcessed { get; set; }
    public int ColumnsProcessed { get; set; }

    // Change type counts (calculated from audit records)
    public int AddedCount { get; set; }
    public int ModifiedCount { get; set; }
    public int DeletedCount { get; set; }
    public int RestoredCount { get; set; }

    // Audit records
    public List<SyncAuditItemViewModel> AuditItems { get; set; } = new();

    // Computed properties
    public int TotalChanges => AddedCount + ModifiedCount + DeletedCount + RestoredCount;
    public bool HasChanges => TotalChanges > 0;
    public bool IsFailed => Status == "Failed";

    /// <summary>
    /// Human-readable duration display.
    /// </summary>
    public string DurationDisplay
    {
        get
        {
            if (!SyncEndTime.HasValue) return "In progress...";
            var duration = SyncEndTime.Value - SyncStartTime;
            return duration.TotalSeconds < 60
                ? $"{duration.TotalSeconds:F1}s"
                : $"{duration.TotalMinutes:F1}m";
        }
    }
}

/// <summary>
/// Individual audit item for the sync results grid.
/// Includes column location from DataElement navigation.
/// </summary>
public class SyncAuditItemViewModel
{
    public int DataElementAuditId { get; set; }
    public int DataElementId { get; set; }

    // Change information
    public string ChangeType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangeTime { get; set; }

    // Column location (from DataElement navigation)
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Bootstrap badge CSS class for the change type.
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
