namespace DataDictionary.AspNetCore.Core.Entities;

public class SyncHistory
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
    public string Status { get; set; } = "Running";
    public string? ErrorMessage { get; set; }
}
