# Phase 2: Service Layer Modifications

## Overview

This phase modifies the existing service layer to use the new provider abstraction, adds a factory pattern for provider selection, and extends the comparison service to support multiple EF model sources.

## Goals

- Create provider factory for selecting appropriate DbContext provider
- Modify `IEfModelService` to accept dynamic sources
- Modify `IComparisonService` to compare using configured sources
- Add DbContext discovery capability
- Maintain full backward compatibility with existing functionality

## Prerequisites

- Phase 1 completed (core infrastructure in place)
- Understanding of existing service interfaces

## Implementation Steps

### Step 2.1: Create Provider Factory

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/IDbContextProviderFactory.cs`

```csharp
namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Factory for creating and selecting appropriate DbContext providers.
/// </summary>
public interface IDbContextProviderFactory
{
    /// <summary>
    /// Gets a provider that can handle the given source.
    /// </summary>
    IDbContextProvider GetProvider(EfModelSource source);

    /// <summary>
    /// Gets all available provider type names.
    /// </summary>
    IEnumerable<string> GetAvailableProviderTypes();

    /// <summary>
    /// Discovers DbContext types in an assembly file.
    /// </summary>
    List<DbContextInfo> DiscoverDbContexts(string assemblyPath);
}
```

**New File:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DbContextProviderFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using System.Reflection;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Factory implementation for creating DbContext providers.
/// </summary>
public class DbContextProviderFactory : IDbContextProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbContextProviderFactory> _logger;
    private readonly ILogger<DynamicDllProvider> _dllProviderLogger;
    private readonly ILogger<DirectReferenceProvider> _directProviderLogger;

    private static readonly string[] ProviderTypes = { "Direct", "DynamicDll" };

    public DbContextProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<DbContextProviderFactory> logger,
        ILogger<DynamicDllProvider> dllProviderLogger,
        ILogger<DirectReferenceProvider> directProviderLogger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _dllProviderLogger = dllProviderLogger;
        _directProviderLogger = directProviderLogger;
    }

    public IDbContextProvider GetProvider(EfModelSource source)
    {
        _logger.LogDebug("Getting provider for source {Name} with type {Type}",
            source.Name, source.ProviderType);

        return source.ProviderType switch
        {
            "Direct" => new DirectReferenceProvider(_serviceProvider, _directProviderLogger),
            "DynamicDll" => new DynamicDllProvider(_dllProviderLogger),
            _ => throw new ArgumentException($"Unknown provider type: {source.ProviderType}")
        };
    }

    public IEnumerable<string> GetAvailableProviderTypes()
    {
        return ProviderTypes;
    }

    public List<DbContextInfo> DiscoverDbContexts(string assemblyPath)
    {
        var result = new List<DbContextInfo>();

        if (!File.Exists(assemblyPath))
        {
            _logger.LogWarning("Assembly file not found: {Path}", assemblyPath);
            return result;
        }

        PluginLoadContext? loadContext = null;

        try
        {
            _logger.LogInformation("Discovering DbContexts in {Path}", assemblyPath);

            loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            var dbContextBaseType = typeof(DbContext);

            var contextTypes = assembly.GetTypes()
                .Where(t => dbContextBaseType.IsAssignableFrom(t)
                    && !t.IsAbstract
                    && t != dbContextBaseType)
                .ToList();

            foreach (var contextType in contextTypes)
            {
                var info = new DbContextInfo
                {
                    Name = contextType.Name,
                    FullName = contextType.FullName ?? contextType.Name,
                    HasParameterlessConstructor = contextType.GetConstructor(Type.EmptyTypes) != null,
                    HasOptionsConstructor = HasDbContextOptionsConstructor(contextType)
                };

                // Try to get entity count from DbSet properties
                var dbSetProperties = contextType.GetProperties()
                    .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .ToList();

                info.EntityCount = dbSetProperties.Count;
                info.EntityNames = dbSetProperties
                    .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
                    .ToList();

                result.Add(info);

                _logger.LogDebug("Found DbContext: {Name} with {Count} entities",
                    info.Name, info.EntityCount);
            }

            _logger.LogInformation("Discovered {Count} DbContext types in {Path}",
                result.Count, assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover DbContexts in {Path}", assemblyPath);
        }
        finally
        {
            if (loadContext != null)
            {
                loadContext.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        return result;
    }

    private bool HasDbContextOptionsConstructor(Type contextType)
    {
        // Check for DbContextOptions<TContext>
        var typedOptionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
        if (contextType.GetConstructor(new[] { typedOptionsType }) != null)
            return true;

        // Check for DbContextOptions (non-generic)
        if (contextType.GetConstructor(new[] { typeof(DbContextOptions) }) != null)
            return true;

        return false;
    }
}
```

### Step 2.2: Modify IEfModelService Interface

**Modified File:** `src/NetSqlDataDicV2.Web/Services/IEfModelService.cs`

