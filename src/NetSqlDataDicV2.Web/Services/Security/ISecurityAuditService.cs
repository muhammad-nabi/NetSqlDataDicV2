namespace NetSqlDataDicV2.Web.Services.Security;

/// <summary>
/// Service for logging security-related events.
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Logs a DLL load attempt.
    /// </summary>
    void LogDllLoadAttempt(string path, bool success, string? errorMessage = null);

    /// <summary>
    /// Logs a DLL validation failure.
    /// </summary>
    void LogDllValidationFailure(string path, string reason);

    /// <summary>
    /// Logs an unauthorized path access attempt.
    /// </summary>
    void LogUnauthorizedPathAccess(string path);

    /// <summary>
    /// Logs when an EF Model Source is created.
    /// </summary>
    void LogSourceCreated(int sourceId, string sourceName, string userName);

    /// <summary>
    /// Logs when an EF Model Source is deleted.
    /// </summary>
    void LogSourceDeleted(int sourceId, string sourceName, string userName);

    /// <summary>
    /// Logs when a comparison is executed.
    /// </summary>
    void LogComparisonExecuted(int sourceId, string sourceName, string userName);
}
