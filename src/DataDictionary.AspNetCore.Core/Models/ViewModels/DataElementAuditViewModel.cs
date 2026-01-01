namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

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
