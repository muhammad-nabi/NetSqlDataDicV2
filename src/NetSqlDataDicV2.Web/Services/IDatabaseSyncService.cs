using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IDatabaseSyncService
{
    Task<SyncResultViewModel> SyncDatabaseAsync(
        string connectionString,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default);

    Task<List<SourceColumnDto>> DiscoverColumnsAsync(
        string connectionString,
        CancellationToken cancellationToken = default);

    Task<List<SyncHistoryViewModel>> GetRecentSyncHistoryAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
