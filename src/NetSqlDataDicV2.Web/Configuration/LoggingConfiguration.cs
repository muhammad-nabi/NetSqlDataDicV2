namespace NetSqlDataDicV2.Web.Configuration;

public static class LoggingConfiguration
{
    public static ILoggingBuilder ConfigureStructuredLogging(this ILoggingBuilder builder)
    {
        // Filter noisy framework logs
        builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning);

        // Keep our logs at appropriate levels
        builder.AddFilter("NetSqlDataDicV2.Web.Services", LogLevel.Information);
        builder.AddFilter("NetSqlDataDicV2.Web.Services.Security", LogLevel.Information);

        return builder;
    }
}
