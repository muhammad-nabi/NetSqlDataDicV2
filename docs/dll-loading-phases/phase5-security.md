# Phase 5: Security & Validation

**Status: ✅ Complete**

## Overview

This phase adds security features to protect against malicious DLLs, path traversal attacks, and exposure of sensitive connection strings. Critical for production deployment.

## Goals

- Implement DLL validation before loading
- Configure allowed directories for DLL loading
- Encrypt connection strings at rest
- Add audit logging for security events
- Prevent path traversal attacks

## Prerequisites

- Phases 1-4 completed
- Understanding of .NET Data Protection API

## Security Concerns Addressed

| Threat | Mitigation |
|--------|------------|
| Malicious DLL execution | Validation, path whitelisting |
| Path traversal | Canonicalization, allowed paths |
| Connection string exposure | Encryption at rest |
| Unauthorized access | Audit logging |
| Dependency attacks | Assembly validation |

## Implementation Steps

### Step 5.1: Create Security Configuration

**New File:** `src/NetSqlDataDicV2.Web/Configuration/DllSecurityOptions.cs`

```csharp
namespace NetSqlDataDicV2.Web.Configuration;

/// <summary>
/// Configuration options for DLL loading security.
/// </summary>
public class DllSecurityOptions
{
    public const string SectionName = "DllSecurity";

    /// <summary>
    /// List of allowed directories for DLL loading.
    /// Paths must be absolute. Empty list means all paths allowed (not recommended).
    /// </summary>
    public List<string> AllowedDirectories { get; set; } = new();

    /// <summary>
    /// File extensions allowed for loading.
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new() { ".dll" };

    /// <summary>
    /// Whether to require assemblies to be signed.
    /// </summary>
    public bool RequireSignedAssemblies { get; set; } = false;

    /// <summary>
    /// Maximum file size in bytes (default 100MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Whether to validate that EF Core dependencies exist.
    /// </summary>
    public bool ValidateEfCoreDependencies { get; set; } = true;

    /// <summary>
    /// Blocked assembly names that cannot be loaded.
    /// </summary>
    public List<string> BlockedAssemblyNames { get; set; } = new();
}
```

**Configuration in appsettings.json:**

```json
{
  "DllSecurity": {
    "AllowedDirectories": [
      "C:\\PluginDlls",
      "D:\\EfModels"
    ],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600,
    "ValidateEfCoreDependencies": true,
    "BlockedAssemblyNames": []
  }
}
```

### Step 5.2: Create DLL Validator Service

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/IDllValidatorService.cs`

```csharp
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services.Security;

/// <summary>
/// Service for validating DLL files before loading.
/// </summary>
public interface IDllValidatorService
{
    /// <summary>
    /// Validates a DLL file for safe loading.
    /// </summary>
    ValidationResultViewModel ValidateDll(string dllPath);

    /// <summary>
    /// Checks if a path is within allowed directories.
    /// </summary>
    bool IsPathAllowed(string path);
}
```

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/DllValidatorService.cs`

