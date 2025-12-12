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
