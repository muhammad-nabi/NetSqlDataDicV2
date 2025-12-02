# Phase 1: Project Setup

## Objective
Create the solution structure, configure Entity Framework Core, set up Kendo UI, and establish the database schema through migrations.

## Tasks

### 1.1 Create Solution and Projects

```bash
# Navigate to project folder
cd /Users/bs01384/Documents/sources/DataDictionary/NetSqlDataDicV2

# Create solution
dotnet new sln -n NetSqlDataDicV2

# Create MVC web project
dotnet new mvc -n NetSqlDataDicV2.Web -o src/NetSqlDataDicV2.Web

# Create class library for scaffolded models
dotnet new classlib -n NetSqlDataDicV2.SourceModels -o src/NetSqlDataDicV2.SourceModels

# Create test project
dotnet new xunit -n NetSqlDataDicV2.Tests -o tests/NetSqlDataDicV2.Tests

# Add projects to solution
dotnet sln add src/NetSqlDataDicV2.Web/NetSqlDataDicV2.Web.csproj
dotnet sln add src/NetSqlDataDicV2.SourceModels/NetSqlDataDicV2.SourceModels.csproj
dotnet sln add tests/NetSqlDataDicV2.Tests/NetSqlDataDicV2.Tests.csproj

# Add project references
dotnet add src/NetSqlDataDicV2.Web reference src/NetSqlDataDicV2.SourceModels
dotnet add tests/NetSqlDataDicV2.Tests reference src/NetSqlDataDicV2.Web
```

### 1.2 Add NuGet Packages

**NetSqlDataDicV2.Web.csproj:**
```xml
<ItemGroup>
    <!-- EF Core -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>

    <!-- Kendo UI -->
    <PackageReference Include="Telerik.UI.for.AspNet.Core" Version="2024.1.130" />

    <!-- SQL Client -->
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.2" />
</ItemGroup>
```

**NetSqlDataDicV2.SourceModels.csproj:**
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
</ItemGroup>
```

### 1.3 Create Entity Models

**src/NetSqlDataDicV2.Web/Models/Entities/DataElement.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.Entities;

public class DataElement
{
    public int DataElementId { get; set; }
    public string DataElementName { get; set; } = string.Empty;
    public string? DataElementType { get; set; }
    public string? DataType { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }

    // Location
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }

    // Metadata
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
    public string? ForeignKeyTo { get; set; }
    public long? RowCount { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    // Audit
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncTime { get; set; }
    public bool IsDeleted { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Models/Entities/SyncHistory.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.Entities;

public class SyncHistory
{
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime SyncStartTime { get; set; }
    public DateTime? SyncEndTime { get; set; }
    public int? TablesProcessed { get; set; }
    public int? ColumnsProcessed { get; set; }
    public int? ColumnsAdded { get; set; }
    public int? ColumnsUpdated { get; set; }
    public int? ColumnsRemoved { get; set; }
    public string Status { get; set; } = "Running";
    public string? ErrorMessage { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Models/Entities/SourceConnection.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.Entities;

public class SourceConnection
{
    public int ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncTime { get; set; }
}
```

### 1.4 Create DbContext and Configurations

**src/NetSqlDataDicV2.Web/Data/DataDictionaryDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data;

public class DataDictionaryDbContext : DbContext
{
    public DataDictionaryDbContext(DbContextOptions<DataDictionaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataElement> DataElements => Set<DataElement>();
    public DbSet<SyncHistory> SyncHistory => Set<SyncHistory>();
    public DbSet<SourceConnection> SourceConnections => Set<SourceConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataDictionaryDbContext).Assembly);
    }
}
```

**src/NetSqlDataDicV2.Web/Data/Configurations/DataElementConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class DataElementConfiguration : IEntityTypeConfiguration<DataElement>
{
    public void Configure(EntityTypeBuilder<DataElement> builder)
    {
        builder.ToTable("DataElements");

        builder.HasKey(e => e.DataElementId);

        builder.Property(e => e.DataElementName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DataElementType)
            .HasMaxLength(50);

        builder.Property(e => e.DataType)
            .HasMaxLength(128);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.SchemaName)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue("dbo");

        builder.Property(e => e.TableName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.ColumnName)
            .HasMaxLength(256);

        builder.Property(e => e.OriginalDataSource)
            .HasMaxLength(512);

        builder.Property(e => e.ForeignKeyTo)
            .HasMaxLength(512);

        // Unique constraint
        builder.HasIndex(e => new {
            e.DatabaseServer,
            e.DatabaseName,
            e.SchemaName,
            e.TableName,
            e.ColumnName
        })
        .IsUnique()
        .HasDatabaseName("UQ_DataElements_Location");

        // Performance index
        builder.HasIndex(e => new {
            e.DatabaseServer,
            e.DatabaseName,
            e.SchemaName,
            e.TableName
        })
        .HasDatabaseName("IX_DataElements_Table")
        .HasFilter("[IsDeleted] = 0");

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
```

