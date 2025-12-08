using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IComparisonService
{
    /// <summary>
    /// Compare using a configured EfModelSource.
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default);
}
