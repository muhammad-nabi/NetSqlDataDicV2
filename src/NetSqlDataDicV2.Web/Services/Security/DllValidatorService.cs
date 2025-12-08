using Microsoft.Extensions.Options;
using NetSqlDataDicV2.Web.Configuration;
using NetSqlDataDicV2.Web.Models.ViewModels;
using System.Reflection;

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
            _logger.LogDebug("Checking if path is allowed: {Path}", canonicalPath);

            foreach (var allowedDir in _options.AllowedDirectories)
            {
                var canonicalAllowed = Path.GetFullPath(allowedDir);

                // Normalize: ensure allowed directory ends with separator for proper matching
                if (!canonicalAllowed.EndsWith(Path.DirectorySeparatorChar) &&
                    !canonicalAllowed.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    canonicalAllowed += Path.DirectorySeparatorChar;
                }

                _logger.LogDebug("Comparing against allowed directory: {AllowedDir}", canonicalAllowed);

                // Check if the file path starts with the allowed directory
                // Using ordinal comparison for case-sensitive file systems (Linux/macOS)
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                if (canonicalPath.StartsWith(canonicalAllowed, comparison))
                {
                    _logger.LogDebug("Path {Path} is within allowed directory {AllowedDir}",
                        canonicalPath, canonicalAllowed);
                    return true;
                }

                // Also check if the path exactly equals the directory (edge case)
                var canonicalAllowedTrimmed = canonicalAllowed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (canonicalPath.Equals(canonicalAllowedTrimmed, comparison))
                {
                    _logger.LogDebug("Path {Path} equals allowed directory {AllowedDir}",
                        canonicalPath, canonicalAllowedTrimmed);
                    return true;
                }
            }

            _logger.LogDebug("Path {Path} is not in any allowed directory", canonicalPath);
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
            if (_options.BlockedAssemblyNames.Contains(assemblyName.Name!, StringComparer.OrdinalIgnoreCase))
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
