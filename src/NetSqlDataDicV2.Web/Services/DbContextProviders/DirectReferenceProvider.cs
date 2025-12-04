using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Models.Entities;

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
