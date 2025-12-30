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
- `tests/NetSqlDataDicV2.Tests` - xUnit tests with Moq & FluentAssertions
  - `TestHelpers/` - Test infrastructure (TestDbContextFactory, TestDataBuilder)
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
| 7 | Complete | Shadow copy (hot-reload support) |

**Hot-Reload Support:**
- DLLs are shadow-copied to temp directory before loading
- Original DLL can be updated while app is running
- Changes reflected on next comparison without restart
- Shadow copies cleaned up automatically

Detailed specs in `docs/01-dll-loading-phases/`.

## Simplification Project

Ongoing effort to reduce complexity and remove licensing dependencies.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Remove Direct Reference provider & SourceModels project |
| 2 | Complete | Replace Kendo UI with DataTables (free) |
| 3 | Complete | Cleanup navigation (remove Privacy page, simplify Home) |
| 4 | Complete | Configuration cleanup & final polish |

Detailed specs in `docs/02-simplification-phases/`.

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

Detailed specs in `docs/03-audit-trail-phases/`.

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

Detailed specs in `docs/04-notes-feature-phases/`.

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

Detailed specs in `docs/05-skipped-tables-phases/`.

## Sync Results Feature

Shows detailed results after a sync operation with all changes (Added, Modified, Deleted, Restored).

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (SyncResultsViewModel, SyncAuditItemViewModel) |
| 2 | Complete | Service layer (GetSyncResultsAsync method) |
| 3 | Complete | Controller layer (Results action) |
| 4 | Complete | UI layer (Results.cshtml with summary cards + DataTable) |
| 5 | Complete | Update sync page (View Results buttons) |

**Key Features:**
- Results page accessible at `/Sync/Results/{syncHistoryId}`
- Summary cards showing counts by change type (Added, Modified, Deleted, Restored)
- Filterable DataTable grid with all audit records
- Column links navigate to Details page for deeper investigation
- "View Results" button appears after sync completes
- "View" button in sync history table for each completed/failed sync

**Navigation:**
- After sync: "View Results" button links to results page
- History table: "View" button for each sync row
- Back navigation returns to Sync Index

Detailed specs in `docs/06-sync-results-phases/`.

## Constraint Comparison Feature

Compares EF Core model constraints against Data Dictionary values for MaxLength, IsNullable, Precision, Scale, and IsPrimaryKey.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Data layer (EfModelColumnDto extension) |
| 2 | Complete | Service layer (Extract constraints from EF metadata) |
| 3 | Complete | ViewModel layer (ConstraintMismatch status) |
| 4 | Complete | Comparison logic (CompareConstraints method) |
| 5 | Complete | Controller layer (JSON response update) |
| 6 | Complete | UI layer (Purple badge, summary card, filter, grid column) |

**Key Features:**
- Detects mismatches for MaxLength, IsNullable, Precision, Scale, IsPrimaryKey
- New "Constraint Mismatch" status (purple badge)
- Summary card shows constraint mismatch count
- Filter button to view only constraint mismatches
- Constraint Details column in comparison grid
- Type mismatch takes precedence over constraint mismatch

**Status Priority:** TypeMismatch > ConstraintMismatch > Match

Detailed specs in `docs/07-constraint-comparison-phases/`.

## Logging Improvements

Enhanced application logging for observability, debugging, and performance monitoring.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Quick wins (unused logger fix, EF SQL logging, shadow copy warning) |
| 2 | Complete | Operational visibility (request middleware, success logging, timing) |
| 3 | Pending | User context & audit (requires authentication) |

**Key Features:**
- `RequestLoggingMiddleware` - Logs all HTTP requests with method, path, query, status, duration
- Controller success logging for sync, comparison, CRUD operations
- Phase-by-phase performance timing in `DatabaseSyncService`
- DbContext load timing in `EfModelService`
- Comparison timing in `ComparisonService`
- EF Core SQL query logging in Development mode

**Log Levels by Status Code:**
- 2xx/3xx: `LogInformation`
- 4xx: `LogWarning`
- 5xx: `LogError`

Detailed specs in `docs/08-logging-improvements/`.

## Unit Testing

Comprehensive unit test infrastructure targeting ~60% code coverage.

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Infrastructure (Moq, FluentAssertions, EF InMemory, test helpers) |
| 2 | Complete | Security services (DllValidatorService, ConnectionStringProtector) |
| 3 | Complete | Core services (DataDictionaryService, ComparisonService, DatabaseSyncService, EfModelSourceService) |
| 4 | Complete | Controllers (DataDictionaryController, ComparisonController, EfModelSourcesController) |
| 5 | Complete | Middleware (ExceptionHandlingMiddleware) |

**Test Infrastructure:**
- `TestDbContextFactory` - Creates isolated in-memory DbContext instances
- `TestDataBuilder` - Builder pattern classes for test entities (DataElement, EfModelSource, SyncHistory, etc.)

**Test Dependencies:**
| Package | Version | Purpose |
|---------|---------|---------|
| Moq | 4.20.72 | Mocking framework |
| FluentAssertions | 8.8.0 | Readable assertion syntax |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.0 | In-memory database for testing |

**Running Tests:**
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~DataDictionaryServiceTests"
```

Detailed specs in `docs/09-unit-testing-phases/`.

For additional coverage expansion (targeting 60%), see `docs/10-unit-testing-phase6/`.

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
