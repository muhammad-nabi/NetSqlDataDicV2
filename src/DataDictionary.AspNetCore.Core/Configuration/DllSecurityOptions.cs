namespace DataDictionary.AspNetCore.Core.Configuration;

/// <summary>
/// Configuration options for DLL loading security.
/// </summary>
public class DllSecurityOptions
{
    public const string SectionName = "DllSecurity";

    /// <summary>
    /// List of allowed directories for DLL loading.
    /// Paths must be absolute. Empty list means all paths denied (secure default).
    /// </summary>
    public List<string> AllowedDirectories { get; set; } = new();

    /// <summary>
    /// File extensions allowed for loading.
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new() { ".dll" };

    /// <summary>
    /// Whether to require assemblies to be signed.
    /// </summary>
    public bool RequireSignedAssemblies { get; set; } = false;

    /// <summary>
    /// Maximum file size in bytes (default 100MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Whether to validate that EF Core dependencies exist.
    /// </summary>
    public bool ValidateEfCoreDependencies { get; set; } = true;

    /// <summary>
    /// Blocked assembly names that cannot be loaded.
    /// </summary>
    public List<string> BlockedAssemblyNames { get; set; } = new();
}
