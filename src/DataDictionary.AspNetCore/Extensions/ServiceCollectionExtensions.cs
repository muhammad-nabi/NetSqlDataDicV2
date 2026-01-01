using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Extensions;
using Microsoft.AspNetCore.DataProtection;
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
        configuration.GetSection(DataDictionaryOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        // Register options as singleton
        services.AddSingleton(options);

        // Add MVC with JSON options
        services.AddControllersWithViews()
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        // Add Data Protection (required for connection string encryption)
        services.AddDataProtection()
            .SetApplicationName("DataDictionary.AspNetCore");

        // Register Core services (DbContext, business services, security services)
        services.AddDataDictionaryCore(configuration, options.ConnectionStringName);

        return services;
    }
}
