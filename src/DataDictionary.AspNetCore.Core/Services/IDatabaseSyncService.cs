using DataDictionary.AspNetCore.Core.Models.Dto;
using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

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

    /// <summary>
    /// Gets detailed sync results including all audit records for a specific sync operation.
    /// </summary>
    /// <param name="syncHistoryId">The sync history ID to get results for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SyncResultsViewModel or null if not found.</returns>
    Task<SyncResultsViewModel?> GetSyncResultsAsync(
        int syncHistoryId,
        CancellationToken cancellationToken = default);
}
