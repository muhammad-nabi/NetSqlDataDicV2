using DataDictionary.AspNetCore.Core.Models.Dto;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Services.DbContextProviders;

/// <summary>
/// Factory for creating and selecting appropriate DbContext providers.
/// </summary>
public interface IDbContextProviderFactory
{
    /// <summary>
    /// Gets a provider that can handle the given source.
    /// </summary>
    IDbContextProvider GetProvider(EfModelSource source);

    /// <summary>
    /// Gets all available provider type names.
    /// </summary>
    IEnumerable<string> GetAvailableProviderTypes();

    /// <summary>
    /// Discovers DbContext types in an assembly file.
    /// </summary>
    List<DbContextInfo> DiscoverDbContexts(string assemblyPath);
}
