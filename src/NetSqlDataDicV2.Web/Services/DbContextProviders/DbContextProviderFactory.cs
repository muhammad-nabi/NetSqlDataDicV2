using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services.Security;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Factory implementation for creating DbContext providers.
/// </summary>
public class DbContextProviderFactory : IDbContextProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDllValidatorService _dllValidator;
    private readonly IConnectionStringProtector _connectionStringProtector;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<DbContextProviderFactory> _logger;
    private readonly ILogger<DynamicDllProvider> _dllProviderLogger;

    private static readonly string[] ProviderTypes = ["DynamicDll"];

    public DbContextProviderFactory(
        IServiceProvider serviceProvider,
        IDllValidatorService dllValidator,
        IConnectionStringProtector connectionStringProtector,
        ISecurityAuditService auditService,
        ILogger<DbContextProviderFactory> logger,
        ILogger<DynamicDllProvider> dllProviderLogger)
    {
        _serviceProvider = serviceProvider;
        _dllValidator = dllValidator;
        _connectionStringProtector = connectionStringProtector;
        _auditService = auditService;
        _logger = logger;
        _dllProviderLogger = dllProviderLogger;
    }

    public IDbContextProvider GetProvider(EfModelSource source)
    {
        _logger.LogDebug("Getting provider for source {Name} with type {Type}",
            source.Name, source.ProviderType);

        return source.ProviderType switch
        {
            "DynamicDll" => new DynamicDllProvider(
                _dllValidator,
                _connectionStringProtector,
                _auditService,
                _dllProviderLogger),
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

        // Validate DLL before loading (security check)
        var validation = _dllValidator.ValidateDll(assemblyPath);
        if (!validation.IsValid)
        {
            _logger.LogWarning("DLL validation failed for {Path}: {Error}", assemblyPath, validation.ErrorMessage);
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
