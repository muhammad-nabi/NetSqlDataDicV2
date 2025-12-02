namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class SyncResultViewModel
{
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TablesProcessed { get; set; }
    public int ColumnsProcessed { get; set; }
    public int ColumnsAdded { get; set; }
    public int ColumnsUpdated { get; set; }
    public int ColumnsRemoved { get; set; }
    public TimeSpan Duration { get; set; }
}
