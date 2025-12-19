# SQL Server Data Dictionary - System Architecture

## 1. Overview

A .NET 9 MVC application that creates and maintains a SQL Server data dictionary, enabling users to:
- Sync database schema metadata into a persistent data dictionary
- View and edit data dictionary entries via DataTables grids
- Compare the data dictionary against EF Core DB-First scaffolded models
- Identify schema drift between database and application models

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              User Interface                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │ Data Dictionary │  │   Sync Page     │  │    Comparison Page          │  │
│  │  DataTables     │  │                 │  │                             │  │
│  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │
└───────────┼────────────────────┼────────────────────────┼───────────────────┘
            │                    │                        │
            ▼                    ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MVC Controllers                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │ DataDictionary  │  │     Sync        │  │       Comparison            │  │
│  │   Controller    │  │   Controller    │  │       Controller            │  │
│  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │
└───────────┼────────────────────┼────────────────────────┼───────────────────┘
            │                    │                        │
            ▼                    ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Service Layer                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────┐ ┌───────────┐  │
│  │ DataDictionary  │  │  DatabaseSync   │  │  EfModel      │ │Comparison │  │
│  │    Service      │  │    Service      │  │  Service      │ │  Service  │  │
│  └────────┬────────┘  └────────┬────────┘  └───────┬───────┘ └─────┬─────┘  │
└───────────┼────────────────────┼──────────────────┼───────────────┼─────────┘
            │                    │                  │               │
            ▼                    ▼                  ▼               │
┌─────────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  DataDictionary     │  │  Source SQL     │  │  Dynamic DLL    │  │
│  DbContext          │  │  Server DB      │  │  DbContext      │◄─┘
│  (EF Core)          │  │  (sys.tables)   │  │  (Runtime Load) │
└─────────┬───────────┘  └─────────────────┘  └─────────────────┘
          │
          ▼
┌─────────────────────┐
│  Data Dictionary    │
│  SQL Server DB      │
│  - DataElements     │
│  - SyncHistory      │
│  - SourceConnections│
│  - DataElementAudits│
│  - DataElementNotes │
└─────────────────────┘
```

## 3. Solution Structure

```
NetSqlDataDicV2/
├── NetSqlDataDicV2.sln
├── docs/
│   ├── architecture.md              # This document
│   ├── phase1-project-setup.md
│   ├── phase2-data-dictionary.md
│   ├── phase3-sync-feature.md
│   ├── phase4-ef-comparison.md
│   ├── phase5-polish.md
│   ├── dll-loading-phases/          # DLL loading feature specs
│   ├── simplification-phases/       # Simplification project specs
│   ├── audit-trail-phases/          # Audit trail feature specs
│   ├── notes-feature-phases/        # Notes feature specs
│   └── skipped-tables-phases/       # Skipped tables feature specs
├── src/
│   └── NetSqlDataDicV2.Web/         # Main MVC Application
│       ├── Controllers/
│       │   ├── HomeController.cs
│       │   ├── DataDictionaryController.cs
│       │   ├── SyncController.cs
│       │   ├── ComparisonController.cs
│       │   └── EfModelSourcesController.cs
│       ├── Models/
│       │   ├── Entities/
│       │   │   ├── DataElement.cs
│       │   │   ├── DataElementAudit.cs
│       │   │   ├── DataElementNote.cs
│       │   │   ├── SyncHistory.cs
│       │   │   ├── SourceConnection.cs
│       │   │   └── EfModelSource.cs
│       │   ├── ViewModels/
│       │   └── Dto/
│       ├── Data/
│       │   ├── DataDictionaryDbContext.cs
│       │   └── Configurations/
│       ├── Services/
│       │   ├── IDataDictionaryService.cs
│       │   ├── DataDictionaryService.cs
│       │   ├── IDatabaseSyncService.cs
│       │   ├── DatabaseSyncService.cs
│       │   ├── IEfModelService.cs
│       │   ├── EfModelService.cs
│       │   ├── IComparisonService.cs
│       │   ├── ComparisonService.cs
│       │   ├── IEfModelSourceService.cs
│       │   ├── EfModelSourceService.cs
│       │   ├── DbContextProviders/     # Dynamic DLL loading
│       │   │   ├── IDbContextProvider.cs
│       │   │   ├── IDbContextProviderFactory.cs
│       │   │   ├── DbContextProviderFactory.cs
│       │   │   ├── DynamicDllProvider.cs
│       │   │   └── PluginLoadContext.cs
│       │   └── Security/               # DLL security services
│       │       ├── IDllValidatorService.cs
│       │       ├── DllValidatorService.cs
│       │       ├── IConnectionStringProtector.cs
│       │       ├── ConnectionStringProtector.cs
│       │       ├── ISecurityAuditService.cs
│       │       └── SecurityAuditService.cs
│       ├── Views/
│       ├── wwwroot/
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
└── tests/
    └── NetSqlDataDicV2.Tests/
