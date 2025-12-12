namespace NetSqlDataDicV2.Web.Models.Entities;

public class DataElementAudit
{
    public int DataElementAuditId { get; set; }

    // Foreign Keys
    public int DataElementId { get; set; }
    public DataElement DataElement { get; set; } = null!;

    public int SyncHistoryId { get; set; }
    public SyncHistory SyncHistory { get; set; } = null!;

    // Change tracking
    public string ChangeType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Timestamp
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
}
