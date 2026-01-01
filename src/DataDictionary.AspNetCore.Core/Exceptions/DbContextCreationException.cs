namespace DataDictionary.AspNetCore.Core.Exceptions;

/// <summary>
/// Exception thrown when a DbContext cannot be created from a loaded assembly.
/// </summary>
public class DbContextCreationException : Exception
{
    public string DbContextTypeName { get; }
    public string AssemblyPath { get; }
    public DbContextCreationErrorType ErrorType { get; }

    public DbContextCreationException(
        string dbContextTypeName,
        string assemblyPath,
        DbContextCreationErrorType errorType,
        string message)
        : base(message)
    {
        DbContextTypeName = dbContextTypeName;
        AssemblyPath = assemblyPath;
        ErrorType = errorType;
    }

    public DbContextCreationException(
        string dbContextTypeName,
        string assemblyPath,
        DbContextCreationErrorType errorType,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        DbContextTypeName = dbContextTypeName;
        AssemblyPath = assemblyPath;
        ErrorType = errorType;
    }
}

public enum DbContextCreationErrorType
{
    TypeNotFound,
    NoSuitableConstructor,
    ConstructorFailed,
    ConnectionStringRequired,
    ModelBuildFailed,
    UnknownError
}
