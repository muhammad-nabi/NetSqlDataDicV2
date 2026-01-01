# Phase 8: Database Migrations

## Status: Pending

## Overview

Configure database migrations to support auto-migration on startup (user's choice) while also supporting manual migration workflows.

## Goals

1. Include migration files in Core package
2. Implement auto-migration in UseDataDictionary()
3. Support manual migration via EF CLI
4. Document migration strategies for consumers

## Migration Strategy: Auto-Migrate on Startup

Per user decision, the package will auto-migrate by default.

### Implementation in UseDataDictionary()

```csharp
public static IApplicationBuilder UseDataDictionary(
    this IApplicationBuilder app,
    Action<DataDictionaryMiddlewareOptions>? configureMiddleware = null)
{
    var options = app.ApplicationServices.GetRequiredService<DataDictionaryOptions>();
    var logger = app.ApplicationServices.GetRequiredService<ILogger<DataDictionaryOptions>>();

    // Auto-migrate if enabled (default: true)
    if (options.AutoMigrate)
    {
        logger.LogInformation("Data Dictionary: Auto-migration enabled, checking database...");

        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataDictionaryDbContext>();

            // Check if there are pending migrations
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Any())
            {
                logger.LogInformation(
                    "Data Dictionary: Applying {Count} pending migration(s): {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));

                db.Database.Migrate();

                logger.LogInformation("Data Dictionary: Migrations applied successfully");
            }
            else
            {
                logger.LogDebug("Data Dictionary: Database is up to date");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data Dictionary: Failed to apply migrations. " +
                "Set DataDictionary:AutoMigrate to false and run migrations manually.");
            throw new InvalidOperationException(
                "Data Dictionary failed to apply database migrations. " +
                "See inner exception for details.", ex);
        }
    }
    else
    {
        logger.LogInformation("Data Dictionary: Auto-migration disabled. " +
            "Ensure migrations are applied manually.");
    }

    // ... rest of middleware setup
}
```

## Migration Files Location

Migrations should be included in the Core package:

```
DataDictionary.AspNetCore.Core/
└── Migrations/
    ├── 20241201000000_InitialCreate.cs
    ├── 20241201000000_InitialCreate.Designer.cs
    ├── 20241204000000_AddEfModelSource.cs
    ├── 20241204000000_AddEfModelSource.Designer.cs
    ├── 20241210000000_AddDataElementAudit.cs
    ├── 20241210000000_AddDataElementAudit.Designer.cs
    ├── 20241218000000_AddDataElementNotes.cs
    ├── 20241218000000_AddDataElementNotes.Designer.cs
    └── DataDictionaryDbContextModelSnapshot.cs
```

## Design-Time DbContext Factory

Create a design-time factory for EF CLI support:

```csharp
namespace DataDictionary.AspNetCore.Core.Data;

/// <summary>
/// Factory for creating DbContext at design time (EF CLI migrations).
/// </summary>
public class DataDictionaryDbContextFactory : IDesignTimeDbContextFactory<DataDictionaryDbContext>
{
    public DataDictionaryDbContext CreateDbContext(string[] args)
    {
        // Try to get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("DataDictionary__ConnectionString")
            ?? "Server=(localdb)\\mssqllocaldb;Database=DataDictionary_Dev;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<DataDictionaryDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DataDictionaryDbContext(optionsBuilder.Options);
    }
}
```

**Location:** `DataDictionary.AspNetCore.Core/Data/DataDictionaryDbContextFactory.cs`

## Consumer Migration Commands

### When Auto-Migrate is Enabled (Default)

No action needed. Migrations apply automatically on first run.

### When Auto-Migrate is Disabled

Consumer runs migrations manually:

```bash
# Set connection string
export DataDictionary__ConnectionString="Server=...;Database=..."

# Add migration (if consumer modifies entities - not recommended)
dotnet ef migrations add CustomMigration \
    --context DataDictionaryDbContext \
    --project path/to/consumer.csproj

# Apply migrations
dotnet ef database update \
    --context DataDictionaryDbContext \
    --project path/to/consumer.csproj
```

### Using SQL Scripts

Generate SQL script for DBA review:

```bash
dotnet ef migrations script \
    --context DataDictionaryDbContext \
    --project path/to/consumer.csproj \
    --output migrations.sql
```

## Configuration Options

```json
{
  "DataDictionary": {
    "AutoMigrate": true,  // Default: true (user's choice)
    "ConnectionStringName": "DataDictionary"
  },
  "ConnectionStrings": {
    "DataDictionary": "Server=...;Database=DataDictionary;..."
  }
}
```

## Disable Auto-Migration

```csharp
builder.Services.AddDataDictionary(builder.Configuration, options =>
{
    options.AutoMigrate = false;  // Disable auto-migration
});
```

## Error Handling

If migration fails:

1. Log detailed error with correlation ID
2. Throw `InvalidOperationException` with guidance
3. Consumer sees clear error message about manual migration

```
Data Dictionary failed to apply database migrations.
Set DataDictionary:AutoMigrate to false and run:
  dotnet ef database update --context DataDictionaryDbContext
See inner exception for details.
```

## Verification Steps

1. Auto-migration works on clean database
2. Auto-migration is idempotent (safe to run multiple times)
3. Manual migration via EF CLI works
4. Error messages are clear when migration fails
5. Logging shows migration status

## Dependencies

- Phase 1-7 complete
- Core package with DbContext and entities

## Next Phase

[Phase 9: Static Asset Delivery](phase9-static-assets.md)
