# SQL Server Data Dictionary - System Architecture

## 1. Overview

A .NET 8 MVC application that creates and maintains a SQL Server data dictionary, enabling users to:
- Sync database schema metadata into a persistent data dictionary
- View and edit data dictionary entries via Kendo UI grids
- Compare the data dictionary against EF Core DB-First scaffolded models
- Identify schema drift between database and application models

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              User Interface                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │ Data Dictionary │  │   Sync Page     │  │    Comparison Page          │  │
│  │   Kendo Grid    │  │                 │  │                             │  │
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
│  DataDictionary     │  │  Source SQL     │  │  SourceModels   │  │
│  DbContext          │  │  Server DB      │  │  DbContext      │◄─┘
│  (EF Core)          │  │  (sys.tables)   │  │  (Referenced)   │
└─────────┬───────────┘  └─────────────────┘  └─────────────────┘
          │
          ▼
┌─────────────────────┐
│  Data Dictionary    │
│  SQL Server DB      │
│  - DataElements     │
│  - SyncHistory      │
│  - SourceConnections│
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
│   └── phase5-polish.md
├── src/
│   ├── NetSqlDataDicV2.Web/         # Main MVC Application
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs
│   │   │   ├── DataDictionaryController.cs
│   │   │   ├── SyncController.cs
│   │   │   └── ComparisonController.cs
│   │   ├── Models/
│   │   │   ├── Entities/
│   │   │   │   ├── DataElement.cs
│   │   │   │   ├── SyncHistory.cs
│   │   │   │   └── SourceConnection.cs
│   │   │   ├── ViewModels/
│   │   │   │   ├── DataElementViewModel.cs
│   │   │   │   ├── SyncResultViewModel.cs
│   │   │   │   └── ComparisonResultViewModel.cs
│   │   │   └── Dto/
│   │   │       ├── SourceColumnDto.cs
│   │   │       └── EfModelColumnDto.cs
│   │   ├── Data/
│   │   │   ├── DataDictionaryDbContext.cs
│   │   │   └── Configurations/
│   │   │       ├── DataElementConfiguration.cs
│   │   │       ├── SyncHistoryConfiguration.cs
│   │   │       └── SourceConnectionConfiguration.cs
│   │   ├── Services/
│   │   │   ├── IDataDictionaryService.cs
│   │   │   ├── DataDictionaryService.cs
│   │   │   ├── IDatabaseSyncService.cs
│   │   │   ├── DatabaseSyncService.cs
│   │   │   ├── IEfModelService.cs
│   │   │   ├── EfModelService.cs
│   │   │   ├── IComparisonService.cs
│   │   │   └── ComparisonService.cs
│   │   ├── Views/
│   │   │   ├── Shared/
│   │   │   │   ├── _Layout.cshtml
│   │   │   │   └── _ValidationScriptsPartial.cshtml
│   │   │   ├── Home/
│   │   │   │   └── Index.cshtml
│   │   │   ├── DataDictionary/
│   │   │   │   └── Index.cshtml
│   │   │   ├── Sync/
│   │   │   │   └── Index.cshtml
│   │   │   └── Comparison/
│   │   │       └── Index.cshtml
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   │   └── site.css
│   │   │   └── js/
│   │   │       └── site.js
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Program.cs
│   │
│   └── NetSqlDataDicV2.SourceModels/    # EF Core Scaffolded Models
│       ├── NetSqlDataDicV2.SourceModels.csproj
│       ├── SourceDbContext.cs
│       └── [Scaffolded entity classes]
│
└── tests/
    └── NetSqlDataDicV2.Tests/
        └── [Unit tests]
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

### 4.2 SyncHistory Table
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

### 4.3 SourceConnections Table
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
- Server-side paging, filtering, sorting for Kendo Grid
- Inline update support for editable fields

#### DatabaseSyncService
- Connects to source SQL Server database
- Queries sys.tables, sys.columns, sys.types, sys.foreign_key_columns
- Performs MERGE-style upsert (insert new, update existing, soft-delete removed)
- Tracks sync history

#### EfModelService
- Reads EF Core model metadata from referenced SourceDbContext
- Uses IModel API to extract entity types, properties, column mappings
- Returns list of EfModelColumnDto for comparison

#### ComparisonService
- Compares DataElement list with EfModelColumn list
- Produces comparison results:
  - **Match**: Column exists in both with compatible types
  - **MissingInEfModel**: Column in DB but not in EF model
  - **MissingInDatabase**: Property in EF model but not in DB
  - **TypeMismatch**: Types are incompatible

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
User clicks "Compare"
       │
       ▼
ComparisonController.Compare()
       │
       ├──► DataDictionaryService.GetByDatabase() → List<DataElement>
       │
       ├──► EfModelService.GetEfModelColumns() → List<EfModelColumnDto>
       │
       ▼
ComparisonService.Compare()
       │
       ├──► Build lookup dictionaries (Schema.Table.Column)
       │
       ├──► For each DataElement:
       │    - If in EF: Check type compatibility → Match or TypeMismatch
       │    - If not in EF: → MissingInEfModel
       │
       ├──► For each EfModelColumn not in DataElements:
       │    - → MissingInDatabase
       │
       └──► Return ComparisonResult with all items
```

## 7. Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 9 |
| Web Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core 9 |
| Database | SQL Server (Docker) |
| UI Grid | Telerik Kendo UI for ASP.NET Core 2024.1.130 |
| CSS Framework | Bootstrap 5.3.2 |

## 8. Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DataDictionary": "Server=.;Database=DataDictionary;Trusted_Connection=True;TrustServerCertificate=True;",
    "SourceDatabase": "Server=.;Database=YourSourceDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "SourceDatabase": {
    "Server": "localhost",
    "Database": "YourSourceDb"
  }
}
```

## 9. Security Considerations

1. **Connection Strings**: Store in appsettings.json (development) or environment variables/Azure Key Vault (production)
2. **No Authentication**: As requested, but can be added later using ASP.NET Core Identity
3. **SQL Injection**: All database queries use parameterized queries via EF Core
4. **Input Validation**: Server-side validation on all user inputs

## 10. Future Enhancements

- Multi-database support (track multiple source databases)
- Export to Excel/CSV
- Schema comparison history
- Automated sync scheduling
- User authentication and role-based access
- Audit logging for changes

## 11. Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Project Setup | Completed |
| Phase 2 | Data Dictionary Core | Not Started |
| Phase 3 | Sync Feature | Not Started |
| Phase 4 | EF Core Comparison | Not Started |
| Phase 5 | Polish & Finalization | Not Started |

**Last Updated:** December 2024
