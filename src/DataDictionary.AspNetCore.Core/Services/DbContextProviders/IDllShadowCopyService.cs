namespace DataDictionary.AspNetCore.Core.Services.DbContextProviders;

/// <summary>
/// Service for creating shadow copies of DLLs to enable hot-reload during runtime.
/// Shadow copies prevent file locks on the original DLL, allowing it to be updated
/// while the application is running.
/// </summary>
public interface IDllShadowCopyService
{
    /// <summary>
    /// Creates a shadow copy of the specified DLL in a temporary directory.
    /// Also copies related files (.deps.json, .runtimeconfig.json) for dependency resolution.
    /// </summary>
    /// <param name="originalPath">Path to the original DLL file.</param>
    /// <returns>Path to the shadow copy that should be loaded.</returns>
    string CreateShadowCopy(string originalPath);

    /// <summary>
    /// Cleans up old shadow copy directories.
    /// Called before creating a new shadow copy.
    /// </summary>
    void CleanupOldCopies();
}
