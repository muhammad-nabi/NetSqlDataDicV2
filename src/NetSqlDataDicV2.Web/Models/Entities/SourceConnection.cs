namespace NetSqlDataDicV2.Web.Models.Entities;

public class SourceConnection
{
    public int ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncTime { get; set; }
}
