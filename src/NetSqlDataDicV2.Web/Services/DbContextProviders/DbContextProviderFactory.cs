using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;

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

    private static readonly string[] ProviderTypes = ["Direct", "DynamicDll"];

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

    private static bool HasDbContextOptionsConstructor(Type contextType)
    {
        // Check for DbContextOptions<TContext>
        var typedOptionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
        if (contextType.GetConstructor([typedOptionsType]) != null)
            return true;

        // Check for DbContextOptions (non-generic)
        if (contextType.GetConstructor([typeof(DbContextOptions)]) != null)
            return true;

        return false;
    }
}