```csharp
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Services;

public interface IEfModelService
{
    /// <summary>
    /// Gets EF model columns from the directly referenced SourceDbContext.
    /// (Backward compatible - existing functionality)
    /// </summary>
    List<EfModelColumnDto> GetEfModelColumns();

    /// <summary>
    /// Gets EF model columns from a configured EfModelSource.
    /// </summary>
    List<EfModelColumnDto> GetEfModelColumns(EfModelSource source);

    /// <summary>
    /// Discovers available DbContext types in an assembly.
    /// </summary>
    List<DbContextInfo> DiscoverDbContexts(string assemblyPath);
}
```

### Step 2.3: Modify EfModelService Implementation

**Modified File:** `src/NetSqlDataDicV2.Web/Services/EfModelService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Web.Services;

public class EfModelService : IEfModelService
{
    private readonly SourceDbContext? _sourceContext;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<EfModelService> _logger;

    public EfModelService(
        IDbContextProviderFactory providerFactory,
        ILogger<EfModelService> logger,
        SourceDbContext? sourceContext = null)
    {
        _sourceContext = sourceContext;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets EF model columns from the directly referenced SourceDbContext.
    /// (Backward compatible)
    /// </summary>
    public List<EfModelColumnDto> GetEfModelColumns()
    {
        if (_sourceContext == null)
        {
            _logger.LogWarning("SourceDbContext is not available");
            return new List<EfModelColumnDto>();
        }

        return ExtractColumnsFromContext(_sourceContext);
    }

    /// <summary>
    /// Gets EF model columns from a configured EfModelSource.
    /// </summary>
    public List<EfModelColumnDto> GetEfModelColumns(EfModelSource source)
    {
        _logger.LogInformation("Getting EF model columns for source: {Name}", source.Name);

        using var provider = _providerFactory.GetProvider(source);
        using var result = provider.GetDbContext(source);

        if (!result.Success || result.Context == null)
        {
            _logger.LogError("Failed to get DbContext: {Error}", result.ErrorMessage);
            throw new InvalidOperationException(
                $"Failed to load DbContext for source '{source.Name}': {result.ErrorMessage}");
        }

        return ExtractColumnsFromContext(result.Context);
    }

    /// <summary>
    /// Discovers DbContext types in an assembly.
    /// </summary>
    public List<DbContextInfo> DiscoverDbContexts(string assemblyPath)
    {
        return _providerFactory.DiscoverDbContexts(assemblyPath);
    }

    /// <summary>
    /// Extracts column metadata from a DbContext's model.
    /// </summary>
    private List<EfModelColumnDto> ExtractColumnsFromContext(DbContext context)
    {
        var columns = new List<EfModelColumnDto>();
        var model = context.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            // Skip owned types (they're part of the owner entity's table)
            if (entityType.IsOwned())
                continue;

            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema() ?? "dbo";

            // Skip entities without a table mapping (e.g., query types)
            if (string.IsNullOrEmpty(tableName))
                continue;

            foreach (var property in entityType.GetProperties())
            {
                // Skip shadow properties unless they map to columns
                if (property.IsShadowProperty() && string.IsNullOrEmpty(property.GetColumnName()))
                    continue;

                var columnName = property.GetColumnName();

                // Skip properties without column mappings
                if (string.IsNullOrEmpty(columnName))
                    continue;

                columns.Add(new EfModelColumnDto
                {
                    EntityName = entityType.ClrType?.Name ?? entityType.Name,
                    PropertyName = property.Name,
                    ColumnName = columnName,
                    SchemaName = schemaName,
                    TableName = tableName,
                    ClrType = GetFriendlyTypeName(property.ClrType),
                    IsNullable = property.IsNullable,
                    MaxLength = property.GetMaxLength()
                });
            }
        }

        _logger.LogInformation("Extracted {Count} columns from DbContext model", columns.Count);

        return columns;
    }

    /// <summary>
    /// Converts CLR type to friendly C# type name.
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        var actualType = nullableUnderlying ?? type;

        var friendlyName = actualType.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "String" => "string",
            "Boolean" => "bool",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Guid" => "Guid",
            "DateTime" => "DateTime",
            "DateTimeOffset" => "DateTimeOffset",
            "DateOnly" => "DateOnly",
            "TimeOnly" => "TimeOnly",
            "TimeSpan" => "TimeSpan",
            "Byte[]" => "byte[]",
            _ => actualType.Name
        };

        return nullableUnderlying != null ? $"{friendlyName}?" : friendlyName;
    }
}
```

### Step 2.4: Modify IComparisonService Interface

**Modified File:** `src/NetSqlDataDicV2.Web/Services/IComparisonService.cs`

```csharp
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IComparisonService
{
    /// <summary>
    /// Compares database columns against the directly referenced SourceDbContext.
    /// (Backward compatible - existing functionality)
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares database columns against a configured EfModelSource.
    /// </summary>
    Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default);
}
```

### Step 2.5: Modify ComparisonService Implementation

**Modified File:** `src/NetSqlDataDicV2.Web/Services/ComparisonService.cs`

Add new method and inject `IEfModelSourceService`:

