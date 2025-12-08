namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown when assembly dependencies cannot be resolved.
/// </summary>
public class DependencyResolutionException : Exception
{
    public string AssemblyPath { get; }
    public List<string> MissingDependencies { get; }

    public DependencyResolutionException(
        string assemblyPath,
        List<string> missingDependencies,
        string message)
        : base(message)
    {
        AssemblyPath = assemblyPath;
        MissingDependencies = missingDependencies ?? new List<string>();
    }

    public DependencyResolutionException(
        string assemblyPath,
        List<string> missingDependencies,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        AssemblyPath = assemblyPath;
        MissingDependencies = missingDependencies ?? new List<string>();
    }
}
