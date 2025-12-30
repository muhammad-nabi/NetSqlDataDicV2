# Phase 7: Shadow Copy for Hot-Reload Support

## Overview

This phase adds shadow copying capability to the DLL loading mechanism, enabling hot-reload of external DLLs without requiring an application restart.

## Problem Statement

When loading DLLs at runtime using `LoadFromAssemblyPath`, the CLR may:
1. Hold file locks on the original DLL
2. Use OS-level file caching that serves stale content
3. Prevent updates to the DLL while the application is running

This means users had to restart the application to pick up changes to their EF model DLLs.

## Solution

Copy the DLL to a unique temporary location before loading. This:
- Prevents file locks on the original DLL
- Ensures each load reads fresh file content
- Allows the original DLL to be updated while the app runs

## Files Created

### DllShadowCopyService.cs

**Location:** `Services/DbContextProviders/DllShadowCopyService.cs`

```csharp
public interface IDllShadowCopyService
{
    string CreateShadowCopy(string originalPath);
    void CleanupOldCopies();
}

public class DllShadowCopyService : IDllShadowCopyService, IDisposable
{
    // Implementation details...
}
```

**Key Features:**
- Creates shadow copies in `{TempPath}/NetSqlDataDicV2/ShadowCopies/`
- Uses GUID-based unique subdirectories for each copy
- Copies main DLL + `.deps.json` + `.runtimeconfig.json`
- Thread-safe via locking
- Cleanup on next comparison (not immediately after use)

## Files Modified

### DynamicDllProvider.cs

Added shadow copy step before loading:

```csharp
// Create shadow copy to enable hot-reload
string loadPath;
try
{
    loadPath = _shadowCopyService.CreateShadowCopy(assemblyPath);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Shadow copy failed, falling back to direct load");
    loadPath = assemblyPath;  // Graceful fallback
}

_loadContext = new PluginLoadContext(loadPath);
assembly = _loadContext.LoadFromAssemblyPath(loadPath);
```

### DbContextProviderFactory.cs

- Added `IDllShadowCopyService` injection
- Passes service to `DynamicDllProvider` constructor

### Program.cs

- Registered `IDllShadowCopyService` as Singleton

## Cleanup Strategy

**On next comparison:**
1. Before creating a new shadow copy, delete all existing shadow copy directories
2. Directories in use (file locks) are silently skipped
3. Final cleanup on service disposal (application shutdown)

This approach balances:
- Disk space management (no unbounded growth)
- Reliability (doesn't fail if files are locked)
- Simplicity (no background cleanup threads)

## Directory Structure

```
{TempPath}/
└── NetSqlDataDicV2/
    └── ShadowCopies/
        ├── abc12345/           # Unique per-load
        │   ├── MyDbContext.dll
        │   ├── MyDbContext.deps.json
        │   └── MyDbContext.runtimeconfig.json
        └── def67890/           # Another load
            └── ...
```

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Shadow copy fails | Falls back to direct load with warning log |
| Cleanup fails (file in use) | Skips that directory, continues with others |
| Temp directory creation fails | Logged at startup, shadow copy will fail gracefully |
| Invalid original path | Throws `ArgumentException` or `FileNotFoundException` |

## Testing

### Manual Test Steps

1. Start the application
2. Run an EF model comparison with DLL v1
3. Update the source DLL (e.g., add a new property to an entity)
4. Rebuild the DLL
5. Run comparison again without restarting the app
6. Verify the new property appears in the comparison results

### Verification Points

- [ ] New shadow copy directory created for each comparison
- [ ] Old shadow copies cleaned up before new comparison
- [ ] Original DLL can be overwritten while app is running
- [ ] Changes in DLL reflected without app restart

## Performance Considerations

- **Overhead:** File copy adds I/O latency (typically <100ms for small DLLs)
- **Disk Space:** Cleaned up on next comparison, minimal accumulation
- **Memory:** No additional memory overhead beyond file buffers

## Security

- Shadow copies inherit security validation from original path
- DLL validation (`IDllValidatorService`) occurs **before** shadow copy
- Temp directory permissions follow OS defaults
- No path traversal risk (uses `Path.Combine` with validated inputs)

## Dependencies

No new NuGet packages required. Uses:
- `System.IO` (built-in)
- `Microsoft.Extensions.Logging` (existing)
