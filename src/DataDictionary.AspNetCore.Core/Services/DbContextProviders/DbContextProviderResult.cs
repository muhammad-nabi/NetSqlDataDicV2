using Microsoft.EntityFrameworkCore;
using System.Runtime.Loader;

namespace DataDictionary.AspNetCore.Core.Services.DbContextProviders;

/// <summary>
/// Result of a DbContext provider operation, including the context and metadata.
/// </summary>
public class DbContextProviderResult : IDisposable
{
    /// <summary>
    /// The loaded DbContext instance. Null if loading failed.
    /// </summary>
    public DbContext? Context { get; init; }

    /// <summary>
    /// Indicates whether the DbContext was successfully loaded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The AssemblyLoadContext used to load the assembly (for cleanup).
    /// Only set for dynamically loaded assemblies.
    /// </summary>
    public AssemblyLoadContext? LoadContext { get; init; }

    /// <summary>
    /// Name of the DbContext type that was loaded.
    /// </summary>
    public string? DbContextTypeName { get; init; }

    /// <summary>
    /// Path to the assembly that was loaded (for DynamicDll provider).
    /// </summary>
    public string? AssemblyPath { get; init; }

    public void Dispose()
    {
        Context?.Dispose();

        // Unload the assembly context if it was dynamically loaded
        if (LoadContext is { IsCollectible: true })
        {
            LoadContext.Unload();
        }
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DbContextProviderResult Ok(
        DbContext context,
        string dbContextTypeName,
        AssemblyLoadContext? loadContext = null,
        string? assemblyPath = null)
    {
        return new DbContextProviderResult
        {
            Context = context,
            Success = true,
            DbContextTypeName = dbContextTypeName,
            LoadContext = loadContext,
            AssemblyPath = assemblyPath
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DbContextProviderResult Fail(string errorMessage)
    {
        return new DbContextProviderResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