```csharp
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Services;

public class ComparisonService : IComparisonService
{
    private readonly IDataDictionaryService _dataDictionaryService;
    private readonly IEfModelService _efModelService;
    private readonly IEfModelSourceService _efModelSourceService;
    private readonly ILogger<ComparisonService> _logger;

    // SQL to CLR type mappings (existing code)
    private static readonly Dictionary<string, HashSet<string>> SqlToClrTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ... existing mappings ...
    };

    public ComparisonService(
        IDataDictionaryService dataDictionaryService,
        IEfModelService efModelService,
        IEfModelSourceService efModelSourceService,
        ILogger<ComparisonService> logger)
    {
        _dataDictionaryService = dataDictionaryService;
        _efModelService = efModelService;
        _efModelSourceService = efModelSourceService;
        _logger = logger;
    }

    /// <summary>
    /// Compares using the directly referenced SourceDbContext.
    /// (Backward compatible)
    /// </summary>
    public async Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting comparison for {Server}/{Database} using direct reference",
            server, database);

        var dbColumns = await _dataDictionaryService.GetByDatabaseAsync(server, database, cancellationToken);
        var efColumns = _efModelService.GetEfModelColumns();

        return PerformComparison(dbColumns, efColumns, server, database);
    }

    /// <summary>
    /// Compares using a configured EfModelSource.
    /// </summary>
    public async Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting comparison for source ID: {SourceId}", sourceId);

        var source = await _efModelSourceService.GetByIdAsync(sourceId, cancellationToken);

        if (source == null)
        {
            throw new ArgumentException($"EfModelSource with ID {sourceId} not found.");
        }

        if (!source.IsActive)
        {
            throw new InvalidOperationException($"EfModelSource '{source.Name}' is not active.");
        }

        var dbColumns = await _dataDictionaryService.GetByDatabaseAsync(
            source.TargetServer,
            source.TargetDatabase,
            cancellationToken);

        var efColumns = _efModelService.GetEfModelColumns(source);

        // Update last compared timestamp
        await _efModelSourceService.UpdateLastComparedAsync(sourceId, cancellationToken);

        return PerformComparison(dbColumns, efColumns, source.TargetServer, source.TargetDatabase);
    }

    /// <summary>
    /// Performs the actual comparison logic.
    /// </summary>
    private ComparisonResultViewModel PerformComparison(
        List<DataElementViewModel> dbColumns,
        List<EfModelColumnDto> efColumns,
        string server,
        string database)
    {
        // ... existing comparison logic extracted here ...
        // This is the same logic from the existing CompareAsync method
    }

    // ... existing helper methods (IsTypeCompatible, etc.) ...
}
```

### Step 2.6: Register Services in DI

**Modified File:** `src/NetSqlDataDicV2.Web/Program.cs`

Add registrations:

```csharp
// Add DbContext provider factory (always available)
builder.Services.AddScoped<IDbContextProviderFactory, DbContextProviderFactory>();

// Modify existing registrations to use new structure
if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddDbContext<SourceDbContext>(options =>
        options.UseSqlServer(sourceConnectionString));
}

// EfModelService - now always registered (can work without SourceDbContext)
builder.Services.AddScoped<IEfModelService, EfModelService>();

// ComparisonService - now always registered
builder.Services.AddScoped<IComparisonService, ComparisonService>();

// EfModelSourceService - new service
builder.Services.AddScoped<IEfModelSourceService, EfModelSourceService>();
```

## Testing Checklist

- [ ] `DbContextProviderFactory.GetProvider()` returns correct provider type
- [ ] `DbContextProviderFactory.DiscoverDbContexts()` finds DbContexts in DLL
- [ ] `DbContextProviderFactory.DiscoverDbContexts()` handles missing file gracefully
- [ ] `EfModelService.GetEfModelColumns()` works with direct reference (backward compat)
- [ ] `EfModelService.GetEfModelColumns(source)` works with DynamicDll provider
- [ ] `EfModelService.GetEfModelColumns(source)` works with Direct provider
- [ ] `ComparisonService.CompareAsync(server, database)` still works (backward compat)
- [ ] `ComparisonService.CompareAsync(sourceId)` works with configured source
- [ ] Services are correctly registered in DI container
- [ ] Null SourceDbContext doesn't break service initialization

## Files Modified

| File | Change Type |
|------|-------------|
| `Services/IEfModelService.cs` | Modified - added new method |
| `Services/EfModelService.cs` | Modified - added provider support |
| `Services/IComparisonService.cs` | Modified - added new method |
| `Services/ComparisonService.cs` | Modified - added source support |
| `Program.cs` | Modified - new service registrations |

## Files Created

| File | Purpose |
|------|---------|
| `Services/DbContextProviders/IDbContextProviderFactory.cs` | Factory interface |
| `Services/DbContextProviders/DbContextProviderFactory.cs` | Factory implementation |

## Next Phase

Phase 3 will add the data layer including the database migration for `EfModelSources` table and the `IEfModelSourceService` implementation.
