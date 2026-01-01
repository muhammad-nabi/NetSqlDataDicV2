using Microsoft.EntityFrameworkCore;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Services.DbContextProviders;

/// <summary>
/// Defines a provider that can create DbContext instances from various sources.
/// </summary>
public interface IDbContextProvider : IDisposable
{
    /// <summary>
    /// Gets the unique name identifying this provider type.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Determines if this provider can handle the given source configuration.
    /// </summary>
    bool CanProvide(EfModelSource source);

    /// <summary>
    /// Creates a DbContext instance from the given source configuration.
    /// </summary>
    DbContextProviderResult GetDbContext(EfModelSource source);
}
