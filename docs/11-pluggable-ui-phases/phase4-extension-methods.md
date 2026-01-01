# Phase 4: Configuration & Extension Methods

## Status: Pending

## Overview

Create the configuration options class and extension methods that enable consumers to easily integrate the Data Dictionary package.

## Goals

1. Create `DataDictionaryOptions` configuration class
2. Create `AddDataDictionary()` service collection extension
3. Create `UseDataDictionary()` application builder extension
4. Create `MapDataDictionary()` endpoint routing extension
5. Enable configuration via appsettings.json and code

## Files to Create

### 1. DataDictionaryOptions.cs

```csharp
namespace DataDictionary.AspNetCore.Configuration;

/// <summary>
/// Configuration options for the Data Dictionary package.
/// </summary>
public class DataDictionaryOptions
{
    /// <summary>
    /// The area name for routing. Default: "DataDictionary"
    /// </summary>
    public string AreaName { get; set; } = "DataDictionary";

    /// <summary>
    /// The route prefix for all Data Dictionary routes. Default: "tools/datadictionary"
    /// </summary>
    public string RoutePrefix { get; set; } = "tools/datadictionary";

    /// <summary>
    /// The connection string name to use. Default: "DataDictionary"
    /// </summary>
    public string ConnectionStringName { get; set; } = "DataDictionary";

    /// <summary>
    /// Whether to use the embedded layout. Default: true
    /// </summary>
    public bool UseEmbeddedLayout { get; set; } = true;

    /// <summary>
    /// Custom layout path when UseEmbeddedLayout is false.
    /// </summary>
    public string? CustomLayoutPath { get; set; }

    /// <summary>
    /// Whether to auto-migrate database on startup. Default: true
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Enable the database sync feature. Default: true
    /// </summary>
    public bool EnableSyncFeature { get; set; } = true;

    /// <summary>
    /// Enable the EF model comparison feature. Default: true
    /// </summary>
    public bool EnableComparisonFeature { get; set; } = true;

    /// <summary>
    /// Enable the EF model sources management. Default: true
    /// </summary>
    public bool EnableEfModelSources { get; set; } = true;

    /// <summary>
    /// DLL security options for assembly loading.
    /// </summary>
    public DllSecurityOptions DllSecurity { get; set; } = new();
}
```

**Location:** `DataDictionary.AspNetCore/Configuration/DataDictionaryOptions.cs`

### 2. ServiceCollectionExtensions.cs

```csharp
using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataDictionary.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring Data Dictionary services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Data Dictionary services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configure">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataDictionary(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DataDictionaryOptions>? configure = null)
    {
        // Create and configure options
        var options = new DataDictionaryOptions();
        configuration.GetSection("DataDictionary").Bind(options);
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Register Core services
        services.AddDataDictionaryCore(configuration);

        // Register DbContext
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{options.ConnectionStringName}' not found. " +
                "Add it to your appsettings.json or configure ConnectionStringName in options.");

        services.AddDbContext<DataDictionaryDbContext>(opts =>
            opts.UseSqlServer(connectionString));

        // Add Data Protection for connection string encryption
        services.AddDataProtection()
            .SetApplicationName("DataDictionary.AspNetCore");

        return services;
    }
}
```

**Location:** `DataDictionary.AspNetCore/Extensions/ServiceCollectionExtensions.cs`

### 3. ApplicationBuilderExtensions.cs

```csharp
using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring the Data Dictionary middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the Data Dictionary middleware and optionally applies migrations.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseDataDictionary(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<DataDictionaryOptions>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<DataDictionaryOptions>>();

        // Auto-migrate if enabled
        if (options.AutoMigrate)
        {
            logger.LogInformation("Data Dictionary: Auto-migration enabled, applying migrations...");

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

        // Ensure static files from RCL are served
        app.UseStaticFiles();

        return app;
    }
}
```

**Location:** `DataDictionary.AspNetCore/Extensions/ApplicationBuilderExtensions.cs`

### 4. EndpointRouteBuilderExtensions.cs

```csharp
using DataDictionary.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DataDictionary.AspNetCore.Extensions;

/// <summary>
/// Extension methods for mapping Data Dictionary routes.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Data Dictionary area routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePrefix">Optional custom route prefix. Uses configured value if null.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapDataDictionary(
        this IEndpointRouteBuilder endpoints,
        string? routePrefix = null)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<DataDictionaryOptions>();
        var prefix = routePrefix ?? options.RoutePrefix;

        endpoints.MapAreaControllerRoute(
            name: "DataDictionary",
            areaName: options.AreaName,
            pattern: $"{prefix}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        // API routes for AJAX endpoints
        endpoints.MapAreaControllerRoute(
            name: "DataDictionaryApi",
            areaName: options.AreaName,
            pattern: $"api/{prefix}/{{controller}}/{{action}}");

        return endpoints;
    }
}
```

**Location:** `DataDictionary.AspNetCore/Extensions/EndpointRouteBuilderExtensions.cs`

## Consumer Configuration Examples

### Basic Usage (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Data Dictionary with defaults
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

app.UseDataDictionary();
app.MapDataDictionary();

app.Run();
```

### Advanced Usage (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Data Dictionary with custom options
builder.Services.AddDataDictionary(builder.Configuration, options =>
{
    options.RoutePrefix = "admin/data-dictionary";
    options.AutoMigrate = false;  // Disable auto-migration
    options.EnableComparisonFeature = true;
    options.DllSecurity.AllowedDirectories.Add(@"C:\Plugins");
});

var app = builder.Build();

app.UseDataDictionary();
app.MapDataDictionary("custom/path");  // Override route at runtime

app.Run();
```

### Configuration via appsettings.json

```json
{
  "ConnectionStrings": {
    "DataDictionary": "Server=...;Database=DataDictionary;..."
  },
  "DataDictionary": {
    "RoutePrefix": "tools/datadictionary",
    "AutoMigrate": true,
    "EnableSyncFeature": true,
    "EnableComparisonFeature": true,
    "DllSecurity": {
      "AllowedDirectories": ["C:\\PluginDlls"],
      "MaxFileSizeBytes": 104857600
    }
  }
}
```

## Sample App Transformation

The `NetSqlDataDicV2.Web` project becomes the sample/demo app showing package integration.

### Before (85 lines)

```csharp
// Full Program.cs with manual service registration
var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var dataDictionaryConnectionString = builder.Configuration.GetConnectionString("DataDictionary")
    ?? throw new InvalidOperationException("...");

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => { ... });

builder.Services.AddDataProtection()
    .SetApplicationName("NetSqlDataDicV2");

builder.Services.Configure<DllSecurityOptions>(...);
builder.Services.AddScoped<IDllValidatorService, DllValidatorService>();
// ... 20+ more service registrations

var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();
// ... middleware pipeline
app.MapControllerRoute(...);
app.Run();
```

### After (Clean Consumer Experience)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

app.UseDataDictionary();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapDataDictionary();

app.Run();
```

**Location:** Update `src/NetSqlDataDicV2.Web/Program.cs` during Phase 4

## Verification Steps

1. Extension methods compile correctly
2. Options bind from configuration
3. DbContext registers with correct connection string
4. Routes map correctly
5. Auto-migration works when enabled

## Dependencies

- Phase 1 complete (project structure)
- Phase 2 complete (Core library with services)
- Phase 3 complete (UI package structure)

## Next Phase

[Phase 5: View Customization](phase5-view-customization.md)
