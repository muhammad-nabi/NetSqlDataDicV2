namespace DataDictionary.AspNetCore.Configuration;

/// <summary>
/// Configuration options for Data Dictionary middleware.
/// </summary>
public class DataDictionaryMiddlewareOptions
{
    /// <summary>
    /// Enable request logging middleware. Default: false
    /// Most consumers have their own logging middleware.
    /// </summary>
    public bool UseRequestLogging { get; set; } = false;

    /// <summary>
    /// Enable Data Dictionary exception handling middleware. Default: false
    /// Most consumers have their own exception handling.
    /// </summary>
    public bool UseExceptionHandling { get; set; } = false;

    /// <summary>
    /// Include stack traces in error responses. Default: false
    /// Only enable in development environments.
    /// </summary>
    public bool IncludeStackTraceInErrors { get; set; } = false;
}