```csharp
using Microsoft.Extensions.Options;
using NetSqlDataDicV2.Web.Configuration;
using NetSqlDataDicV2.Web.Models.ViewModels;
using System.Reflection;
using System.Security.Cryptography;

namespace NetSqlDataDicV2.Web.Services.Security;

public class DllValidatorService : IDllValidatorService
{
    private readonly DllSecurityOptions _options;
    private readonly ILogger<DllValidatorService> _logger;

    public DllValidatorService(
        IOptions<DllSecurityOptions> options,
        ILogger<DllValidatorService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ValidationResultViewModel ValidateDll(string dllPath)
    {
        _logger.LogInformation("Validating DLL: {Path}", dllPath);

        // Step 1: Validate path format
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            return Fail("DLL path cannot be empty.");
        }

        // Step 2: Canonicalize path to prevent traversal
        string canonicalPath;
        try
        {
            canonicalPath = Path.GetFullPath(dllPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid path format: {Path}", dllPath);
            return Fail($"Invalid path format: {ex.Message}");
        }

        // Step 3: Check allowed directories
        if (!IsPathAllowed(canonicalPath))
        {
            _logger.LogWarning("Path not in allowed directories: {Path}", canonicalPath);
            return Fail("DLL path is not in an allowed directory.");
        }

        // Step 4: Check file exists
        if (!File.Exists(canonicalPath))
        {
            return Fail($"File not found: {canonicalPath}");
        }

        // Step 5: Check file extension
        var extension = Path.GetExtension(canonicalPath).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            return Fail($"File extension '{extension}' is not allowed.");
        }

        // Step 6: Check file size
        var fileInfo = new FileInfo(canonicalPath);
        if (fileInfo.Length > _options.MaxFileSizeBytes)
        {
            return Fail($"File size ({fileInfo.Length:N0} bytes) exceeds maximum allowed ({_options.MaxFileSizeBytes:N0} bytes).");
        }

        // Step 7: Validate it's a .NET assembly
        var assemblyValidation = ValidateAssembly(canonicalPath);
        if (!assemblyValidation.IsValid)
        {
            return assemblyValidation;
        }

        _logger.LogInformation("DLL validation passed: {Path}", canonicalPath);

        return new ValidationResultViewModel
        {
            IsValid = true,
            Message = "DLL validation passed."
        };
    }

    public bool IsPathAllowed(string path)
    {
        // If no allowed directories configured, deny all (secure default)
        if (_options.AllowedDirectories == null || _options.AllowedDirectories.Count == 0)
        {
            _logger.LogWarning("No allowed directories configured. Denying all paths.");
            return false;
        }

        try
        {
            var canonicalPath = Path.GetFullPath(path);

            foreach (var allowedDir in _options.AllowedDirectories)
            {
                var canonicalAllowed = Path.GetFullPath(allowedDir);

                // Ensure the path starts with the allowed directory
                if (canonicalPath.StartsWith(canonicalAllowed, StringComparison.OrdinalIgnoreCase))
                {
                    // Additional check: must be within directory, not just prefix match
                    // e.g., "C:\Allowed" should not match "C:\AllowedOther\file.dll"
                    var relativePath = canonicalPath.Substring(canonicalAllowed.Length);
                    if (relativePath.Length == 0 ||
                        relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()) ||
                        relativePath.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking path: {Path}", path);
            return false;
        }
    }

    private ValidationResultViewModel ValidateAssembly(string path)
    {
        try
        {
            // Try to read as assembly without loading into AppDomain
            var assemblyName = AssemblyName.GetAssemblyName(path);

            // Check if assembly name is blocked
            if (_options.BlockedAssemblyNames.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase))
            {
                return Fail($"Assembly '{assemblyName.Name}' is blocked.");
            }

            // Check for strong name if required
            if (_options.RequireSignedAssemblies)
            {
                var publicKey = assemblyName.GetPublicKey();
                if (publicKey == null || publicKey.Length == 0)
                {
                    return Fail("Assembly is not signed. Signed assemblies are required.");
                }
            }

            _logger.LogDebug("Assembly validated: {Name} v{Version}",
                assemblyName.Name, assemblyName.Version);

            return new ValidationResultViewModel { IsValid = true };
        }
        catch (BadImageFormatException)
        {
            return Fail("File is not a valid .NET assembly.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating assembly: {Path}", path);
            return Fail($"Error validating assembly: {ex.Message}");
        }
    }

    private static ValidationResultViewModel Fail(string message)
    {
        return new ValidationResultViewModel
        {
            IsValid = false,
            ErrorMessage = message
        };
    }
}
```

### Step 5.3: Create Connection String Encryption Service

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/IConnectionStringProtector.cs`

```csharp
namespace NetSqlDataDicV2.Web.Services.Security;

/// <summary>
/// Service for encrypting and decrypting connection strings.
/// </summary>
public interface IConnectionStringProtector
{
    /// <summary>
    /// Encrypts a connection string for storage.
    /// </summary>
    string Protect(string connectionString);

    /// <summary>
    /// Decrypts a stored connection string.
    /// </summary>
    string Unprotect(string protectedConnectionString);

    /// <summary>
    /// Checks if a string appears to be encrypted.
    /// </summary>
    bool IsProtected(string value);
}
```

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/ConnectionStringProtector.cs`