```

## 4. Database Schema

### 4.1 DataElements Table
Stores metadata for each column in the source database.

| Column | Type | Description |
|--------|------|-------------|
| DataElementId | INT (PK) | Auto-increment primary key |
| DataElementName | NVARCHAR(256) | Formatted column name (PascalCase) |
| DataElementType | NVARCHAR(50) | Column, Dimension, Fact |
| DataType | NVARCHAR(128) | SQL data type (e.g., VARCHAR(50)) |
| DataPurpose | NVARCHAR(MAX) | User-editable business description |
| EntityPurpose | NVARCHAR(MAX) | User-editable table description |
| DatabaseServer | NVARCHAR(256) | Source server name |
| DatabaseName | NVARCHAR(256) | Source database name |
| SchemaName | NVARCHAR(128) | Schema (default: dbo) |
| TableName | NVARCHAR(256) | Table name |
| ColumnName | NVARCHAR(256) | Column name |
| OriginalDataSource | NVARCHAR(512) | Data origin information |
| Notes | NVARCHAR(MAX) | Additional notes |
| ForeignKeyTo | NVARCHAR(512) | FK reference (Schema.Table.Column) |
| RowCount | BIGINT | Table row count at sync time |
| IsNullable | BIT | Column nullability |
| IsPrimaryKey | BIT | Is part of primary key |
| MaxLength | INT | Max length for string types |
| Precision | INT | Decimal precision |
| Scale | INT | Decimal scale |
| CreateTime | DATETIME2 | Record creation time |
| LastUpdateTime | DATETIME2 | Last modification time |
| LastSyncTime | DATETIME2 | Last sync from source |
| IsDeleted | BIT | Soft delete flag |

### 4.2 DataElementAudits Table
Stores property-level change history for data elements during sync operations.

| Column | Type | Description |
|--------|------|-------------|
| DataElementAuditId | INT (PK) | Auto-increment primary key |
| DataElementId | INT (FK) | Reference to DataElement |
| SyncHistoryId | INT (FK) | Reference to SyncHistory |
| ChangeType | NVARCHAR(20) | Added, Modified, Deleted, Restored |
| PropertyName | NVARCHAR(50) | Name of changed property (null for Added/Deleted) |
| OldValue | NVARCHAR(500) | Previous value |
| NewValue | NVARCHAR(500) | New value |
| ChangeTime | DATETIME2 | When the change occurred |

**Indexes:**
- IX_DataElementAudits_DataElementId
- IX_DataElementAudits_SyncHistoryId
- IX_DataElementAudits_ChangeTime

### 4.3 DataElementNotes Table
Stores user-added notes for data elements (append-only).

| Column | Type | Description |
|--------|------|-------------|
| DataElementNoteId | INT (PK) | Auto-increment primary key |
| DataElementId | INT (FK) | Reference to DataElement |
| NoteText | NVARCHAR(2000) | Note content |
| CreatedAt | DATETIME2 | When the note was created |

**Indexes:**
- IX_DataElementNotes_DataElementId
- IX_DataElementNotes_CreatedAt

### 4.5 SyncHistory Table
Tracks synchronization operations.

| Column | Type | Description |
|--------|------|-------------|
| SyncHistoryId | INT (PK) | Auto-increment primary key |
| DatabaseServer | NVARCHAR(256) | Target server |
| DatabaseName | NVARCHAR(256) | Target database |
| SyncStartTime | DATETIME2 | When sync started |
| SyncEndTime | DATETIME2 | When sync completed |
| TablesProcessed | INT | Number of tables processed |
| ColumnsProcessed | INT | Number of columns processed |
| ColumnsAdded | INT | New columns found |
| ColumnsUpdated | INT | Columns with changes |
| ColumnsRemoved | INT | Columns soft-deleted |
| Status | NVARCHAR(50) | Running, Completed, Failed |
| ErrorMessage | NVARCHAR(MAX) | Error details if failed |

### 4.6 SourceConnections Table
Stores source database connection information.

| Column | Type | Description |
|--------|------|-------------|
| ConnectionId | INT (PK) | Auto-increment primary key |
| ConnectionName | NVARCHAR(256) | Friendly name |
| DatabaseServer | NVARCHAR(256) | Server name |
| DatabaseName | NVARCHAR(256) | Database name |
| IsActive | BIT | Whether connection is active |
| LastSyncTime | DATETIME2 | Last successful sync |

## 5. Component Details

### 5.1 Services

#### DataDictionaryService
- CRUD operations for DataElement entities
- Server-side paging, filtering, sorting for DataTables
- Inline update support for editable fields
- Audit history retrieval for Details page
- Deleted records query (bypasses global query filter)
- Notes management (add, retrieve notes for elements)

#### DatabaseSyncService
- Connects to source SQL Server database
- Queries sys.tables, sys.columns, sys.types, sys.foreign_key_columns
- Performs MERGE-style upsert (insert new, update existing, soft-delete removed)
- Tracks sync history
- Creates audit records for all changes:
  - `Added` - New column discovered
  - `Modified` - Property-level changes (DataType, MaxLength, IsNullable, etc.)
  - `Deleted` - Column no longer in source (soft delete)
  - `Restored` - Previously deleted column reappears

#### EfModelService
- Reads EF Core model metadata from dynamically loaded DbContexts
- Uses IModel API to extract entity types, properties, column mappings
- Returns list of EfModelColumnDto for comparison
- Works with EfModelSource configurations for DLL-based loading

#### EfModelSourceService
- CRUD operations for EfModelSource configurations
- Manages DLL paths, DbContext types, and connection strings

#### ComparisonService
- Compares DataElement list with EfModelColumn list
- Detects tables entirely missing from DbContext (skipped tables)
- Produces comparison results:
  - **Match**: Column exists in both with compatible types
  - **MissingInEfModel**: Column in DB but not in EF model (table IS in DbContext)
  - **MissingInDatabase**: Property in EF model but not in DB
  - **TypeMismatch**: Types are incompatible
  - **SkippedTables**: Tables in DB with no corresponding entity in DbContext

### 5.2 Type Mapping (SQL to CLR)

| SQL Type | CLR Types |
|----------|-----------|
| INT | Int32, int |
| BIGINT | Int64, long |
| SMALLINT | Int16, short |
| TINYINT | Byte, byte |
| BIT | Boolean, bool |
| DECIMAL, NUMERIC, MONEY | Decimal, decimal |
| FLOAT | Double, double |
| REAL | Single, float |
| DATETIME, DATETIME2 | DateTime |
| DATE | DateTime, DateOnly |
| TIME | TimeSpan, TimeOnly |
| DATETIMEOFFSET | DateTimeOffset |
| VARCHAR, NVARCHAR, CHAR, NCHAR, TEXT | String, string |
| UNIQUEIDENTIFIER | Guid |
| VARBINARY, BINARY, IMAGE | Byte[], byte[] |

## 6. Data Flow

### 6.1 Sync Flow
```
User clicks "Sync"
       │
       ▼
