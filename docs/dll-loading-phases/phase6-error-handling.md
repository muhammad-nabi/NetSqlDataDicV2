# Phase 6: Error Handling & Logging

## Overview

This phase adds comprehensive error handling with custom exceptions, structured logging, and user-friendly error messages throughout the DLL loading feature.

## Goals

- Create custom exception types for specific error scenarios
- Implement structured logging with correlation IDs
- Provide clear, actionable error messages to users
- Add error recovery mechanisms where possible
- Ensure errors don't expose sensitive information

## Prerequisites

- Phases 1-5 completed
- Understanding of .NET logging patterns

## Implementation Steps

### Step 6.1: Create Custom Exceptions

**New File:** `src/NetSqlDataDicV2.Web/Exceptions/DllLoadException.cs`

```csharp
namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown when a DLL cannot be loaded.
/// </summary>
public class DllLoadException : Exception
{
    public string DllPath { get; }
    public DllLoadErrorType ErrorType { get; }

    public DllLoadException(string dllPath, DllLoadErrorType errorType, string message)
        : base(message)
    {
        DllPath = dllPath;
        ErrorType = errorType;
    }

    public DllLoadException(string dllPath, DllLoadErrorType errorType, string message, Exception innerException)
        : base(message, innerException)
    {
        DllPath = dllPath;
        ErrorType = errorType;
    }
}

public enum DllLoadErrorType
{
    FileNotFound,
    InvalidAssembly,
    SecurityViolation,
    DependencyMissing,
    LoadFailed,
    UnknownError
}
```

**New File:** `src/NetSqlDataDicV2.Web/Exceptions/DbContextCreationException.cs`

```csharp
namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown when a DbContext cannot be created from a loaded assembly.
/// </summary>
public class DbContextCreationException : Exception
{
    public string DbContextTypeName { get; }
    public string AssemblyPath { get; }
    public DbContextCreationErrorType ErrorType { get; }

    public DbContextCreationException(
        string dbContextTypeName,
        string assemblyPath,
        DbContextCreationErrorType errorType,
        string message)
        : base(message)
    {
        DbContextTypeName = dbContextTypeName;
        AssemblyPath = assemblyPath;
        ErrorType = errorType;
    }

    public DbContextCreationException(
        string dbContextTypeName,
        string assemblyPath,
        DbContextCreationErrorType errorType,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        DbContextTypeName = dbContextTypeName;
        AssemblyPath = assemblyPath;
        ErrorType = errorType;
    }
}

public enum DbContextCreationErrorType
{
    TypeNotFound,
    NoSuitableConstructor,
    ConstructorFailed,
    ConnectionStringRequired,
    ModelBuildFailed,
    UnknownError
}
```

**New File:** `src/NetSqlDataDicV2.Web/Exceptions/DependencyResolutionException.cs`

```csharp
namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown when assembly dependencies cannot be resolved.
/// </summary>
public class DependencyResolutionException : Exception
{
    public string AssemblyPath { get; }
    public List<string> MissingDependencies { get; }

    public DependencyResolutionException(
        string assemblyPath,
        List<string> missingDependencies,
        string message)
        : base(message)
    {
        AssemblyPath = assemblyPath;
        MissingDependencies = missingDependencies ?? new List<string>();
    }

    public DependencyResolutionException(
        string assemblyPath,
        List<string> missingDependencies,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        AssemblyPath = assemblyPath;
        MissingDependencies = missingDependencies ?? new List<string>();
    }
}
```

**New File:** `src/NetSqlDataDicV2.Web/Exceptions/EfModelSourceException.cs`

```csharp
namespace NetSqlDataDicV2.Web.Exceptions;

/// <summary>
/// Exception thrown for EF Model Source configuration errors.
/// </summary>
public class EfModelSourceException : Exception
{
    public int? SourceId { get; }
    public string? SourceName { get; }

    public EfModelSourceException(string message)
        : base(message)
    {
    }

    public EfModelSourceException(int sourceId, string sourceName, string message)
        : base(message)
    {
        SourceId = sourceId;
        SourceName = sourceName;
    }

    public EfModelSourceException(int sourceId, string sourceName, string message, Exception innerException)
        : base(message, innerException)
    {
        SourceId = sourceId;
        SourceName = sourceName;
    }
}
```

### Step 6.2: Create Error Message Helper

**New File:** `src/NetSqlDataDicV2.Web/Helpers/ErrorMessages.cs`

