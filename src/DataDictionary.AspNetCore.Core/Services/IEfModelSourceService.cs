using DataDictionary.AspNetCore.Core.Entities;
using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

/// <summary>
/// Service for managing EF Model Source configurations.
/// </summary>
public interface IEfModelSourceService
{
    /// <summary>
    /// Gets all configured EF model sources.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all active EF model sources.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an EF model source by ID.
    /// </summary>
    Task<EfModelSource?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets EF model sources for a specific target database.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetByTargetDatabaseAsync(
        string server,
        string database,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new EF model source.
    /// </summary>
    Task<EfModelSource> CreateAsync(EfModelSourceCreateViewModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing EF model source.
    /// </summary>
    Task<EfModelSource> UpdateAsync(int id, EfModelSourceEditViewModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes an EF model source.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Updates the last compared timestamp for a source.
    /// </summary>
    Task UpdateLastComparedAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Toggles the active status of a source.
    /// </summary>
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Validates that the assembly path and DbContext are accessible.
    /// </summary>
    Task<ValidationResultViewModel> ValidateSourceAsync(int id, CancellationToken ct = default);
}
