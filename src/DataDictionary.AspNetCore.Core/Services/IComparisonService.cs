using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

public interface IComparisonService
{
    /// <summary>
    /// Compare using a configured EfModelSource.
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default);
}