```csharp
namespace NetSqlDataDicV2.Web.Helpers;

/// <summary>
/// Centralized user-friendly error messages.
/// </summary>
public static class ErrorMessages
{
    // DLL Loading Errors
    public static string DllNotFound(string path) =>
        $"The DLL file was not found at the specified path. Please verify the path is correct: {SanitizePath(path)}";

    public static string InvalidDll(string path) =>
        $"The file is not a valid .NET assembly. Please ensure you're pointing to a compiled DLL file.";

    public static string DllSecurityViolation() =>
        "The DLL path is not in an allowed directory. Contact your administrator to configure allowed paths.";

    public static string DllTooLarge(long size, long maxSize) =>
        $"The DLL file ({FormatSize(size)}) exceeds the maximum allowed size ({FormatSize(maxSize)}).";

    // DbContext Errors
    public static string DbContextNotFound(string typeName, string assemblyName) =>
        $"Could not find DbContext type '{typeName}' in assembly '{assemblyName}'. " +
        "Use the Discover feature to see available DbContext types.";

    public static string NoDbContextInAssembly(string assemblyName) =>
        $"No DbContext types were found in '{assemblyName}'. " +
        "Ensure the assembly contains at least one class that inherits from DbContext.";

    public static string DbContextConstructorFailed(string typeName) =>
        $"Failed to create an instance of '{typeName}'. " +
        "The DbContext may require a connection string or have missing dependencies.";

    public static string ConnectionStringRequired(string typeName) =>
        $"DbContext '{typeName}' requires a connection string. " +
        "Please provide a connection string in the source configuration.";

    // Dependency Errors
    public static string MissingDependencies(List<string> dependencies) =>
        $"The following dependencies are missing: {string.Join(", ", dependencies.Take(5))}. " +
        "Ensure all required NuGet packages are included with the DLL.";

    // Source Configuration Errors
    public static string SourceNotFound(int id) =>
        $"EF Model Source with ID {id} was not found. It may have been deleted.";

    public static string SourceInactive(string name) =>
        $"EF Model Source '{name}' is currently inactive. Enable it before running comparisons.";

    public static string InvalidSourceConfiguration(string name, string reason) =>
        $"Source '{name}' has an invalid configuration: {reason}";

    // Comparison Errors
    public static string ComparisonFailed(string reason) =>
        $"Comparison failed: {reason}. Check the source configuration and try again.";

    public static string NoDataInDictionary(string server, string database) =>
        $"No data found in the Data Dictionary for {server}/{database}. " +
        "Run a sync operation first to populate the database metadata.";

    // Generic Errors
    public static string UnexpectedError() =>
        "An unexpected error occurred. Please try again or contact support if the problem persists.";

    // Helper methods
    private static string SanitizePath(string path)
    {
        // Remove potentially sensitive parts of the path
        if (string.IsNullOrEmpty(path)) return "(empty)";

        // Just show filename for security
        try
        {
            return Path.GetFileName(path);
        }
        catch
        {
            return "(invalid path)";
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
```

### Step 6.3: Create Operation Result Types

**New File:** `src/NetSqlDataDicV2.Web/Models/OperationResult.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models;

/// <summary>
/// Generic result type for operations that can fail.
/// </summary>
public class OperationResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, object>? Details { get; init; }

    public static OperationResult Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static OperationResult Fail(string message, string? errorCode = null) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    public static OperationResult Fail(string message, Dictionary<string, object> details) =>
        new() { Success = false, Message = message, Details = details };
}

/// <summary>
/// Generic result type with data payload.
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; init; }

    public static OperationResult<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public new static OperationResult<T> Fail(string message, string? errorCode = null) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };
}
```

### Step 6.4: Update DynamicDllProvider with Better Error Handling

**Modified File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DynamicDllProvider.cs`

```csharp
using NetSqlDataDicV2.Web.Exceptions;
using NetSqlDataDicV2.Web.Helpers;

