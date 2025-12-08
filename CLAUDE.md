# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build solution
dotnet build

# Run web application (https://localhost:7074)
cd src/NetSqlDataDicV2.Web && dotnet run

# Run tests
dotnet test

# Run single test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Database Commands

```bash
# Create migration (run from src/NetSqlDataDicV2.Web)
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update <PreviousMigrationName>
```

## Architecture

**Solution Structure:**
- `src/NetSqlDataDicV2.Web` - ASP.NET Core MVC app (.NET 9) with Kendo UI
- `src/NetSqlDataDicV2.SourceModels` - EF Core scaffolded models from source databases
- `tests/NetSqlDataDicV2.Tests` - xUnit tests
- `docs/` - Phase documentation (architecture.md, phase1-5 specs)

**Data Flow:**
```
Razor Views + Kendo Grid → Controllers → Services → DataDictionaryDbContext → SQL Server
```

**Key Entities:**
- `DataElement` - Database column metadata (soft delete enabled via `IsDeleted` flag)
- `SyncHistory` - Tracks sync operations
- `SourceConnection` - Source database connection info
- `EfModelSource` - Configured EF Model sources for dynamic DLL loading

**Patterns Used:**
- Entity configurations via `IEntityTypeConfiguration<T>` in `/Data/Configurations/`
- Global query filter for soft deletes: `builder.HasQueryFilter(e => !e.IsDeleted)`
- Unique constraint on (DatabaseServer, DatabaseName, SchemaName, TableName, ColumnName)
- Service layer pattern: `IDataDictionaryService`, `IDatabaseSyncService`
- Provider pattern for DbContext loading: `IDbContextProvider`, `IDbContextProviderFactory`
- Plugin architecture with `AssemblyLoadContext` for runtime DLL loading

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Telerik.UI.for.AspNet.Core | 2024.1.130 | Kendo UI grids |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Database ORM |
| Microsoft.Data.SqlClient | 5.2.2 | SQL Server connectivity |

## Kendo UI License

The project uses Telerik Kendo UI which requires a license:
1. Add your license key to `wwwroot/js/kendo-ui-license.js`
2. Get your key from [Telerik Account](https://www.telerik.com/account/your-licenses)
3. The license file is gitignored to protect your key

## Sync Configuration

To sync from a source database, configure in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "SourceDatabase": "Server=...;Database=...;..."
  },
  "SourceDatabase": {
    "Server": "your-server",
    "Database": "your-database"
  }
}
```

## Project Phases

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Project setup, DbContext, migrations |
| 2 | Complete | Data Dictionary Kendo Grid UI |
| 3 | Complete | Database sync from source SQL Server |
| 4 | Complete | EF Core model comparison |
| 5 | Complete | Polish, error handling, CSV export |

Detailed specs for each phase are in `docs/phase*.md`.

## DLL-Based Loading Feature

Runtime loading of EF Core DbContexts from external DLLs for comparison without compile-time references.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Core infrastructure (providers, plugin context) |
| 2 | Complete | Service layer (factory pattern, EfModelService) |
| 3 | Complete | Data layer (EfModelSource entity, CRUD service) |
| 4 | Complete | UI layer (management pages, comparison integration) |
| 5 | Complete | Security & validation |
| 6 | Pending | Error handling |

Detailed specs in `docs/dll-loading-phases/`.

## Security Configuration

DLL loading security is configured in `appsettings.json` under `DllSecurity`:

```json
{
  "DllSecurity": {
    "AllowedDirectories": ["C:\\PluginDlls"],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600,
    "BlockedAssemblyNames": []
  }
}
```

**Security Services:**
- `IDllValidatorService` - Validates DLL paths and assemblies before loading
- `IConnectionStringProtector` - Encrypts connection strings at rest using Data Protection API
- `ISecurityAuditService` - Logs security events (DLL loads, source changes)
