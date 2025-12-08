namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown when a DLL cannot be loaded.
/// </summary>
public class DllLoadException : Exception
{
    public string DllPath { get; }
    public DllLoadErrorType ErrorType { get; }

    public DllLoadException(string dllPath, DllLoadErrorType errorType, string message)
        : base(message)
    {
        DllPath = dllPath;
        ErrorType = errorType;
    }

    public DllLoadException(string dllPath, DllLoadErrorType errorType, string message, Exception innerException)
        : base(message, innerException)
    {
        DllPath = dllPath;
        ErrorType = errorType;
    }
}

public enum DllLoadErrorType
{
    FileNotFound,
    InvalidAssembly,
    SecurityViolation,
    DependencyMissing,
    LoadFailed,
    UnknownError
}