```csharp
using Microsoft.AspNetCore.DataProtection;

namespace NetSqlDataDicV2.Web.Services.Security;

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
```

### Step 5.4: Create Security Audit Service

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/ISecurityAuditService.cs`

```csharp
namespace NetSqlDataDicV2.Web.Services.Security;

/// <summary>
/// Service for logging security-related events.
/// </summary>
public interface ISecurityAuditService
{
    void LogDllLoadAttempt(string path, bool success, string? errorMessage = null);
    void LogDllValidationFailure(string path, string reason);
    void LogUnauthorizedPathAccess(string path);
    void LogSourceCreated(int sourceId, string sourceName, string userName);
    void LogSourceDeleted(int sourceId, string sourceName, string userName);
    void LogComparisonExecuted(int sourceId, string sourceName, string userName);
}
```

**New File:** `src/NetSqlDataDicV2.Web/Services/Security/SecurityAuditService.cs`

```csharp
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
```

### Step 5.5: Update DynamicDllProvider to Use Validation

**Modified File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DynamicDllProvider.cs`

```csharp
public class DynamicDllProvider : IDbContextProvider
{
    private readonly IDllValidatorService _validator;
    private readonly IConnectionStringProtector _connectionStringProtector;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<DynamicDllProvider> _logger;
    // ... other fields

    public DynamicDllProvider(
        IDllValidatorService validator,
        IConnectionStringProtector connectionStringProtector,
        ISecurityAuditService auditService,
        ILogger<DynamicDllProvider> logger)
    {
        _validator = validator;
        _connectionStringProtector = connectionStringProtector;
        _auditService = auditService;
        _logger = logger;
    }

    public DbContextProviderResult GetDbContext(EfModelSource source)
    {
        var assemblyPath = source.AssemblyPath!;

        // Validate DLL before loading
        var validation = _validator.ValidateDll(assemblyPath);
        if (!validation.IsValid)
        {
            _auditService.LogDllValidationFailure(assemblyPath, validation.ErrorMessage!);
            return DbContextProviderResult.Fail(validation.ErrorMessage!);
        }

        try
        {
            // Decrypt connection string if needed
            var connectionString = source.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                connectionString = _connectionStringProtector.Unprotect(connectionString);
            }

            // ... rest of existing loading logic using decrypted connectionString ...

            _auditService.LogDllLoadAttempt(assemblyPath, true);

            return DbContextProviderResult.Ok(/* ... */);
        }
        catch (Exception ex)
        {
            _auditService.LogDllLoadAttempt(assemblyPath, false, ex.Message);
            throw;
        }
    }
}
```

### Step 5.6: Update EfModelSourceService to Encrypt Connection Strings

**Modified File:** `src/NetSqlDataDicV2.Web/Services/EfModelSourceService.cs`

```csharp
public class EfModelSourceService : IEfModelSourceService
{
    private readonly IConnectionStringProtector _protector;
    private readonly ISecurityAuditService _auditService;
    // ... other fields

    public async Task<EfModelSource> CreateAsync(
        EfModelSourceCreateViewModel model,
        CancellationToken ct = default)
    {
        var source = new EfModelSource
        {
            // ... other properties ...

            // Encrypt connection string before storage
            ConnectionString = !string.IsNullOrEmpty(model.ConnectionString)
                ? _protector.Protect(model.ConnectionString)
                : null,
        };

        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync(ct);

        _auditService.LogSourceCreated(source.Id, source.Name, GetCurrentUserName());

        return source;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source == null)
            throw new ArgumentException($"EfModelSource with ID {id} not found.");

        var sourceName = source.Name;

        _context.EfModelSources.Remove(source);
        await _context.SaveChangesAsync(ct);

        _auditService.LogSourceDeleted(id, sourceName, GetCurrentUserName());
    }

    private string GetCurrentUserName()
    {
        // Get from HttpContext if authentication is configured
        return "system"; // Placeholder - implement based on auth setup
    }
}
```

### Step 5.7: Register Security Services

**Modified File:** `src/NetSqlDataDicV2.Web/Program.cs`