public class DynamicDllProvider : IDbContextProvider
{
    public DbContextProviderResult GetDbContext(EfModelSource source)
    {
        var assemblyPath = source.AssemblyPath!;

        try
        {
            // Validate DLL
            var validation = _validator.ValidateDll(assemblyPath);
            if (!validation.IsValid)
            {
                _logger.LogWarning("DLL validation failed for {Path}: {Error}",
                    assemblyPath, validation.ErrorMessage);

                return DbContextProviderResult.Fail(
                    GetUserFriendlyValidationMessage(validation.ErrorMessage!));
            }

            // Load assembly
            Assembly assembly;
            try
            {
                _loadContext = new PluginLoadContext(assemblyPath);
                assembly = _loadContext.LoadFromAssemblyPath(assemblyPath);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Assembly file not found: {Path}", assemblyPath);
                throw new DllLoadException(
                    assemblyPath,
                    DllLoadErrorType.FileNotFound,
                    ErrorMessages.DllNotFound(assemblyPath),
                    ex);
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "Invalid assembly format: {Path}", assemblyPath);
                throw new DllLoadException(
                    assemblyPath,
                    DllLoadErrorType.InvalidAssembly,
                    ErrorMessages.InvalidDll(assemblyPath),
                    ex);
            }
            catch (FileLoadException ex)
            {
                _logger.LogError(ex, "Failed to load assembly: {Path}", assemblyPath);

                // Check for dependency issues
                var missingDeps = TryIdentifyMissingDependencies(ex);
                if (missingDeps.Any())
                {
                    throw new DependencyResolutionException(
                        assemblyPath,
                        missingDeps,
                        ErrorMessages.MissingDependencies(missingDeps),
                        ex);
                }

                throw new DllLoadException(
                    assemblyPath,
                    DllLoadErrorType.LoadFailed,
                    "Failed to load assembly. Check that all dependencies are present.",
                    ex);
            }

            // Find DbContext type
            var dbContextType = FindDbContextType(assembly, source.DbContextTypeName);
            if (dbContextType == null)
            {
                var assemblyName = assembly.GetName().Name ?? "unknown";

                if (string.IsNullOrEmpty(source.DbContextTypeName))
                {
                    throw new DbContextCreationException(
                        "any",
                        assemblyPath,
                        DbContextCreationErrorType.TypeNotFound,
                        ErrorMessages.NoDbContextInAssembly(assemblyName));
                }

                throw new DbContextCreationException(
                    source.DbContextTypeName,
                    assemblyPath,
                    DbContextCreationErrorType.TypeNotFound,
                    ErrorMessages.DbContextNotFound(source.DbContextTypeName, assemblyName));
            }

            // Create instance
            var connectionString = DecryptConnectionString(source.ConnectionString);
            var context = CreateDbContextInstance(dbContextType, connectionString);

            if (context == null)
            {
                throw new DbContextCreationException(
                    dbContextType.FullName ?? dbContextType.Name,
                    assemblyPath,
                    DbContextCreationErrorType.ConstructorFailed,
                    ErrorMessages.DbContextConstructorFailed(dbContextType.Name));
            }

            _auditService.LogDllLoadAttempt(assemblyPath, true);

            return DbContextProviderResult.Ok(
                context,
                dbContextType.FullName ?? dbContextType.Name,
                _loadContext,
                assemblyPath);
        }
        catch (DllLoadException)
        {
            throw; // Re-throw custom exceptions
        }
        catch (DbContextCreationException)
        {
            throw;
        }
        catch (DependencyResolutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading DbContext from {Path}", assemblyPath);

            _auditService.LogDllLoadAttempt(assemblyPath, false, ex.Message);

            throw new DllLoadException(
                assemblyPath,
                DllLoadErrorType.UnknownError,
                ErrorMessages.UnexpectedError(),
                ex);
        }
    }

    private List<string> TryIdentifyMissingDependencies(Exception ex)
    {
        var missing = new List<string>();

        // Parse exception message for dependency hints
        var message = ex.ToString();

        if (message.Contains("Could not load file or assembly"))
        {
            // Try to extract assembly name
            var match = System.Text.RegularExpressions.Regex.Match(
                message,
                @"Could not load file or assembly '([^']+)'");

            if (match.Success)
            {
                missing.Add(match.Groups[1].Value);
            }
        }

        return missing;
    }

    private string GetUserFriendlyValidationMessage(string technicalMessage)
    {
        // Convert technical validation messages to user-friendly ones
        if (technicalMessage.Contains("not in an allowed directory"))
            return ErrorMessages.DllSecurityViolation();

        if (technicalMessage.Contains("not found"))
            return ErrorMessages.DllNotFound("");

        if (technicalMessage.Contains("not a valid .NET assembly"))
            return ErrorMessages.InvalidDll("");

        return technicalMessage;
    }

    private string? DecryptConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return null;

        try
        {
            return _connectionStringProtector.Unprotect(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt connection string");
            throw new InvalidOperationException(
                "Failed to decrypt connection string. The encryption key may have changed.");
        }
    }
}
```

### Step 6.5: Add Global Exception Handler Middleware

**New File:** `src/NetSqlDataDicV2.Web/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
using NetSqlDataDicV2.Web.Exceptions;
using NetSqlDataDicV2.Web.Helpers;
using System.Text.Json;

namespace NetSqlDataDicV2.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogError(exception,
            "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}",
            correlationId, context.Request.Path);

        // Determine response based on exception type
        var (statusCode, message) = exception switch
        {
            DllLoadException dle => (400, GetDllLoadErrorMessage(dle)),
            DbContextCreationException dce => (400, dce.Message),
            DependencyResolutionException dre => (400, dre.Message),
            EfModelSourceException ese => (400, ese.Message),
            ArgumentException ae => (400, ae.Message),
            InvalidOperationException ioe => (400, ioe.Message),
            _ => (500, ErrorMessages.UnexpectedError())
        };

        // For API requests, return JSON
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                error = message,
                correlationId,
                // Don't include stack trace in production
                details = IsDevelopment() ? exception.ToString() : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        else
        {
            // For page requests, redirect to error page
            context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(message)}&correlationId={correlationId}");
        }
    }

    private string GetDllLoadErrorMessage(DllLoadException ex)
    {
        return ex.ErrorType switch
        {
            DllLoadErrorType.FileNotFound => ErrorMessages.DllNotFound(ex.DllPath),
            DllLoadErrorType.InvalidAssembly => ErrorMessages.InvalidDll(ex.DllPath),
            DllLoadErrorType.SecurityViolation => ErrorMessages.DllSecurityViolation(),
            _ => ex.Message
        };
    }

    private bool IsDevelopment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