SyncController.Execute()
       │
       ▼
DatabaseSyncService.SyncAsync()
       │
       ├──► Connect to Source DB (using connection string from config)
       │
       ├──► Query sys.tables, sys.columns, sys.types, sys.foreign_key_columns
       │
       ├──► Transform results to DataElement entities
       │
       ├──► Upsert into DataDictionary DB:
       │    - INSERT new columns
       │    - UPDATE changed columns
       │    - SET IsDeleted=1 for removed columns
       │
       └──► Return SyncResult with statistics
```

### 6.2 Comparison Flow
```
User selects EfModelSource and clicks "Compare"
       │
       ▼
ComparisonController.CompareSource(sourceId)
       │
       ├──► EfModelSourceService.GetById() → EfModelSource config
       │
       ├──► DataDictionaryService.GetByDatabase() → List<DataElement>
       │
       ├──► EfModelService.GetEfModelColumns(source) → List<EfModelColumnDto>
       │    │
       │    └──► DynamicDllProvider loads DbContext from DLL at runtime
       │
       ▼
ComparisonService.CompareAsync(sourceId)
       │
       ├──► Build lookup dictionaries (Schema.Table.Column)
       │
       ├──► Build efTableKeys set (tables that exist in DbContext)
       │
       ├──► For each DataElement:
       │    - If TABLE not in EF: → Add to SkippedTables (skip column comparison)
       │    - If TABLE in EF but column not in EF: → MissingInEfModel
       │    - If in EF: Check type compatibility → Match or TypeMismatch
       │
       ├──► For each EfModelColumn not in DataElements:
       │    - → MissingInDatabase
       │
       └──► Return ComparisonResult with Items + SkippedTables