**src/NetSqlDataDicV2.Web/Data/Configurations/SyncHistoryConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class SyncHistoryConfiguration : IEntityTypeConfiguration<SyncHistory>
{
    public void Configure(EntityTypeBuilder<SyncHistory> builder)
    {
        builder.ToTable("SyncHistory");

        builder.HasKey(e => e.SyncHistoryId);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);
    }
}
```

**src/NetSqlDataDicV2.Web/Data/Configurations/SourceConnectionConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class SourceConnectionConfiguration : IEntityTypeConfiguration<SourceConnection>
{
    public void Configure(EntityTypeBuilder<SourceConnection> builder)
    {
        builder.ToTable("SourceConnections");

        builder.HasKey(e => e.ConnectionId);

        builder.Property(e => e.ConnectionName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);
    }
}
```

### 1.5 Configure Program.cs

**src/NetSqlDataDicV2.Web/Program.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Kendo UI services
builder.Services.AddKendo();

// Add DbContext
builder.Services.AddDbContext<DataDictionaryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DataDictionary")));

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### 1.6 Configure appsettings.json

**src/NetSqlDataDicV2.Web/appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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

### 1.7 Setup Kendo UI in Layout

**src/NetSqlDataDicV2.Web/Views/Shared/_Layout.cshtml:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Data Dictionary</title>

    <!-- Bootstrap CSS -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />

    <!-- Kendo UI CSS -->
    <link href="https://kendo.cdn.telerik.com/2024.1.130/styles/kendo.bootstrap-main.min.css" rel="stylesheet" />

    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
            <div class="container">
                <a class="navbar-brand" asp-controller="Home" asp-action="Index">
                    Data Dictionary
                </a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav">
                        <li class="nav-item">
                            <a class="nav-link" asp-controller="DataDictionary" asp-action="Index">Dictionary</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" asp-controller="Sync" asp-action="Index">Sync</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" asp-controller="Comparison" asp-action="Index">Compare</a>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    </header>

    <main class="container mt-4">
        @RenderBody()
    </main>

    <footer class="footer mt-auto py-3 bg-light">
        <div class="container text-center">
            <span class="text-muted">SQL Server Data Dictionary &copy; @DateTime.Now.Year</span>
        </div>
    </footer>

    <!-- jQuery (required for Kendo) -->
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>

    <!-- Bootstrap JS -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>

    <!-- Kendo UI JS -->
    <script src="https://kendo.cdn.telerik.com/2024.1.130/js/kendo.all.min.js"></script>
    <script src="https://kendo.cdn.telerik.com/2024.1.130/js/kendo.aspnetmvc.min.js"></script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 1.8 Create Initial Migration

```bash
cd src/NetSqlDataDicV2.Web

# Create migration
dotnet ef migrations add InitialCreate

# Apply migration (creates database)
dotnet ef database update
```

### 1.9 Create Home Page

**src/NetSqlDataDicV2.Web/Views/Home/Index.cshtml:**
```html
@{
    ViewData["Title"] = "Home";
}

<div class="text-center">
    <h1 class="display-4">SQL Server Data Dictionary</h1>
    <p class="lead">Manage and compare your database schema metadata</p>
</div>

<div class="row mt-5">
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">Data Dictionary</h5>
                <p class="card-text">View and edit metadata for all database columns including purpose, notes, and business context.</p>
                <a asp-controller="DataDictionary" asp-action="Index" class="btn btn-primary">View Dictionary</a>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">Sync Database</h5>
                <p class="card-text">Synchronize the data dictionary with your source database to capture schema changes.</p>
                <a asp-controller="Sync" asp-action="Index" class="btn btn-primary">Sync Now</a>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">Compare Models</h5>
                <p class="card-text">Compare the data dictionary against EF Core scaffolded models to identify differences.</p>
                <a asp-controller="Comparison" asp-action="Index" class="btn btn-primary">Compare</a>
            </div>
        </div>
    </div>
</div>
```

## Deliverables

- [x] Solution file with all projects
- [x] Entity models (DataElement, SyncHistory, SourceConnection)
- [x] DbContext with configurations
- [x] EF Core migrations created
- [x] Kendo UI configured in layout
- [x] Basic navigation working
- [x] Home page with links to main features

## Verification

1. Run `dotnet build` - should compile without errors
2. Run `dotnet ef database update` - should create database
3. Run `dotnet run` - should start web application
4. Navigate to https://localhost:5001 - should see home page with navigation

## Implementation Notes

**Completed:** December 2024

**Actual Implementation Details:**
- Framework: .NET 9 (upgraded from planned .NET 8)
- EF Core: 9.0.0
- Microsoft.Data.SqlClient: 5.2.2
- Migration created: `20251201093844_InitialCreate`

**Configuration:**
- `appsettings.Development.json` is gitignored to protect credentials
- Connection strings use Docker SQL Server format: `Server=localhost,1433;...`
