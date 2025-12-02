namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class SyncHistoryViewModel
{
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime SyncStartTime { get; set; }
    public DateTime? SyncEndTime { get; set; }
    public int? TablesProcessed { get; set; }
    public int? ColumnsProcessed { get; set; }
    public int? ColumnsAdded { get; set; }
    public int? ColumnsUpdated { get; set; }
    public int? ColumnsRemoved { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "Completed" => "bg-success",
        "Failed" => "bg-danger",
        "Running" => "bg-warning",
        _ => "bg-secondary"
    };

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
