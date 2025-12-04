# Phase 1: Core Infrastructure

## Overview

This phase establishes the foundational abstractions and implementations for dynamically loading DbContext instances from external DLL files at runtime.

## Goals

- Create provider abstraction layer for DbContext loading
- Implement isolated assembly loading using `AssemblyLoadContext`
- Define configuration entity for EF Model Sources
- Maintain backward compatibility with existing direct-reference approach

## Prerequisites

- .NET 9.0 SDK
- Understanding of `System.Runtime.Loader` namespace
- Familiarity with EF Core DbContext lifecycle

## Implementation Steps

### Step 1.1: Create DbContext Provider Interfaces

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/IDbContextProvider.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using System.Runtime.Loader;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Defines a provider that can create DbContext instances from various sources.
/// </summary>
public interface IDbContextProvider : IDisposable
{
    /// <summary>
    /// Gets the unique name identifying this provider type.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Determines if this provider can handle the given source configuration.
    /// </summary>
    bool CanProvide(EfModelSource source);

    /// <summary>
    /// Creates a DbContext instance from the given source configuration.
    /// </summary>
    DbContextProviderResult GetDbContext(EfModelSource source);
}
```

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DbContextProviderResult.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using System.Runtime.Loader;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Result of a DbContext provider operation, including the context and metadata.
/// </summary>
public class DbContextProviderResult : IDisposable
{
    /// <summary>
    /// The loaded DbContext instance. Null if loading failed.
    /// </summary>
    public DbContext? Context { get; init; }

    /// <summary>
    /// Indicates whether the DbContext was successfully loaded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The AssemblyLoadContext used to load the assembly (for cleanup).
    /// Only set for dynamically loaded assemblies.
    /// </summary>
    public AssemblyLoadContext? LoadContext { get; init; }

    /// <summary>
    /// Name of the DbContext type that was loaded.
    /// </summary>
    public string? DbContextTypeName { get; init; }

    /// <summary>
    /// Path to the assembly that was loaded (for DynamicDll provider).
    /// </summary>
    public string? AssemblyPath { get; init; }

    public void Dispose()
    {
        Context?.Dispose();

        // Unload the assembly context if it was dynamically loaded
        if (LoadContext is { IsCollectible: true })
        {
            LoadContext.Unload();
        }
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DbContextProviderResult Ok(
        DbContext context,
        string dbContextTypeName,
        AssemblyLoadContext? loadContext = null,
        string? assemblyPath = null)
    {
        return new DbContextProviderResult
        {
            Context = context,
            Success = true,
            DbContextTypeName = dbContextTypeName,
            LoadContext = loadContext,
            AssemblyPath = assemblyPath
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DbContextProviderResult Fail(string errorMessage)
    {
        return new DbContextProviderResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
```

### Step 1.2: Create EfModelSource Entity

**New File:** `src/NetSqlDataDicV2.Web/Models/Entities/EfModelSource.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.Entities;

/// <summary>
/// Represents a configured source for EF Core model metadata.
/// </summary>
public class EfModelSource
{
    public int Id { get; set; }

    /// <summary>
    /// Display name for this source (e.g., "Sales API DbContext").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider type: "Direct" (compile-time reference) or "DynamicDll" (runtime loading).
    /// </summary>
    public string ProviderType { get; set; } = "Direct";

    /// <summary>
    /// Full path to the DLL file (for DynamicDll provider).
    /// </summary>
    public string? AssemblyPath { get; set; }

    /// <summary>
    /// Fully qualified type name of the DbContext class.
    /// Example: "MyApp.Data.ApplicationDbContext"
    /// </summary>
    public string? DbContextTypeName { get; set; }

    /// <summary>
    /// Connection string for the DbContext (encrypted at rest).
    /// Used to instantiate the DbContext for model reflection.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Target database server name to compare against in Data Dictionary.
    /// </summary>
    public string TargetServer { get; set; } = string.Empty;

    /// <summary>
    /// Target database name to compare against in Data Dictionary.
    /// </summary>
    public string TargetDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Whether this source is active and available for comparison.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this source configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When a comparison was last performed using this source.
    /// </summary>
    public DateTime? LastComparedAt { get; set; }

    /// <summary>
    /// Optional description or notes about this source.
    /// </summary>
    public string? Description { get; set; }
}
```

