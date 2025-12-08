namespace NetSqlDataDicV2.Web.Services.Security;

/// <summary>
/// Service for encrypting and decrypting connection strings.
/// </summary>
public interface IConnectionStringProtector
{
    /// <summary>
    /// Encrypts a connection string for storage.
    /// </summary>
    /// <param name="connectionString">The plain text connection string.</param>
    /// <returns>The encrypted connection string.</returns>
    string Protect(string connectionString);

    /// <summary>
    /// Decrypts a stored connection string.
    /// </summary>
    /// <param name="protectedConnectionString">The encrypted connection string.</param>
    /// <returns>The decrypted connection string.</returns>
    string Unprotect(string protectedConnectionString);

    /// <summary>
    /// Checks if a string appears to be encrypted.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is encrypted, false otherwise.</returns>
    bool IsProtected(string value);
}
