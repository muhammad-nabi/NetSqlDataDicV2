# SQL Server Data Dictionary

A .NET 9 MVC application for managing SQL Server database metadata and comparing it against EF Core models.

## Features

- **Data Dictionary**: View and edit metadata for all database tables and columns
- **Manual Sync**: Synchronize metadata from source database on demand
- **EF Core Comparison**: Compare data dictionary against scaffolded EF Core models
- **CSV Export**: Export data dictionary to CSV format
- **Kendo UI Grids**: Rich filtering, sorting, and inline editing

## Prerequisites

- .NET 9 SDK
- SQL Server (local or remote)
- Telerik Kendo UI license

## Setup

1. Clone the repository

2. Update connection strings in `appsettings.json`:
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

3. Create the DataDictionary database:
   ```bash
   cd src/NetSqlDataDicV2.Web
   dotnet ef database update
   ```

4. Scaffold your source database models:
   ```bash
   cd src/NetSqlDataDicV2.SourceModels
   dotnet ef dbcontext scaffold "YOUR_CONNECTION_STRING" Microsoft.EntityFrameworkCore.SqlServer --context SourceDbContext
   ```

5. Run the application:
   ```bash
   dotnet run --project src/NetSqlDataDicV2.Web
   ```

## Usage

### Sync Data Dictionary

1. Navigate to **Sync** page
2. Click **Sync Now**
3. View sync results and history

### View/Edit Data Dictionary

1. Navigate to **Dictionary** page
2. Use filters to find specific tables/columns
3. Click **Edit** to modify Purpose or Notes fields
4. Click **Export CSV** to download the data

### Compare with EF Core Models

1. Ensure models are scaffolded in SourceModels project
2. Navigate to **Compare** page
3. Click **Run Comparison**
4. Review differences:
   - **Missing in EF Model**: Re-scaffold needed
   - **Missing in Database**: Add column to DB, then sync
   - **Type Mismatch**: Review and fix as needed

## Project Structure

```
NetSqlDataDicV2/
├── src/
│   ├── NetSqlDataDicV2.Web/          # Main MVC application
│   └── NetSqlDataDicV2.SourceModels/ # EF Core scaffolded models
├── tests/
│   └── NetSqlDataDicV2.Tests/        # Unit tests
└── docs/                              # Documentation
```

## Build & Run Commands

```bash
# Build solution
dotnet build

# Run web application
cd src/NetSqlDataDicV2.Web && dotnet run

# Run tests
dotnet test
```

## License

MIT
