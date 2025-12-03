# Phase 5: Polish and Finalization

## Objective
Add final touches including error handling, UI improvements, and ensure the complete workflow functions end-to-end.

## Tasks

### 5.1 Add Global Error Handling

**src/NetSqlDataDicV2.Web/Controllers/HomeController.cs:**
```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models;

namespace NetSqlDataDicV2.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var errorViewModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        return View(errorViewModel);
    }
}
```

**src/NetSqlDataDicV2.Web/Models/ErrorViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string? Message { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Views/Shared/Error.cshtml:**
```html
@model NetSqlDataDicV2.Web.Models.ErrorViewModel
@{
    ViewData["Title"] = "Error";
}

<div class="text-center">
    <h1 class="display-4 text-danger">Error</h1>
    <p class="lead">An error occurred while processing your request.</p>

    @if (Model?.ShowRequestId ?? false)
    {
        <p>
            <strong>Request ID:</strong> <code>@Model.RequestId</code>
        </p>
    }

    <hr />

    <p>
        <a asp-controller="Home" asp-action="Index" class="btn btn-primary">Return to Home</a>
    </p>
</div>
```

### 5.2 Add Loading Indicators CSS

**src/NetSqlDataDicV2.Web/wwwroot/css/site.css:**
```css
/* Loading overlay */
.loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(255, 255, 255, 0.8);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

.loading-spinner {
    width: 3rem;
    height: 3rem;
}

/* Card hover effects */
.card {
    transition: box-shadow 0.2s ease-in-out;
}

.card:hover {
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
}

/* Badge improvements */
.badge {
    font-weight: 500;
}

/* Grid improvements */
.k-grid {
    border: none;
}

.k-grid .k-grid-header {
    background-color: #f8f9fa;
}

.k-grid .k-filter-row th {
    padding: 4px;
}

/* Table improvements */
.table th {
    font-weight: 600;
    background-color: #f8f9fa;
}

/* Status indicators */
.status-match { color: #198754; }
.status-warning { color: #ffc107; }
.status-danger { color: #dc3545; }
.status-info { color: #0dcaf0; }

/* Comparison card */
.comparison-summary .col {
    border-right: 1px solid #dee2e6;
}

.comparison-summary .col:last-child {
    border-right: none;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .comparison-summary .col {
        border-right: none;
        border-bottom: 1px solid #dee2e6;
        padding-bottom: 1rem;
        margin-bottom: 1rem;
    }

    .comparison-summary .col:last-child {
        border-bottom: none;
    }
}

/* Navbar active state */
.navbar-nav .nav-link.active {
    font-weight: 600;
}

/* Code styling */
code {
    background-color: #f8f9fa;
    padding: 0.2rem 0.4rem;
    border-radius: 0.25rem;
    font-size: 0.875em;
}

/* Button improvements */
.btn-group-sm .btn {
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
}

/* Toast notifications (for future use) */
.toast-container {
    position: fixed;
    top: 1rem;
    right: 1rem;
    z-index: 1050;
}
```

### 5.3 Add Navigation Active State

Update **src/NetSqlDataDicV2.Web/Views/Shared/_Layout.cshtml** to highlight active nav:

```html
@{
    var controller = ViewContext.RouteData.Values["controller"]?.ToString();
}

<!-- In the navbar section, update the nav-link classes: -->
<li class="nav-item">
    <a class="nav-link @(controller == "DataDictionary" ? "active" : "")"
       asp-controller="DataDictionary" asp-action="Index">Dictionary</a>
</li>
<li class="nav-item">
    <a class="nav-link @(controller == "Sync" ? "active" : "")"
       asp-controller="Sync" asp-action="Index">Sync</a>
</li>
<li class="nav-item">
    <a class="nav-link @(controller == "Comparison" ? "active" : "")"
       asp-controller="Comparison" asp-action="Index">Compare</a>
</li>
```

### 5.4 Add Export to CSV (Optional Enhancement)

Add to **DataDictionaryController.cs:**
```csharp
[HttpGet]
public async Task<IActionResult> ExportCsv(string? server = null, string? database = null)
{
    var data = string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database)
        ? await _service.GetAllAsync()
        : await _service.GetByDatabaseAsync(server, database);

    var csv = new StringBuilder();
    csv.AppendLine("Server,Database,Schema,Table,Column,DataType,Nullable,PrimaryKey,ForeignKeyTo,Purpose,Notes");

    foreach (var item in data)
    {
        csv.AppendLine($"\"{item.DatabaseServer}\",\"{item.DatabaseName}\",\"{item.SchemaName}\",\"{item.TableName}\",\"{item.ColumnName}\",\"{item.DataType}\",{item.IsNullable},{item.IsPrimaryKey},\"{item.ForeignKeyTo}\",\"{EscapeCsv(item.DataPurpose)}\",\"{EscapeCsv(item.Notes)}\"");
    }

    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
    return File(bytes, "text/csv", $"data-dictionary-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
}

private static string EscapeCsv(string? value)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
}
```

Add export button to **Views/DataDictionary/Index.cshtml** toolbar:
```html
<a href="@Url.Action("ExportCsv", "DataDictionary")" class="btn btn-outline-primary btn-sm ms-2">
    Export CSV
</a>
```

### 5.5 Add Configuration Validation

Update **Program.cs** to validate configuration:
```csharp
// Validate required configuration
var dataDictionaryConnectionString = builder.Configuration.GetConnectionString("DataDictionary");
if (string.IsNullOrEmpty(dataDictionaryConnectionString))
{
    throw new InvalidOperationException("DataDictionary connection string is not configured");
}

var sourceConnectionString = builder.Configuration.GetConnectionString("SourceDatabase");
if (string.IsNullOrEmpty(sourceConnectionString))
{
    Console.WriteLine("WARNING: SourceDatabase connection string is not configured. Sync feature will not work.");
}
```

### 5.6 Update appsettings.Development.json

**src/NetSqlDataDicV2.Web/appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
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

### 5.7 Create README.md

**README.md** (in solution root):
```markdown
# SQL Server Data Dictionary

A .NET 8 MVC application for managing SQL Server database metadata and comparing it against EF Core models.

## Features

- **Data Dictionary**: View and edit metadata for all database tables and columns
- **Manual Sync**: Synchronize metadata from source database on demand
- **EF Core Comparison**: Compare data dictionary against scaffolded EF Core models
- **Kendo UI Grids**: Rich filtering, sorting, and inline editing

## Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Telerik Kendo UI license

## Setup

1. Clone the repository
2. Update connection strings in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DataDictionary": "Server=.;Database=DataDictionary;...",
       "SourceDatabase": "Server=.;Database=YourSourceDb;..."
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

## License

MIT
```

### 5.8 Final Program.cs

Complete **src/NetSqlDataDicV2.Web/Program.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var dataDictionaryConnectionString = builder.Configuration.GetConnectionString("DataDictionary")
    ?? throw new InvalidOperationException("DataDictionary connection string is required");

var sourceConnectionString = builder.Configuration.GetConnectionString("SourceDatabase");
if (string.IsNullOrEmpty(sourceConnectionString))
{
    Console.WriteLine("WARNING: SourceDatabase connection string not configured. Sync feature will not work.");
}

// Add MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Kendo UI services
builder.Services.AddKendo();

// Add DbContexts
builder.Services.AddDbContext<DataDictionaryDbContext>(options =>
    options.UseSqlServer(dataDictionaryConnectionString));

if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddDbContext<SourceDbContext>(options =>
        options.UseSqlServer(sourceConnectionString));
}

