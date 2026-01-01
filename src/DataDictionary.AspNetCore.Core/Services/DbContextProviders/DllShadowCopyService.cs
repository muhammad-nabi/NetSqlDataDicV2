using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Core.Services.DbContextProviders;

/// <summary>
/// Implementation of shadow copy service that creates unique temporary directories
/// for each DLL load, enabling hot-reload without application restart.
/// </summary>
public class DllShadowCopyService : IDllShadowCopyService, IDisposable
{
    private readonly string _shadowCopyDir;
    private readonly ILogger<DllShadowCopyService> _logger;
    private readonly object _lock = new();
    private bool _disposed;

    public DllShadowCopyService(ILogger<DllShadowCopyService> logger)
    {
        _logger = logger;
        _shadowCopyDir = Path.Combine(Path.GetTempPath(), "DataDictionary", "ShadowCopies");

        try
        {
            Directory.CreateDirectory(_shadowCopyDir);
            _logger.LogInformation("Shadow copy directory initialized: {Path}", _shadowCopyDir);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create shadow copy directory: {Path}", _shadowCopyDir);
        }
    }

    public string CreateShadowCopy(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath))
        {
            throw new ArgumentException("Original path cannot be null or empty.", nameof(originalPath));
        }

        if (!File.Exists(originalPath))
        {
            throw new FileNotFoundException("Original DLL file not found.", originalPath);
        }

        lock (_lock)
        {
            // Clean up old copies first (per cleanup strategy)
            CleanupOldCopies();

            // Create unique subdirectory for this copy
            var copyId = Guid.NewGuid().ToString("N")[..8];
            var copyDir = Path.Combine(_shadowCopyDir, copyId);

            try
            {
                Directory.CreateDirectory(copyDir);

                // Copy main DLL
                var fileName = Path.GetFileName(originalPath);
                var destPath = Path.Combine(copyDir, fileName);
                File.Copy(originalPath, destPath, overwrite: true);

                _logger.LogDebug("Created shadow copy of DLL: {Source} -> {Dest}", originalPath, destPath);

                // Copy .deps.json if exists (for dependency resolution)
                var depsFile = Path.ChangeExtension(originalPath, ".deps.json");
                if (File.Exists(depsFile))
                {
                    var destDeps = Path.Combine(copyDir, Path.GetFileName(depsFile));
                    File.Copy(depsFile, destDeps, overwrite: true);
                    _logger.LogDebug("Copied deps.json: {File}", Path.GetFileName(depsFile));
                }

                // Copy .runtimeconfig.json if exists
                var runtimeConfig = Path.ChangeExtension(originalPath, ".runtimeconfig.json");
                if (File.Exists(runtimeConfig))
                {
                    var destConfig = Path.Combine(copyDir, Path.GetFileName(runtimeConfig));
                    File.Copy(runtimeConfig, destConfig, overwrite: true);
                    _logger.LogDebug("Copied runtimeconfig.json: {File}", Path.GetFileName(runtimeConfig));
                }

                _logger.LogInformation("Shadow copy created successfully: {Path}", destPath);
                return destPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shadow copy of {Path}", originalPath);

                // Cleanup failed copy directory
                try
                {
                    if (Directory.Exists(copyDir))
                    {
                        Directory.Delete(copyDir, recursive: true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                // Re-throw to let caller handle (or fall back to direct load)
                throw;
            }
        }
    }

    public void CleanupOldCopies()
    {
        if (!Directory.Exists(_shadowCopyDir))
        {
            return;
        }

        try
        {
            var directories = Directory.GetDirectories(_shadowCopyDir);
            var deletedCount = 0;

            foreach (var dir in directories)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    // Directory may be in use - skip it
                    _logger.LogDebug(ex, "Could not delete shadow copy directory (may be in use): {Dir}", dir);
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old shadow copy directories", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during shadow copy cleanup");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Final cleanup on disposal
        CleanupOldCopies();
    }
}
