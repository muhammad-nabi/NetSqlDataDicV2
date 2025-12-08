using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IComparisonService
{
    /// <summary>
    /// Compare using direct reference (backward compatible).
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare using a configured EfModelSource.
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default);
}
