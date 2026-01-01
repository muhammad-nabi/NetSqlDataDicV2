namespace DataDictionary.AspNetCore.Core.Exceptions;

/// <summary>
/// Exception thrown for EF Model Source configuration errors.
/// </summary>
public class EfModelSourceException : Exception
{
    public int? SourceId { get; }
    public string? SourceName { get; }

    public EfModelSourceException(string message)
        : base(message)
    {
    }

    public EfModelSourceException(int sourceId, string sourceName, string message)
        : base(message)
    {
        SourceId = sourceId;
        SourceName = sourceName;
    }

    public EfModelSourceException(int sourceId, string sourceName, string message, Exception innerException)
        : base(message, innerException)
    {
        SourceId = sourceId;
        SourceName = sourceName;
    }
}
