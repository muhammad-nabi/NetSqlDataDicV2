using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DataDictionary.AspNetCore.Core.Configuration;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Core.Services;
using DataDictionary.AspNetCore.Core.Services.DbContextProviders;
using DataDictionary.AspNetCore.Core.Services.Security;

namespace DataDictionary.AspNetCore.Core.Extensions;

/// <summary>
/// Extension methods for registering Data Dictionary Core services.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds Data Dictionary Core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="connectionStringName">The connection string name (default: "DataDictionary").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataDictionaryCore(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DataDictionary")
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString(connectionStringName);
        services.AddDbContext<DataDictionaryDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Security services
        services.AddScoped<IDllValidatorService, DllValidatorService>();
        services.AddScoped<IConnectionStringProtector, ConnectionStringProtector>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddSingleton<IDllShadowCopyService, DllShadowCopyService>();

        // DbContext provider services
        services.AddScoped<IDbContextProviderFactory, DbContextProviderFactory>();

        // Business services
        services.AddScoped<IDataDictionaryService, DataDictionaryService>();
        services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
        services.AddScoped<IComparisonService, ComparisonService>();
        services.AddScoped<IEfModelService, EfModelService>();
        services.AddScoped<IEfModelSourceService, EfModelSourceService>();

        // Configuration
        services.Configure<DllSecurityOptions>(
            configuration.GetSection(DllSecurityOptions.SectionName));

        return services;
    }
}
