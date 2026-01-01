using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Middleware;
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
    /// <param name="configureMiddleware">Optional action to configure middleware options.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseDataDictionary(
        this IApplicationBuilder app,
        Action<DataDictionaryMiddlewareOptions>? configureMiddleware = null)
    {
        var options = app.ApplicationServices.GetRequiredService<DataDictionaryOptions>();
        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataDictionary.AspNetCore");

        // Configure middleware options
        var middlewareOptions = new DataDictionaryMiddlewareOptions();
        configureMiddleware?.Invoke(middlewareOptions);

        // Optionally add request logging middleware
        if (middlewareOptions.UseRequestLogging)
        {
            logger.LogInformation("Data Dictionary: Request logging middleware enabled");
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        // Optionally add exception handling middleware
        if (middlewareOptions.UseExceptionHandling)
        {
            logger.LogInformation("Data Dictionary: Exception handling middleware enabled");
            app.UseMiddleware<ExceptionHandlingMiddleware>(middlewareOptions.IncludeStackTraceInErrors);
        }

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
