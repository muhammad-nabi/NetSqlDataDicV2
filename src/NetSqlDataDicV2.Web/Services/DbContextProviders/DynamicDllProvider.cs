using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Entities;
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
        // Strategy 1: Try constructor with DbContextOptions<T>
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