**New File:** `src/NetSqlDataDicV2.Web/Data/Configurations/EfModelSourceConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class EfModelSourceConfiguration : IEntityTypeConfiguration<EfModelSource>
{
    public void Configure(EntityTypeBuilder<EfModelSource> builder)
    {
        builder.ToTable("EfModelSources");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ProviderType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AssemblyPath)
            .HasMaxLength(500);

        builder.Property(e => e.DbContextTypeName)
            .HasMaxLength(500);

        builder.Property(e => e.ConnectionString)
            .HasMaxLength(2000);

        builder.Property(e => e.TargetServer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.TargetDatabase)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Index for quick lookup by target database
        builder.HasIndex(e => new { e.TargetServer, e.TargetDatabase });

        // Index for active sources
        builder.HasIndex(e => e.IsActive);
    }
}
```

### Step 1.3: Implement Plugin Load Context

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/PluginLoadContext.cs`

```csharp
using System.Reflection;
using System.Runtime.Loader;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Custom AssemblyLoadContext for loading plugin assemblies in isolation.
/// Marked as collectible to allow unloading when no longer needed.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // First, try to resolve from the plugin's dependencies
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // For shared framework assemblies, return null to let the default context handle it
        // This prevents loading duplicate copies of Microsoft.EntityFrameworkCore, etc.
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
```

### Step 1.4: Implement Dynamic DLL Provider

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DynamicDllProvider.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Provider that loads DbContext from external DLL files at runtime.
/// </summary>
public class DynamicDllProvider : IDbContextProvider
{
    private readonly ILogger<DynamicDllProvider> _logger;
    private PluginLoadContext? _loadContext;
    private bool _disposed;

    public DynamicDllProvider(ILogger<DynamicDllProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "DynamicDll";

    public bool CanProvide(EfModelSource source)
    {
        return source.ProviderType == "DynamicDll"
            && !string.IsNullOrEmpty(source.AssemblyPath);
    }

    public DbContextProviderResult GetDbContext(EfModelSource source)
    {
        if (!CanProvide(source))
        {
            return DbContextProviderResult.Fail("Invalid source configuration for DynamicDll provider.");
        }

        var assemblyPath = source.AssemblyPath!;

        // Validate file exists
        if (!File.Exists(assemblyPath))
        {
            _logger.LogError("Assembly file not found: {Path}", assemblyPath);
            return DbContextProviderResult.Fail($"Assembly file not found: {assemblyPath}");
        }

        try
        {
            _logger.LogInformation("Loading assembly from {Path}", assemblyPath);

            // Create isolated load context
            _loadContext = new PluginLoadContext(assemblyPath);

            // Load the assembly
            var assembly = _loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find DbContext type
            var dbContextType = FindDbContextType(assembly, source.DbContextTypeName);

            if (dbContextType == null)
            {
                var message = string.IsNullOrEmpty(source.DbContextTypeName)
                    ? "No DbContext type found in assembly."
                    : $"DbContext type '{source.DbContextTypeName}' not found in assembly.";

                _logger.LogError(message);
                return DbContextProviderResult.Fail(message);
            }

            _logger.LogInformation("Found DbContext type: {Type}", dbContextType.FullName);

            // Create DbContext instance
            var context = CreateDbContextInstance(dbContextType, source.ConnectionString);

            if (context == null)
            {
                return DbContextProviderResult.Fail($"Failed to create instance of {dbContextType.FullName}");
            }

            _logger.LogInformation("Successfully created DbContext instance: {Type}", dbContextType.FullName);

            return DbContextProviderResult.Ok(
                context,
                dbContextType.FullName ?? dbContextType.Name,
                _loadContext,
                assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DbContext from {Path}", assemblyPath);
            return DbContextProviderResult.Fail($"Failed to load DbContext: {ex.Message}");
        }
    }

    private Type? FindDbContextType(Assembly assembly, string? specifiedTypeName)
    {
        var dbContextBaseType = typeof(DbContext);

        if (!string.IsNullOrEmpty(specifiedTypeName))
        {
            // Look for specific type by name
            var type = assembly.GetType(specifiedTypeName);

            if (type != null && dbContextBaseType.IsAssignableFrom(type))
            {
                return type;
            }

            // Try partial match (class name only)
            type = assembly.GetTypes()
                .FirstOrDefault(t =>
                    dbContextBaseType.IsAssignableFrom(t)
                    && !t.IsAbstract
                    && (t.Name == specifiedTypeName || t.FullName == specifiedTypeName));

            return type;
        }

        // Find first DbContext in assembly
        return assembly.GetTypes()
            .FirstOrDefault(t =>
                dbContextBaseType.IsAssignableFrom(t)
                && !t.IsAbstract
                && t != dbContextBaseType);
    }

    private DbContext? CreateDbContextInstance(Type dbContextType, string? connectionString)
    {
        // Strategy 1: Try constructor with DbContextOptions
        if (!string.IsNullOrEmpty(connectionString))
        {
            var context = TryCreateWithOptions(dbContextType, connectionString);
            if (context != null) return context;
        }

        // Strategy 2: Try parameterless constructor
        var parameterlessCtor = dbContextType.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
        {
            _logger.LogDebug("Creating DbContext using parameterless constructor");
            return (DbContext?)Activator.CreateInstance(dbContextType);
        }

        // Strategy 3: Try constructor with DbContextOptions (generic)
        var context2 = TryCreateWithGenericOptions(dbContextType, connectionString);
        if (context2 != null) return context2;

        _logger.LogWarning("Could not find suitable constructor for {Type}", dbContextType.FullName);
        return null;
    }

    private DbContext? TryCreateWithOptions(Type dbContextType, string connectionString)
    {
        try
        {
            // Look for constructor accepting DbContextOptions<TContext>
            var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
            var ctor = dbContextType.GetConstructor(new[] { optionsType });

            if (ctor != null)
            {
                _logger.LogDebug("Creating DbContext using DbContextOptions<{Type}> constructor", dbContextType.Name);

                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
                var optionsBuilder = Activator.CreateInstance(optionsBuilderType);

                // Call UseSqlServer extension method
                var useSqlServerMethod = typeof(SqlServerDbContextOptionsExtensions)
                    .GetMethods()
                    .First(m => m.Name == "UseSqlServer"
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType == typeof(string));

                var genericMethod = useSqlServerMethod.MakeGenericMethod(dbContextType);
                genericMethod.Invoke(null, new[] { optionsBuilder, connectionString });

                // Get Options property
                var optionsProperty = optionsBuilderType.GetProperty("Options");
                var options = optionsProperty?.GetValue(optionsBuilder);

                return (DbContext?)Activator.CreateInstance(dbContextType, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to create DbContext with typed options");
        }

        return null;
    }

    private DbContext? TryCreateWithGenericOptions(Type dbContextType, string? connectionString)
    {
        try
        {
            // Look for constructor accepting DbContextOptions (non-generic)
            var ctor = dbContextType.GetConstructor(new[] { typeof(DbContextOptions) });

            if (ctor != null && !string.IsNullOrEmpty(connectionString))
            {
                _logger.LogDebug("Creating DbContext using DbContextOptions constructor");

                var optionsBuilder = new DbContextOptionsBuilder();
                optionsBuilder.UseSqlServer(connectionString);

                return (DbContext?)Activator.CreateInstance(dbContextType, optionsBuilder.Options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to create DbContext with generic options");
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _loadContext?.Unload();
        _loadContext = null;
        _disposed = true;

        // Request garbage collection to clean up unloaded assemblies
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
```

