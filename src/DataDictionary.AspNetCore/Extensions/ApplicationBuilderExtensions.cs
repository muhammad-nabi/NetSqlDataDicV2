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
        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataDictionary.AspNetCore");

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