```

## 7. Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 9 |
| Web Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core 9 |
| Database | SQL Server (Docker) |
| UI Grid | DataTables 1.13.7 (CDN) |
| CSS Framework | Bootstrap 5.3.2 (CDN) |
| JavaScript | jQuery 3.7.1 (CDN) |

## 8. Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DataDictionary": "Server=.;Database=DataDictionary;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "DllSecurity": {
    "AllowedDirectories": [],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600,
    "BlockedAssemblyNames": []
  }
}
```

EF model comparison sources are configured via the EfModelSources management UI, not appsettings.

## 9. Security Considerations

1. **Connection Strings**: Store in appsettings.json (development) or environment variables/Azure Key Vault (production)
2. **Connection String Encryption**: EfModelSource connection strings are encrypted at rest using Data Protection API
3. **DLL Validation**: DLLs are validated before loading (path restrictions, file size limits, blocked assemblies)
4. **Security Audit Logging**: DLL loads and source configuration changes are logged
5. **No Authentication**: As requested, but can be added later using ASP.NET Core Identity
6. **SQL Injection**: All database queries use parameterized queries via EF Core
7. **Input Validation**: Server-side validation on all user inputs

## 10. Future Enhancements

- Multi-database support (track multiple source databases)
- Schema comparison history
- Automated sync scheduling
- User authentication and role-based access
- Audit retention policies (auto-cleanup old records)

## 11. Implementation Status

### Core Phases
| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Project Setup | Complete |
| Phase 2 | Data Dictionary Core | Complete |
| Phase 3 | Sync Feature | Complete |
| Phase 4 | EF Core Comparison | Complete |
| Phase 5 | Polish & Finalization | Complete |

### DLL Loading Feature
| Phase | Description | Status |
|-------|-------------|--------|
| DLL Phase 1 | Core Infrastructure | Complete |
| DLL Phase 2 | Service Layer | Complete |
| DLL Phase 3 | Data Layer | Complete |
| DLL Phase 4 | UI Layer | Complete |
| DLL Phase 5 | Security | Complete |
| DLL Phase 6 | Error Handling | Complete |

### Simplification Project
| Phase | Description | Status |
|-------|-------------|--------|
| Simplify Phase 1 | Remove Direct Reference | Complete |
| Simplify Phase 2 | Replace Kendo UI with DataTables | Complete |
| Simplify Phase 3 | Cleanup Navigation | Complete |
| Simplify Phase 4 | Configuration Cleanup | Complete |

### Audit Trail Feature
| Phase | Description | Status |
|-------|-------------|--------|
| Audit Phase 1 | Data Layer (Entity, Configuration, Migration) | Complete |
| Audit Phase 2 | Service Layer (Change Detection, Audit Records) | Complete |
| Audit Phase 3 | ViewModel Layer (Audit History Queries) | Complete |
| Audit Phase 4 | UI Layer (Details Page, Deleted View) | Complete |

### Notes Feature
| Phase | Description | Status |
|-------|-------------|--------|
| Notes Phase 1 | Data Layer (Entity, Configuration, Migration) | Complete |
| Notes Phase 2 | Service Layer (ViewModels, Service Methods) | Complete |
| Notes Phase 3 | UI Layer (Details Page Notes Card, Add Modal) | Complete |
| Notes Phase 4 | Cleanup (Grid Note Count, Documentation) | Complete |

### Skipped Tables Feature
| Phase | Description | Status |
|-------|-------------|--------|
| Skipped Phase 1 | Data Layer (SkippedTableViewModel, Result Model) | Complete |
| Skipped Phase 2 | Service Layer (Table-Level Detection Logic) | Complete |
| Skipped Phase 3 | Controller Layer (JSON Response Update) | Complete |
| Skipped Phase 4 | UI Layer (Skipped Tables Section, DataTable) | Complete |

**Last Updated:** December 2024