```csharp
// Add Data Protection (required for connection string encryption)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Keys"))
    .SetApplicationName("NetSqlDataDicV2");

// Configure DLL security options
builder.Services.Configure<DllSecurityOptions>(
    builder.Configuration.GetSection(DllSecurityOptions.SectionName));

// Register security services
builder.Services.AddScoped<IDllValidatorService, DllValidatorService>();
builder.Services.AddScoped<IConnectionStringProtector, ConnectionStringProtector>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
```

### Step 5.8: Add Security Configuration to appsettings

**Modified File:** `src/NetSqlDataDicV2.Web/appsettings.json`

```json
{
  "DllSecurity": {
    "AllowedDirectories": [
      "C:\\DataDictionary\\Plugins"
    ],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600,
    "ValidateEfCoreDependencies": true,
    "BlockedAssemblyNames": []
  }
}
```

## Testing Checklist

- [ ] DLL validation rejects files outside allowed directories
- [ ] DLL validation rejects non-.NET files
- [ ] DLL validation rejects files over size limit
- [ ] Path traversal attempts are blocked (e.g., `..\..\`)
- [ ] Connection strings are encrypted when saved
- [ ] Connection strings are decrypted when used
- [ ] Double-encryption is prevented
- [ ] Backward compatibility with unencrypted strings
- [ ] Security audit logs capture all events
- [ ] Signed assembly requirement works when enabled
- [ ] Blocked assembly names are rejected

## Security Testing Scenarios

### Path Traversal Tests
```
Input: C:\DataDictionary\Plugins\..\..\..\Windows\System32\ntdll.dll
Expected: Blocked - not in allowed directory

Input: C:\DataDictionary\Plugins\valid.dll
Expected: Allowed
```

### Malformed Path Tests
```
Input: C:\DataDictionary\Plugins\<script>alert(1)</script>.dll
Expected: Blocked - invalid characters

Input: (empty string)
Expected: Blocked - path required
```

## Files Created

| File | Purpose |
|------|---------|
| `Configuration/DllSecurityOptions.cs` | Security configuration model |
| `Services/Security/IDllValidatorService.cs` | Validator interface |
| `Services/Security/DllValidatorService.cs` | DLL validation implementation |
| `Services/Security/IConnectionStringProtector.cs` | Encryption interface |
| `Services/Security/ConnectionStringProtector.cs` | Encryption implementation |
| `Services/Security/ISecurityAuditService.cs` | Audit interface |
| `Services/Security/SecurityAuditService.cs` | Audit implementation |

## Files Modified

| File | Change Type |
|------|-------------|
| `Services/DbContextProviders/DynamicDllProvider.cs` | Added validation, decryption, audit logging |
| `Services/DbContextProviders/DbContextProviderFactory.cs` | Added security service dependencies, validation in DiscoverDbContexts |
| `Services/EfModelSourceService.cs` | Added encryption on save, audit logging |
| `Program.cs` | Service registration, Data Protection |
| `appsettings.json` | Security configuration section |

## Implementation Notes

### Path Validation
- Uses `Path.GetFullPath()` for canonicalization to prevent traversal attacks
- Platform-aware comparison (case-sensitive on macOS/Linux, case-insensitive on Windows)
- Normalizes directory paths to ensure proper prefix matching

### Connection String Encryption
- Uses ASP.NET Core Data Protection API
- Prefixes encrypted values with `PROTECTED:` for identification
- Backward compatible - handles unencrypted strings gracefully
- Double-encryption prevention built-in

### Audit Logging
- All DLL load attempts logged (success/failure)
- Validation failures logged with reasons
- Source creation/deletion logged with user context

## Production Considerations

1. **Data Protection Keys**: Store keys securely (Azure Key Vault, AWS KMS, or secure file system)
2. **Allowed Directories**: Restrict to specific directories with proper file system permissions
3. **Audit Log Retention**: Configure log retention and forwarding to SIEM
4. **Certificate Requirements**: Consider requiring signed assemblies in production
5. **Network Isolation**: Run DLL loading in isolated process if high security required

## Next Phase

Phase 6 will add comprehensive error handling and logging throughout the application.
