using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IComparisonService
{
    Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default);
}
