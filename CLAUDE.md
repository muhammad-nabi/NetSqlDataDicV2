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
- `src/NetSqlDataDicV2.Web` - ASP.NET Core MVC app (.NET 9) with Bootstrap 5 + DataTables
- `tests/NetSqlDataDicV2.Tests` - xUnit tests
- `docs/` - Phase documentation (architecture.md, phase1-5 specs)

**Data Flow:**
```
Razor Views + DataTables → Controllers → Services → DataDictionaryDbContext → SQL Server
```

**Key Entities:**
- `DataElement` - Database column metadata (soft delete enabled via `IsDeleted` flag)
- `DataElementAudit` - Audit trail for schema changes during sync operations
- `DataElementNote` - User notes for data elements (append-only, timestamped)
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

**EF Model Comparison:**
- All EF model comparisons use dynamic DLL loading via `EfModelSource` configurations
- Configure DLL sources in the EF Model Sources management page
- No compile-time references to source databases required

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Database ORM |
| Microsoft.Data.SqlClient | 5.2.2 | SQL Server connectivity |
| DataTables.net | 1.13.7 (CDN) | Data grid tables |
| Bootstrap | 5.3.2 (CDN) | UI framework |

## Frontend Libraries

The project uses CDN-hosted JavaScript libraries (no licensing required):
- **jQuery 3.7.1** - DOM manipulation
- **Bootstrap 5.3.2** - UI components and styling
- **DataTables 1.13.7** - Grid tables with sorting, paging, filtering

## Sync Configuration

Database sync pulls schema metadata from a live SQL Server database into the Data Dictionary. Configure the target database connection when initiating a sync from the UI.

For EF model comparison, configure an `EfModelSource` via the management UI with:
- Path to the DLL containing your DbContext
- DbContext type name
- Connection string for model initialization
- Target server/database to compare against

## Project Phases

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Project setup, DbContext, migrations |
| 2 | Complete | Data Dictionary Grid UI |
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
| 6 | Complete | Error handling |

Detailed specs in `docs/dll-loading-phases/`.

## Simplification Project

Ongoing effort to reduce complexity and remove licensing dependencies.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Remove Direct Reference provider & SourceModels project |
| 2 | Complete | Replace Kendo UI with DataTables (free) |
| 3 | Complete | Cleanup navigation (remove Privacy page, simplify Home) |
| 4 | Complete | Configuration cleanup & final polish |

Detailed specs in `docs/simplification-phases/`.

## Audit Trail Feature

Tracks property-level changes to database columns during sync operations.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (DataElementAudit entity, configuration, migration) |
| 2 | Complete | Service layer (change detection, audit record creation) |
| 3 | Complete | ViewModel layer (audit history queries, display models) |
| 4 | Complete | UI layer (Details page, Deleted columns view) |

**Change Types Tracked:**
- `Added` - New column discovered during sync
- `Modified` - Property changed (DataType, MaxLength, IsNullable, etc.)
- `Deleted` - Column removed from source database (soft delete)
- `Restored` - Previously deleted column reappears

**Key Features:**
- Property-level tracking (e.g., "DataType: VARCHAR(50) → VARCHAR(100)")
- No audit record created if nothing changed during sync
- Details page accessible from Dictionary grid shows current state + audit history
- Separate "Deleted Columns" view for soft-deleted records

Detailed specs in `docs/audit-trail-phases/`.

## Notes Feature

Allows multiple timestamped notes per DataElement (database column).

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (DataElementNote entity, configuration, migration) |
| 2 | Complete | Service layer (ViewModels, service methods) |
| 3 | Complete | UI layer (Details page notes card, Add Note modal) |
| 4 | Complete | Cleanup (grid note count, documentation) |

**Key Features:**
- Append-only notes - maintains full history
- Notes displayed newest-first on Details page
- Grid shows note count badge linking to Details
- 2000 character limit per note
- Migrated existing Notes data from legacy single-value field

Detailed specs in `docs/notes-feature-phases/`.

## Skipped Tables Feature

Shows tables that exist in Data Dictionary but are completely absent from DbContext during EF model comparison.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (SkippedTableViewModel, result model update) |
| 2 | Complete | Service layer (table-level detection logic) |
| 3 | Complete | Controller layer (JSON response update) |
| 4 | Complete | UI layer (skipped tables section, DataTable) |

**Key Features:**
- Tables not in DbContext shown in separate "Skipped Tables" section
- Column-level mismatches (table in DbContext, column missing) stay in main grid
- Summary card shows skipped table count
- Collapsible skipped tables grid with schema, table name, column count

**Behavior:**
- Entire table missing from DbContext → Skipped Tables grid
- Table in DbContext, column missing → Main grid as "MissingInEfModel"
- No tables skipped → Skipped section hidden

Detailed specs in `docs/skipped-tables-phases/`.

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

## Error Handling

Custom exceptions for DLL loading operations in `/Exceptions/`:

| Exception | Purpose |
|-----------|---------|
| `DllLoadException` | Assembly loading failures (file not found, invalid format, security violation) |
| `DbContextCreationException` | DbContext instantiation failures (type not found, constructor failed) |
| `DependencyResolutionException` | Missing assembly dependencies |
| `EfModelSourceException` | Source configuration errors |

**Error Handling Infrastructure:**
- `ErrorMessages` helper (`/Helpers/`) - Centralized user-friendly messages with path sanitization
- `ExceptionHandlingMiddleware` (`/Middleware/`) - Global exception handler with correlation IDs
- `OperationResult<T>` (`/Models/`) - Generic result types for service operations

**Error Response Behavior:**
- API requests (`/api/*` or JSON Accept header): Returns JSON with error, correlationId, details (dev only)
- Page requests: Redirects to `/Home/Error` with message and correlationId
