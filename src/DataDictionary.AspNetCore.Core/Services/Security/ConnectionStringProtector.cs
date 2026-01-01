using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Core.Services.Security;

public class ConnectionStringProtector : IConnectionStringProtector
{
    private readonly IDataProtector _protector;
    private readonly ILogger<ConnectionStringProtector> _logger;

    private const string Purpose = "ConnectionString.Protection.v1";
    private const string ProtectedPrefix = "PROTECTED:";

    public ConnectionStringProtector(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ConnectionStringProtector> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Don't double-encrypt
        if (IsProtected(connectionString))
        {
            return connectionString;
        }

        try
        {
            var protectedBytes = _protector.Protect(
                System.Text.Encoding.UTF8.GetBytes(connectionString));

            return ProtectedPrefix + Convert.ToBase64String(protectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to protect connection string");
            throw new InvalidOperationException("Failed to encrypt connection string.", ex);
        }
    }

    public string Unprotect(string protectedConnectionString)
    {
        if (string.IsNullOrEmpty(protectedConnectionString))
        {
            return protectedConnectionString;
        }

        // If not protected, return as-is (for backward compatibility)
        if (!IsProtected(protectedConnectionString))
        {
            return protectedConnectionString;
        }

        try
        {
            var base64 = protectedConnectionString.Substring(ProtectedPrefix.Length);
            var protectedBytes = Convert.FromBase64String(base64);
            var unprotectedBytes = _protector.Unprotect(protectedBytes);

            return System.Text.Encoding.UTF8.GetString(unprotectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unprotect connection string");
            throw new InvalidOperationException("Failed to decrypt connection string.", ex);
        }
    }

    public bool IsProtected(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(ProtectedPrefix);
    }
}