```

### Step 6.6: Add Structured Logging Configuration

**New File:** `src/NetSqlDataDicV2.Web/Configuration/LoggingConfiguration.cs`

```csharp
namespace NetSqlDataDicV2.Web.Configuration;

public static class LoggingConfiguration
{
    public static ILoggingBuilder ConfigureStructuredLogging(this ILoggingBuilder builder)
    {
        builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning);

        // Keep our logs at appropriate levels
        builder.AddFilter("NetSqlDataDicV2.Web.Services", LogLevel.Information);
        builder.AddFilter("NetSqlDataDicV2.Web.Services.Security", LogLevel.Information);

        return builder;
    }
}
```

### Step 6.7: Register Middleware

**Modified File:** `src/NetSqlDataDicV2.Web/Program.cs`

```csharp
// Add after building the app
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

### Step 6.8: Update Controller Error Handling

**Modified File:** `src/NetSqlDataDicV2.Web/Controllers/EfModelSourcesController.cs`

```csharp
[HttpPost]
public async Task<IActionResult> Validate(int id, CancellationToken ct)
{
    try
    {
        var result = await _sourceService.ValidateSourceAsync(id, ct);
        return Json(result);
    }
    catch (DllLoadException ex)
    {
        _logger.LogWarning(ex, "DLL validation failed for source {Id}", id);
        return Json(new ValidationResultViewModel
        {
            IsValid = false,
            ErrorMessage = ex.Message
        });
    }
    catch (DbContextCreationException ex)
    {
        _logger.LogWarning(ex, "DbContext creation failed for source {Id}", id);
        return Json(new ValidationResultViewModel
        {
            IsValid = false,
            ErrorMessage = ex.Message
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error validating source {Id}", id);
        return Json(new ValidationResultViewModel
        {
            IsValid = false,
            ErrorMessage = ErrorMessages.UnexpectedError()
        });
    }
}

[HttpPost]
public async Task<IActionResult> Delete(int id, CancellationToken ct)
{
    try
    {
        await _sourceService.DeleteAsync(id, ct);
        return Json(new { success = true, message = "Source deleted successfully." });
    }
    catch (ArgumentException ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete source {Id}", id);
        return Json(new { success = false, message = ErrorMessages.UnexpectedError() });
    }
}
```

## Testing Checklist

- [ ] DllLoadException thrown for missing files
- [ ] DllLoadException thrown for invalid assemblies
- [ ] DbContextCreationException thrown when type not found
- [ ] DependencyResolutionException includes missing dependency names
- [ ] Error messages don't expose sensitive paths
- [ ] Error messages are user-friendly
- [ ] API endpoints return JSON errors
- [ ] Page requests redirect to error page
- [ ] Correlation IDs appear in logs
- [ ] Stack traces only in development
- [ ] All exceptions properly logged

## Files Created

| File | Purpose |
|------|---------|
| `Exceptions/DllLoadException.cs` | DLL loading errors |
| `Exceptions/DbContextCreationException.cs` | DbContext creation errors |
| `Exceptions/DependencyResolutionException.cs` | Missing dependency errors |
| `Exceptions/EfModelSourceException.cs` | Source configuration errors |
| `Helpers/ErrorMessages.cs` | User-friendly messages |
| `Models/OperationResult.cs` | Result type pattern |
| `Middleware/ExceptionHandlingMiddleware.cs` | Global error handling |
| `Configuration/LoggingConfiguration.cs` | Logging setup |

## Files Modified

| File | Change Type |
|------|-------------|
| `Services/DbContextProviders/DynamicDllProvider.cs` | Better error handling |
| `Controllers/EfModelSourcesController.cs` | Exception handling |
| `Program.cs` | Middleware registration |

## Error Message Guidelines

1. **Be specific** - Tell users exactly what went wrong
2. **Be actionable** - Tell users what they can do to fix it
3. **Be safe** - Don't expose file paths, connection strings, or stack traces
4. **Be consistent** - Use centralized error messages
5. **Include context** - Add correlation IDs for support tickets

## Next Steps

After Phase 6 is complete, the DLL loading feature is ready for:
1. Internal testing with various DbContext patterns
2. Documentation for end users
3. Beta release to select users
4. Production deployment
