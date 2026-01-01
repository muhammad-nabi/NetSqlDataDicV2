# Phase 7: Middleware Integration

## Status: Complete

## Overview

Make middleware components optional and configurable for consumers who may have their own logging and error handling.

## Goals

1. Move middleware to UI package
2. Make middleware registration optional
3. Provide configuration options for middleware behavior
4. Document middleware integration for consumers

## Middleware Files to Move

| Source | Destination |
|--------|-------------|
| `Middleware/RequestLoggingMiddleware.cs` | `DataDictionary.AspNetCore/Middleware/RequestLoggingMiddleware.cs` |
| `Middleware/ExceptionHandlingMiddleware.cs` | `DataDictionary.AspNetCore/Middleware/ExceptionHandlingMiddleware.cs` |

## Middleware Options Class

```csharp
namespace DataDictionary.AspNetCore.Configuration;

/// <summary>
/// Configuration options for Data Dictionary middleware.
/// </summary>
public class DataDictionaryMiddlewareOptions
{
    /// <summary>
    /// Enable request logging middleware. Default: false
    /// Most consumers have their own logging middleware.
    /// </summary>
    public bool UseRequestLogging { get; set; } = false;

    /// <summary>
    /// Enable Data Dictionary exception handling middleware. Default: false
    /// Most consumers have their own exception handling.
    /// </summary>
    public bool UseExceptionHandling { get; set; } = false;

    /// <summary>
    /// Include stack traces in error responses (development only). Default: false
    /// </summary>
    public bool IncludeStackTraceInErrors { get; set; } = false;
}
```

**Location:** `DataDictionary.AspNetCore/Configuration/DataDictionaryMiddlewareOptions.cs`

## Updated ApplicationBuilderExtensions

```csharp
using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Data Dictionary middleware and optionally applies migrations.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configureMiddleware">Optional middleware configuration.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseDataDictionary(
        this IApplicationBuilder app,
        Action<DataDictionaryMiddlewareOptions>? configureMiddleware = null)
    {
        var options = app.ApplicationServices.GetRequiredService<DataDictionaryOptions>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<DataDictionaryOptions>>();

        // Configure middleware options
        var middlewareOptions = new DataDictionaryMiddlewareOptions();
        configureMiddleware?.Invoke(middlewareOptions);

        // Optionally add request logging
        if (middlewareOptions.UseRequestLogging)
        {
            logger.LogInformation("Data Dictionary: Request logging middleware enabled");
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        // Optionally add exception handling
        if (middlewareOptions.UseExceptionHandling)
        {
            logger.LogInformation("Data Dictionary: Exception handling middleware enabled");
            app.UseMiddleware<ExceptionHandlingMiddleware>(middlewareOptions.IncludeStackTraceInErrors);
        }

        // Auto-migrate if enabled
        if (options.AutoMigrate)
        {
            logger.LogInformation("Data Dictionary: Applying database migrations...");

            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataDictionaryDbContext>();
                db.Database.Migrate();
                logger.LogInformation("Data Dictionary: Migrations applied successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Data Dictionary: Failed to apply migrations");
                throw;
            }
        }

        // Ensure static files are served
        app.UseStaticFiles();

        return app;
    }
}
```

## Consumer Usage Examples

### Default (No Package Middleware)

```csharp
var app = builder.Build();

// Consumer uses their own middleware
app.UseExceptionHandler("/Error");
app.UseSerilogRequestLogging();

app.UseDataDictionary();  // No middleware enabled by default
app.MapDataDictionary();
```

### With Package Middleware

```csharp
var app = builder.Build();

app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
    middleware.IncludeStackTraceInErrors = app.Environment.IsDevelopment();
});

app.MapDataDictionary();
```

## RequestLoggingMiddleware Updates

Update namespace and make configurable:

```csharp
namespace DataDictionary.AspNetCore.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log Data Dictionary area requests
        if (!context.Request.Path.StartsWithSegments("/tools/datadictionary") &&
            !context.Request.Path.StartsWithSegments("/api/tools/datadictionary"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.ElapsedMilliseconds);
        }
    }

    private void LogRequest(HttpContext context, long durationMs)
    {
        var statusCode = context.Response.StatusCode;
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "DataDictionary {Method} {Path}{Query} responded {StatusCode} in {Duration}ms",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            statusCode,
            durationMs);
    }
}
```

## ExceptionHandlingMiddleware Updates

```csharp
namespace DataDictionary.AspNetCore.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _includeStackTrace;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        bool includeStackTrace = false)
    {
        _next = next;
        _logger = logger;
        _includeStackTrace = includeStackTrace;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only handle Data Dictionary area requests
        if (!context.Request.Path.StartsWithSegments("/tools/datadictionary") &&
            !context.Request.Path.StartsWithSegments("/api/tools/datadictionary"))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    // ... rest of implementation
}
```

## Verification Steps

1. Package builds without middleware by default
2. Consumer can enable middleware via options
3. Middleware only applies to Data Dictionary routes
4. Consumer's own middleware works alongside

## Dependencies

- Phase 1-6 complete

## Next Phase

[Phase 8: Database Migrations](phase8-migrations.md)
