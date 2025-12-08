namespace NetSqlDataDicV2.Web.Services.Security;

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public void LogDllLoadAttempt(string path, bool success, string? errorMessage = null)
    {
        if (success)
        {
            _logger.LogInformation(
                "SECURITY_AUDIT: DLL loaded successfully. Path: {Path}",
                path);
        }
        else
        {
            _logger.LogWarning(
                "SECURITY_AUDIT: DLL load failed. Path: {Path}, Error: {Error}",
                path, errorMessage);
        }
    }

    public void LogDllValidationFailure(string path, string reason)
    {
        _logger.LogWarning(
            "SECURITY_AUDIT: DLL validation failed. Path: {Path}, Reason: {Reason}",
            path, reason);
    }

    public void LogUnauthorizedPathAccess(string path)
    {
        _logger.LogWarning(
            "SECURITY_AUDIT: Unauthorized path access attempted. Path: {Path}",
            path);
    }

    public void LogSourceCreated(int sourceId, string sourceName, string userName)
    {
        _logger.LogInformation(
            "SECURITY_AUDIT: EF Model Source created. ID: {SourceId}, Name: {Name}, User: {User}",
            sourceId, sourceName, userName);
    }

    public void LogSourceDeleted(int sourceId, string sourceName, string userName)
    {
        _logger.LogInformation(
            "SECURITY_AUDIT: EF Model Source deleted. ID: {SourceId}, Name: {Name}, User: {User}",
            sourceId, sourceName, userName);
    }

    public void LogComparisonExecuted(int sourceId, string sourceName, string userName)
    {
        _logger.LogInformation(
            "SECURITY_AUDIT: Comparison executed. SourceID: {SourceId}, Name: {Name}, User: {User}",
            sourceId, sourceName, userName);
    }
}
