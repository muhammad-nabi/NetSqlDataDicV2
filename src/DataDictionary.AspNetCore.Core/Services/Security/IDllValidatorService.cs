using DataDictionary.AspNetCore.Core.Models;

namespace DataDictionary.AspNetCore.Core.Services.Security;

/// <summary>
/// Service for validating DLL files before loading.
/// </summary>
public interface IDllValidatorService
{
    /// <summary>
    /// Validates a DLL file for safe loading.
    /// </summary>
    /// <param name="dllPath">The path to the DLL file.</param>
    /// <returns>Validation result indicating success or failure with details.</returns>
    ValidationResult ValidateDll(string dllPath);

    /// <summary>
    /// Checks if a path is within allowed directories.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is allowed, false otherwise.</returns>
    bool IsPathAllowed(string path);
}
