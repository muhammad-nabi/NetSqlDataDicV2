using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services.Security;
using System.Reflection;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Provider that loads DbContext from external DLL files at runtime.
/// </summary>
public class DynamicDllProvider : IDbContextProvider
{
    private readonly IDllValidatorService _validator;
    private readonly IConnectionStringProtector _connectionStringProtector;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<DynamicDllProvider> _logger;
    private PluginLoadContext? _loadContext;
    private bool _disposed;

    public DynamicDllProvider(
        IDllValidatorService validator,
        IConnectionStringProtector connectionStringProtector,
        ISecurityAuditService auditService,
        ILogger<DynamicDllProvider> logger)
    {
        _validator = validator;
        _connectionStringProtector = connectionStringProtector;
        _auditService = auditService;
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

        // Validate DLL before loading (security check)
        var validation = _validator.ValidateDll(assemblyPath);
        if (!validation.IsValid)
        {
            _auditService.LogDllValidationFailure(assemblyPath, validation.ErrorMessage!);
            return DbContextProviderResult.Fail(validation.ErrorMessage!);
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

            // Decrypt connection string if needed
            var connectionString = source.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                connectionString = _connectionStringProtector.Unprotect(connectionString);
            }

            // Create DbContext instance
            var context = CreateDbContextInstance(dbContextType, connectionString);

            if (context == null)
            {
                _auditService.LogDllLoadAttempt(assemblyPath, false, $"Failed to create instance of {dbContextType.FullName}");
                return DbContextProviderResult.Fail($"Failed to create instance of {dbContextType.FullName}");
            }

            _logger.LogInformation("Successfully created DbContext instance: {Type}", dbContextType.FullName);
            _auditService.LogDllLoadAttempt(assemblyPath, true);

            return DbContextProviderResult.Ok(
                context,
                dbContextType.FullName ?? dbContextType.Name,
                _loadContext,
                assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DbContext from {Path}", assemblyPath);
            _auditService.LogDllLoadAttempt(assemblyPath, false, ex.Message);
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
        // Use provided connection string or a dummy one for model reflection
        // We need a connection string to configure the database provider
        var effectiveConnectionString = !string.IsNullOrEmpty(connectionString)
            ? connectionString
            : "Server=.;Database=DummyForModelReflection;Trusted_Connection=True;TrustServerCertificate=True;";

        // Strategy 1: Try constructor with DbContextOptions<T>
        var context = TryCreateWithOptions(dbContextType, effectiveConnectionString);
        if (context != null)
        {
            _logger.LogDebug("Created DbContext using DbContextOptions<{Type}>", dbContextType.Name);
            return context;
        }

        // Strategy 2: Try constructor with DbContextOptions (non-generic)
        var context2 = TryCreateWithGenericOptions(dbContextType, effectiveConnectionString);
        if (context2 != null)
        {
            _logger.LogDebug("Created DbContext using DbContextOptions");
            return context2;
        }

        // Strategy 3: Try parameterless constructor only if OnConfiguring sets up the provider
        // Note: This may fail at runtime if OnConfiguring doesn't configure a provider
        var parameterlessCtor = dbContextType.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
        {
            _logger.LogDebug("Creating DbContext using parameterless constructor (may fail if OnConfiguring is empty)");
            return (DbContext?)Activator.CreateInstance(dbContextType);
        }

        _logger.LogWarning("Could not find suitable constructor for {Type}. " +
            "Ensure the DbContext has a constructor accepting DbContextOptions<TContext>.",
            dbContextType.FullName);
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
                _logger.LogDebug("Found DbContextOptions<{Type}> constructor", dbContextType.Name);

                // Create DbContextOptionsBuilder<TContext> via reflection
                // This ensures we get DbContextOptions<TContext>, not DbContextOptions<DbContext>
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
                var optionsBuilder = Activator.CreateInstance(optionsBuilderType);

                // Cast to base DbContextOptionsBuilder and configure SQL Server
                // UseSqlServer extension method works on the base class
                var baseBuilder = (DbContextOptionsBuilder)optionsBuilder!;
                baseBuilder.UseSqlServer(connectionString);

                // Get the typed Options property (returns DbContextOptions<TContext>)
                // Use DeclaredOnly to avoid AmbiguousMatchException (generic class hides base Options property)
                var optionsProperty = optionsBuilderType.GetProperty("Options",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var typedOptions = optionsProperty!.GetValue(optionsBuilder);

                _logger.LogInformation("Creating DbContext with SQL Server provider configured");
                return (DbContext?)ctor.Invoke(new[] { typedOptions });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create DbContext with typed options for {Type}", dbContextType.Name);
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
