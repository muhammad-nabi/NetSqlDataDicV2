namespace NetSqlDataDicV2.Web.Helpers;

/// <summary>
/// Centralized user-friendly error messages.
/// </summary>
public static class ErrorMessages
{
    // DLL Loading Errors
    public static string DllNotFound(string path) =>
        $"The DLL file was not found at the specified path. Please verify the path is correct: {SanitizePath(path)}";

    public static string InvalidDll(string path) =>
        "The file is not a valid .NET assembly. Please ensure you're pointing to a compiled DLL file.";

    public static string DllSecurityViolation() =>
        "The DLL path is not in an allowed directory. Contact your administrator to configure allowed paths.";

    public static string DllTooLarge(long size, long maxSize) =>
        $"The DLL file ({FormatSize(size)}) exceeds the maximum allowed size ({FormatSize(maxSize)}).";

    // DbContext Errors
    public static string DbContextNotFound(string typeName, string assemblyName) =>
        $"Could not find DbContext type '{typeName}' in assembly '{assemblyName}'. " +
        "Use the Discover feature to see available DbContext types.";

    public static string NoDbContextInAssembly(string assemblyName) =>
        $"No DbContext types were found in '{assemblyName}'. " +
        "Ensure the assembly contains at least one class that inherits from DbContext.";

    public static string DbContextConstructorFailed(string typeName) =>
        $"Failed to create an instance of '{typeName}'. " +
        "The DbContext may require a connection string or have missing dependencies.";

    public static string ConnectionStringRequired(string typeName) =>
        $"DbContext '{typeName}' requires a connection string. " +
        "Please provide a connection string in the source configuration.";

    // Dependency Errors
    public static string MissingDependencies(List<string> dependencies) =>
        $"The following dependencies are missing: {string.Join(", ", dependencies.Take(5))}. " +
        "Ensure all required NuGet packages are included with the DLL.";

    // Source Configuration Errors
    public static string SourceNotFound(int id) =>
        $"EF Model Source with ID {id} was not found. It may have been deleted.";

    public static string SourceInactive(string name) =>
        $"EF Model Source '{name}' is currently inactive. Enable it before running comparisons.";

    public static string InvalidSourceConfiguration(string name, string reason) =>
        $"Source '{name}' has an invalid configuration: {reason}";

    // Comparison Errors
    public static string ComparisonFailed(string reason) =>
        $"Comparison failed: {reason}. Check the source configuration and try again.";

    public static string NoDataInDictionary(string server, string database) =>
        $"No data found in the Data Dictionary for {server}/{database}. " +
        "Run a sync operation first to populate the database metadata.";

    // Generic Errors
    public static string UnexpectedError() =>
        "An unexpected error occurred. Please try again or contact support if the problem persists.";

    // Helper methods
    private static string SanitizePath(string path)
    {
        // Remove potentially sensitive parts of the path
        if (string.IsNullOrEmpty(path)) return "(empty)";

        // Just show filename for security
        try
        {
            return Path.GetFileName(path);
        }
        catch
        {
            return "(invalid path)";
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