// Add application services
builder.Services.AddScoped<IDataDictionaryService, DataDictionaryService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();

if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddScoped<IEfModelService, EfModelService>();
    builder.Services.AddScoped<IComparisonService, ComparisonService>();
}

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

## Deliverables

- [x] Error handling page and controller
- [x] CSS improvements for loading states and UI polish
- [x] Navigation active state highlighting
- [x] CSV export functionality
- [x] Configuration validation on startup
- [x] README.md documentation
- [x] Complete Program.cs with all services

## Final Verification Checklist

### Setup
- [ ] Solution builds without errors
- [ ] Database migrations run successfully
- [ ] Application starts without errors

### Data Dictionary
- [ ] Grid loads and displays data
- [ ] Server-side paging works
- [ ] Filtering on all columns works
- [ ] Sorting works
- [ ] Inline editing saves changes
- [ ] CSV export downloads file

### Sync
- [ ] Sync executes successfully
- [ ] Progress indicator shows during sync
- [ ] Results display correctly
- [ ] History table updates
- [ ] New columns appear in dictionary
- [ ] Removed columns are soft-deleted

### Comparison
- [ ] Comparison runs successfully
- [ ] Summary statistics are accurate
- [ ] Grid displays all items
- [ ] Filter buttons work
- [ ] Status badges display correctly
- [ ] Missing in EF items show warning
- [ ] Missing in DB items show danger

### End-to-End Workflow
1. [ ] Run initial sync - dictionary populates
2. [ ] View dictionary - all columns visible
3. [ ] Edit a column's Purpose - saves correctly
4. [ ] Run comparison - all items match (assuming models match)
5. [ ] Add column to source DB
6. [ ] Run sync - new column appears
7. [ ] Run comparison - shows "Missing in EF Model"
8. [ ] Re-scaffold models, rebuild
9. [ ] Run comparison - all match again