### Step 1.5: Implement Direct Reference Provider

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DirectReferenceProvider.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.SourceModels;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Provider that uses the directly referenced SourceDbContext (backward compatibility).
/// </summary>
public class DirectReferenceProvider : IDbContextProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DirectReferenceProvider> _logger;

    public DirectReferenceProvider(
        IServiceProvider serviceProvider,
        ILogger<DirectReferenceProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string ProviderName => "Direct";

    public bool CanProvide(EfModelSource source)
    {
        return source.ProviderType == "Direct";
    }

    public DbContextProviderResult GetDbContext(EfModelSource source)
    {
        if (!CanProvide(source))
        {
            return DbContextProviderResult.Fail("Invalid source configuration for Direct provider.");
        }

        try
        {
            // Get SourceDbContext from DI container
            var context = _serviceProvider.GetService<SourceDbContext>();

            if (context == null)
            {
                _logger.LogError("SourceDbContext is not registered in DI container");
                return DbContextProviderResult.Fail(
                    "SourceDbContext is not configured. Check connection string in appsettings.json.");
            }

            _logger.LogInformation("Using directly referenced SourceDbContext");

            return DbContextProviderResult.Ok(
                context,
                typeof(SourceDbContext).FullName ?? nameof(SourceDbContext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SourceDbContext from DI container");
            return DbContextProviderResult.Fail($"Failed to get SourceDbContext: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // Nothing to dispose - context is managed by DI container
    }
}
```

### Step 1.6: Create DbContext Info DTO

**New File:** `src/NetSqlDataDicV2.Web/Models/Dto/DbContextInfo.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.Dto;

/// <summary>
/// Information about a discovered DbContext type in an assembly.
/// </summary>
public class DbContextInfo
{
    /// <summary>
    /// Short name of the DbContext class.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified type name including namespace.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Number of entity types (DbSet properties) in the context.
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Names of the entity types in the context.
    /// </summary>
    public List<string> EntityNames { get; set; } = new();

    /// <summary>
    /// Whether the DbContext has a parameterless constructor.
    /// </summary>
    public bool HasParameterlessConstructor { get; set; }

    /// <summary>
    /// Whether the DbContext has a constructor accepting DbContextOptions.
    /// </summary>
    public bool HasOptionsConstructor { get; set; }
}
```

## Testing Checklist

- [ ] `PluginLoadContext` can load an assembly from file path
- [ ] `PluginLoadContext` resolves dependencies from the plugin directory
- [ ] `PluginLoadContext` falls back to default context for shared assemblies
- [ ] `DynamicDllProvider.CanProvide()` returns correct values
- [ ] `DynamicDllProvider.GetDbContext()` loads valid DLL successfully
- [ ] `DynamicDllProvider.GetDbContext()` returns error for missing file
- [ ] `DynamicDllProvider.GetDbContext()` returns error for invalid assembly
- [ ] `DynamicDllProvider.GetDbContext()` finds DbContext by full name
- [ ] `DynamicDllProvider.GetDbContext()` finds DbContext by class name only
- [ ] `DynamicDllProvider.GetDbContext()` finds first DbContext when no name specified
- [ ] `DynamicDllProvider.Dispose()` unloads assembly context
- [ ] `DirectReferenceProvider` returns SourceDbContext from DI
- [ ] `DbContextProviderResult.Dispose()` cleans up resources
- [ ] `EfModelSource` entity is properly configured in EF

## Files Created

| File | Purpose |
|------|---------|
| `Services/DbContextProviders/IDbContextProvider.cs` | Provider interface |
| `Services/DbContextProviders/DbContextProviderResult.cs` | Result wrapper with disposal |
| `Services/DbContextProviders/PluginLoadContext.cs` | Isolated assembly loading |
| `Services/DbContextProviders/DynamicDllProvider.cs` | Runtime DLL loading |
| `Services/DbContextProviders/DirectReferenceProvider.cs` | Backward compatibility |
| `Models/Entities/EfModelSource.cs` | Source configuration entity |
| `Models/Dto/DbContextInfo.cs` | DbContext discovery DTO |
| `Data/Configurations/EfModelSourceConfiguration.cs` | EF configuration |

## Dependencies Added

None required - uses built-in .NET libraries:
- `System.Runtime.Loader` (included in .NET runtime)
- `System.Reflection` (included in .NET runtime)

## Next Phase

Phase 2 will modify the existing service layer to use the new provider abstraction and add the factory pattern for provider selection.
