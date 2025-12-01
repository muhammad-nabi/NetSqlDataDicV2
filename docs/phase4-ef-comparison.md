# Phase 4: EF Core Comparison

## Objective
Implement the comparison feature that compares the data dictionary against EF Core DB-First scaffolded models to identify schema drift.

## Tasks

### 4.1 Scaffold Source Database Models

First, scaffold the EF Core models from your source database into the SourceModels project:

```bash
# Navigate to the SourceModels project
cd src/NetSqlDataDicV2.SourceModels

# Scaffold models (adjust connection string as needed)
dotnet ef dbcontext scaffold "Server=.;Database=YourSourceDb;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --context SourceDbContext --output-dir .
```

This creates:
- `SourceDbContext.cs` - The DbContext for the source database
- Entity classes for each table

### 4.2 Create DTOs and ViewModels

**src/NetSqlDataDicV2.Web/Models/Dto/EfModelColumnDto.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.Dto;

public class EfModelColumnDto
{
    public string EntityName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public string? SchemaName { get; set; }
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Models/ViewModels/ComparisonResultViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class ComparisonResultViewModel
{
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime ComparisonTime { get; set; } = DateTime.UtcNow;
    public List<ComparisonItemViewModel> Items { get; set; } = new();

    public int TotalItems => Items.Count;
    public int TotalMatches => Items.Count(i => i.Status == ComparisonStatus.Match);
    public int TotalMissingInEf => Items.Count(i => i.Status == ComparisonStatus.MissingInEfModel);
    public int TotalMissingInDb => Items.Count(i => i.Status == ComparisonStatus.MissingInDatabase);
    public int TotalTypeMismatches => Items.Count(i => i.Status == ComparisonStatus.TypeMismatch);
}

public class ComparisonItemViewModel
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? DatabaseType { get; set; }
    public string? EfClrType { get; set; }
    public string? EfEntityName { get; set; }
    public string? EfPropertyName { get; set; }
    public ComparisonStatus Status { get; set; }
    public string? Notes { get; set; }

    public string StatusDisplay => Status switch
    {
        ComparisonStatus.Match => "Match",
        ComparisonStatus.MissingInEfModel => "Missing in EF Model",
        ComparisonStatus.MissingInDatabase => "Missing in Database",
        ComparisonStatus.TypeMismatch => "Type Mismatch",
        _ => "Unknown"
    };

    public string StatusBadgeClass => Status switch
    {
        ComparisonStatus.Match => "bg-success",
        ComparisonStatus.MissingInEfModel => "bg-warning",
        ComparisonStatus.MissingInDatabase => "bg-danger",
        ComparisonStatus.TypeMismatch => "bg-info",
        _ => "bg-secondary"
    };
}

public enum ComparisonStatus
{
    Match,
    MissingInEfModel,
    MissingInDatabase,
    TypeMismatch
}
```

### 4.3 Create EF Model Service

**src/NetSqlDataDicV2.Web/Services/IEfModelService.cs:**
```csharp
using NetSqlDataDicV2.Web.Models.Dto;

namespace NetSqlDataDicV2.Web.Services;

public interface IEfModelService
{
    List<EfModelColumnDto> GetEfModelColumns();
}
```

**src/NetSqlDataDicV2.Web/Services/EfModelService.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Models.Dto;

namespace NetSqlDataDicV2.Web.Services;

public class EfModelService : IEfModelService
{
    private readonly SourceDbContext _sourceContext;
    private readonly ILogger<EfModelService> _logger;

    public EfModelService(
        SourceDbContext sourceContext,
        ILogger<EfModelService> logger)
    {
        _sourceContext = sourceContext;
        _logger = logger;
    }

    public List<EfModelColumnDto> GetEfModelColumns()
    {
        var results = new List<EfModelColumnDto>();
        var model = _sourceContext.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            // Skip shadow types and query types
            if (entityType.IsOwned() || entityType.ClrType == null)
                continue;

            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema() ?? "dbo";

            _logger.LogDebug("Processing entity {Entity} -> {Schema}.{Table}",
                entityType.ClrType.Name, schemaName, tableName);

            foreach (var property in entityType.GetProperties())
            {
                // Skip shadow properties
                if (property.IsShadowProperty())
                    continue;

                var columnName = property.GetColumnName();
                var maxLength = property.GetMaxLength();

                results.Add(new EfModelColumnDto
                {
                    EntityName = entityType.ClrType.Name,
                    PropertyName = property.Name,
                    ClrType = GetClrTypeName(property.ClrType),
                    ColumnName = columnName,
                    TableName = tableName,
                    SchemaName = schemaName,
                    IsNullable = property.IsNullable,
                    MaxLength = maxLength
                });
            }
        }

        _logger.LogInformation("Extracted {Count} columns from EF Core model", results.Count);
        return results;
    }

    private static string GetClrTypeName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return $"{GetSimpleTypeName(underlyingType)}?";
        }

        return GetSimpleTypeName(type);
    }

    private static string GetSimpleTypeName(Type type)
    {
        // Map common types to C# keywords
        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "Boolean" => "bool",
            "String" => "string",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "DateTime" => "DateTime",
            "DateTimeOffset" => "DateTimeOffset",
            "TimeSpan" => "TimeSpan",
            "Guid" => "Guid",
            "Byte[]" => "byte[]",
            _ => type.Name
        };
    }
}
```

### 4.4 Create Comparison Service

**src/NetSqlDataDicV2.Web/Services/IComparisonService.cs:**
```csharp
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IComparisonService
{
    Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default);
}
```

**src/NetSqlDataDicV2.Web/Services/ComparisonService.cs:**
```csharp
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public class ComparisonService : IComparisonService
{
    private readonly IDataDictionaryService _dataDictionaryService;
    private readonly IEfModelService _efModelService;
    private readonly ILogger<ComparisonService> _logger;

    // SQL Server to CLR type mapping
    private static readonly Dictionary<string, string[]> SqlToClrTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["INT"] = new[] { "int", "Int32", "int?" },
        ["BIGINT"] = new[] { "long", "Int64", "long?" },
        ["SMALLINT"] = new[] { "short", "Int16", "short?" },
        ["TINYINT"] = new[] { "byte", "Byte", "byte?" },
        ["BIT"] = new[] { "bool", "Boolean", "bool?" },
        ["DECIMAL"] = new[] { "decimal", "Decimal", "decimal?" },
        ["NUMERIC"] = new[] { "decimal", "Decimal", "decimal?" },
        ["MONEY"] = new[] { "decimal", "Decimal", "decimal?" },
        ["SMALLMONEY"] = new[] { "decimal", "Decimal", "decimal?" },
        ["FLOAT"] = new[] { "double", "Double", "double?" },
        ["REAL"] = new[] { "float", "Single", "float?" },
        ["DATETIME"] = new[] { "DateTime", "DateTime?" },
        ["DATETIME2"] = new[] { "DateTime", "DateTime?" },
        ["SMALLDATETIME"] = new[] { "DateTime", "DateTime?" },
        ["DATE"] = new[] { "DateTime", "DateOnly", "DateTime?", "DateOnly?" },
        ["TIME"] = new[] { "TimeSpan", "TimeOnly", "TimeSpan?", "TimeOnly?" },
        ["DATETIMEOFFSET"] = new[] { "DateTimeOffset", "DateTimeOffset?" },
        ["VARCHAR"] = new[] { "string", "String" },
        ["NVARCHAR"] = new[] { "string", "String" },
        ["CHAR"] = new[] { "string", "String" },
        ["NCHAR"] = new[] { "string", "String" },
        ["TEXT"] = new[] { "string", "String" },
        ["NTEXT"] = new[] { "string", "String" },
        ["UNIQUEIDENTIFIER"] = new[] { "Guid", "Guid?" },
        ["VARBINARY"] = new[] { "byte[]", "Byte[]" },
        ["BINARY"] = new[] { "byte[]", "Byte[]" },
        ["IMAGE"] = new[] { "byte[]", "Byte[]" },
        ["XML"] = new[] { "string", "String" }
    };

    public ComparisonService(
        IDataDictionaryService dataDictionaryService,
        IEfModelService efModelService,
        ILogger<ComparisonService> logger)
    {
        _dataDictionaryService = dataDictionaryService;
        _efModelService = efModelService;
        _logger = logger;
    }

    public async Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting comparison for {Server}/{Database}", server, database);

        // Get data dictionary entries
        var dictionaryEntries = await _dataDictionaryService.GetByDatabaseAsync(
            server, database, cancellationToken);

        var columnEntries = dictionaryEntries
            .Where(d => !string.IsNullOrEmpty(d.ColumnName))
            .ToList();

        // Get EF model columns
        var efColumns = _efModelService.GetEfModelColumns();

        _logger.LogInformation(
            "Comparing {DictCount} dictionary entries with {EfCount} EF model columns",
            columnEntries.Count, efColumns.Count);

        // Build lookup dictionaries
        var dictLookup = columnEntries.ToDictionary(
            d => $"{d.SchemaName}.{d.TableName}.{d.ColumnName}".ToUpperInvariant(),
            StringComparer.OrdinalIgnoreCase);

        var efLookup = efColumns
            .Where(e => !string.IsNullOrEmpty(e.TableName) && !string.IsNullOrEmpty(e.ColumnName))
            .ToDictionary(
                e => $"{e.SchemaName ?? "dbo"}.{e.TableName}.{e.ColumnName}".ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase);

        var results = new List<ComparisonItemViewModel>();
        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Compare dictionary entries against EF model
        foreach (var dict in columnEntries)
        {
            var key = $"{dict.SchemaName}.{dict.TableName}.{dict.ColumnName}".ToUpperInvariant();
            processedKeys.Add(key);

            if (efLookup.TryGetValue(key, out var ef))
            {
                // Both exist - check type compatibility
                var isCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);

                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = dict.SchemaName,
                    TableName = dict.TableName,
                    ColumnName = dict.ColumnName,
                    DatabaseType = dict.DataType,
                    EfClrType = ef.ClrType,
                    EfEntityName = ef.EntityName,
                    EfPropertyName = ef.PropertyName,
                    Status = isCompatible ? ComparisonStatus.Match : ComparisonStatus.TypeMismatch,
                    Notes = isCompatible ? null : $"DB type '{dict.DataType}' may not match CLR type '{ef.ClrType}'"
                });
            }
            else
            {
                // In dictionary but not in EF model
                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = dict.SchemaName,
                    TableName = dict.TableName,
                    ColumnName = dict.ColumnName,
                    DatabaseType = dict.DataType,
                    Status = ComparisonStatus.MissingInEfModel,
                    Notes = "Column exists in database but not mapped in EF Core model. Re-scaffold may be needed."
                });
            }
        }

        // Find columns in EF but not in dictionary
        foreach (var ef in efColumns.Where(e =>
            !string.IsNullOrEmpty(e.TableName) && !string.IsNullOrEmpty(e.ColumnName)))
        {
            var key = $"{ef.SchemaName ?? "dbo"}.{ef.TableName}.{ef.ColumnName}".ToUpperInvariant();

            if (!processedKeys.Contains(key))
            {
                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = ef.SchemaName ?? "dbo",
                    TableName = ef.TableName!,
                    ColumnName = ef.ColumnName,
                    EfClrType = ef.ClrType,
                    EfEntityName = ef.EntityName,
                    EfPropertyName = ef.PropertyName,
                    Status = ComparisonStatus.MissingInDatabase,
                    Notes = "Property exists in EF Core model but column not found in database. Add column and sync."
                });
            }
        }

        var result = new ComparisonResultViewModel
        {
            DatabaseServer = server,
            DatabaseName = database,
            ComparisonTime = DateTime.UtcNow,
            Items = results
                .OrderBy(r => r.Status)
                .ThenBy(r => r.SchemaName)
                .ThenBy(r => r.TableName)
                .ThenBy(r => r.ColumnName)
                .ToList()
        };

        _logger.LogInformation(
            "Comparison complete: {Matches} matches, {MissingEf} missing in EF, {MissingDb} missing in DB, {Mismatches} type mismatches",
            result.TotalMatches, result.TotalMissingInEf, result.TotalMissingInDb, result.TotalTypeMismatches);

        return result;
    }

    private bool IsTypeCompatible(string? sqlType, string? clrType)
    {
        if (string.IsNullOrEmpty(sqlType) || string.IsNullOrEmpty(clrType))
            return true; // Can't determine, assume compatible

        // Extract base SQL type (remove size specifiers)
        var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();

        // Normalize CLR type (remove nullable indicator for lookup)
        var normalizedClrType = clrType.TrimEnd('?');

        if (SqlToClrTypeMap.TryGetValue(baseSqlType, out var compatibleTypes))
        {
            return compatibleTypes.Any(t =>
                t.Equals(clrType, StringComparison.OrdinalIgnoreCase) ||
                t.Equals(normalizedClrType, StringComparison.OrdinalIgnoreCase));
        }

        // Unknown SQL type - log and assume compatible
        _logger.LogWarning("Unknown SQL type '{SqlType}' - assuming compatible with '{ClrType}'",
            sqlType, clrType);
        return true;
    }
}
```

### 4.5 Register Services and DbContext in Program.cs

Add to **src/NetSqlDataDicV2.Web/Program.cs**:
```csharp
using NetSqlDataDicV2.SourceModels;

// Add SourceDbContext (read-only, for model reflection)
builder.Services.AddDbContext<SourceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SourceDatabase")));

// Add comparison services
builder.Services.AddScoped<IEfModelService, EfModelService>();
builder.Services.AddScoped<IComparisonService, ComparisonService>();
```

### 4.6 Create Comparison Controller

**src/NetSqlDataDicV2.Web/Controllers/ComparisonController.cs:**
```csharp
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class ComparisonController : Controller
{
    private readonly IComparisonService _comparisonService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        IComparisonService comparisonService,
        IConfiguration configuration,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewBag.SourceServer = _configuration["SourceDatabase:Server"] ?? "localhost";
        ViewBag.SourceDatabase = _configuration["SourceDatabase:Database"] ?? "";

        return View(new ComparisonResultViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Compare(CancellationToken cancellationToken)
    {
        try
        {
            var server = _configuration["SourceDatabase:Server"] ?? "localhost";
            var database = _configuration["SourceDatabase:Database"] ?? "";

            var result = await _comparisonService.CompareAsync(server, database, cancellationToken);

            return Json(new
            {
                success = true,
                totalItems = result.TotalItems,
                totalMatches = result.TotalMatches,
                totalMissingInEf = result.TotalMissingInEf,
                totalMissingInDb = result.TotalMissingInDb,
                totalTypeMismatches = result.TotalTypeMismatches,
                items = result.Items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CompareGrid(
        [DataSourceRequest] DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var server = _configuration["SourceDatabase:Server"] ?? "localhost";
            var database = _configuration["SourceDatabase:Database"] ?? "";

            var result = await _comparisonService.CompareAsync(server, database, cancellationToken);

            return Json(result.Items.ToDataSourceResult(request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison grid failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 4.7 Create Comparison View

**src/NetSqlDataDicV2.Web/Views/Comparison/Index.cshtml:**
```html
@model NetSqlDataDicV2.Web.Models.ViewModels.ComparisonResultViewModel
@{
    ViewData["Title"] = "EF Core Comparison";
}

<h2>Compare Data Dictionary with EF Core Models</h2>

<div class="row mb-4">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h5 class="mb-0">Comparison Configuration</h5>
            </div>
            <div class="card-body">
                <dl class="row mb-3">
                    <dt class="col-sm-4">Server</dt>
                    <dd class="col-sm-8"><code>@ViewBag.SourceServer</code></dd>

                    <dt class="col-sm-4">Database</dt>
                    <dd class="col-sm-8"><code>@ViewBag.SourceDatabase</code></dd>
                </dl>

                <button type="button" id="runComparison" class="btn btn-primary">
                    <span class="spinner-border spinner-border-sm d-none" role="status"></span>
                    <span class="btn-text">Run Comparison</span>
                </button>
            </div>
        </div>
    </div>

    <div class="col-md-6">
        <div id="summaryCard" class="card d-none">
            <div class="card-header">
                <h5 class="mb-0">Comparison Summary</h5>
            </div>
            <div class="card-body">
                <div class="row text-center">
                    <div class="col">
                        <h3 id="matchCount" class="text-success mb-0">0</h3>
                        <small class="text-muted">Matches</small>
                    </div>
                    <div class="col">
                        <h3 id="missingEfCount" class="text-warning mb-0">0</h3>
                        <small class="text-muted">Missing in EF</small>
                    </div>
                    <div class="col">
                        <h3 id="missingDbCount" class="text-danger mb-0">0</h3>
                        <small class="text-muted">Missing in DB</small>
                    </div>
                    <div class="col">
                        <h3 id="mismatchCount" class="text-info mb-0">0</h3>
                        <small class="text-muted">Type Mismatches</small>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h5 class="mb-0">Comparison Results</h5>
        <div>
            <div class="btn-group btn-group-sm" role="group">
                <button type="button" class="btn btn-outline-secondary filter-btn active" data-filter="all">All</button>
                <button type="button" class="btn btn-outline-success filter-btn" data-filter="Match">Matches</button>
                <button type="button" class="btn btn-outline-warning filter-btn" data-filter="MissingInEfModel">Missing in EF</button>
                <button type="button" class="btn btn-outline-danger filter-btn" data-filter="MissingInDatabase">Missing in DB</button>
                <button type="button" class="btn btn-outline-info filter-btn" data-filter="TypeMismatch">Mismatches</button>
            </div>
        </div>
    </div>
    <div class="card-body p-0">
        <div id="comparisonGrid"></div>
    </div>
</div>

@section Scripts {
<script>
    var comparisonData = [];
    var grid = null;

    $(document).ready(function () {
        // Initialize empty grid
        initializeGrid([]);

        // Run comparison button
        $("#runComparison").on("click", function () {
            var $btn = $(this);
            var $spinner = $btn.find(".spinner-border");
            var $text = $btn.find(".btn-text");

            $btn.prop("disabled", true);
            $spinner.removeClass("d-none");
            $text.text("Comparing...");

            $.ajax({
                url: "@Url.Action("Compare", "Comparison")",
                type: "POST",
                success: function (data) {
                    if (data.success) {
                        // Update summary
                        $("#summaryCard").removeClass("d-none");
                        $("#matchCount").text(data.totalMatches);
                        $("#missingEfCount").text(data.totalMissingInEf);
                        $("#missingDbCount").text(data.totalMissingInDb);
                        $("#mismatchCount").text(data.totalTypeMismatches);

                        // Store data and refresh grid
                        comparisonData = data.items;
                        refreshGrid(comparisonData);
                    } else {
                        alert("Comparison failed: " + (data.error || "Unknown error"));
                    }
                },
                error: function (xhr, status, error) {
                    alert("Error: " + error);
                },
                complete: function () {
                    $btn.prop("disabled", false);
                    $spinner.addClass("d-none");
                    $text.text("Run Comparison");
                }
            });
        });

        // Filter buttons
        $(".filter-btn").on("click", function () {
            $(".filter-btn").removeClass("active");
            $(this).addClass("active");

            var filter = $(this).data("filter");
            if (filter === "all") {
                refreshGrid(comparisonData);
            } else {
                var filtered = comparisonData.filter(function (item) {
                    return item.status === getStatusValue(filter);
                });
                refreshGrid(filtered);
            }
        });
    });

    function getStatusValue(statusName) {
        var statusMap = {
            "Match": 0,
            "MissingInEfModel": 1,
            "MissingInDatabase": 2,
            "TypeMismatch": 3
        };
        return statusMap[statusName];
    }

    function getStatusName(statusValue) {
        var statusNames = ["Match", "Missing in EF Model", "Missing in Database", "Type Mismatch"];
        return statusNames[statusValue] || "Unknown";
    }

    function getStatusBadgeClass(statusValue) {
        var classes = ["bg-success", "bg-warning", "bg-danger", "bg-info"];
        return classes[statusValue] || "bg-secondary";
    }

    function initializeGrid(data) {
        grid = $("#comparisonGrid").kendoGrid({
            dataSource: {
                data: data,
                pageSize: 50
            },
            height: 550,
            sortable: true,
            filterable: {
                mode: "row"
            },
            pageable: {
                pageSizes: [25, 50, 100, "all"],
                refresh: true
            },
            groupable: true,
            resizable: true,
            columns: [
                {
                    field: "status",
                    title: "Status",
                    width: 150,
                    template: function (dataItem) {
                        return '<span class="badge ' + getStatusBadgeClass(dataItem.status) + '">' +
                            getStatusName(dataItem.status) + '</span>';
                    },
                    filterable: {
                        cell: {
                            template: function (args) {
                                args.element.kendoDropDownList({
                                    dataSource: [
                                        { text: "All", value: "" },
                                        { text: "Match", value: 0 },
                                        { text: "Missing in EF Model", value: 1 },
                                        { text: "Missing in Database", value: 2 },
                                        { text: "Type Mismatch", value: 3 }
                                    ],
                                    dataTextField: "text",
                                    dataValueField: "value",
                                    valuePrimitive: true
                                });
                            },
                            showOperators: false
                        }
                    }
                },
                {
                    field: "schemaName",
                    title: "Schema",
                    width: 80
                },
                {
                    field: "tableName",
                    title: "Table",
                    width: 150,
                    filterable: { cell: { operator: "contains" } }
                },
                {
                    field: "columnName",
                    title: "Column",
                    width: 150,
                    filterable: { cell: { operator: "contains" } }
                },
                {
                    field: "databaseType",
                    title: "DB Type",
                    width: 120
                },
                {
                    field: "efEntityName",
                    title: "Entity",
                    width: 150,
                    filterable: { cell: { operator: "contains" } }
                },
                {
                    field: "efPropertyName",
                    title: "Property",
                    width: 150
                },
                {
                    field: "efClrType",
                    title: "CLR Type",
                    width: 100
                },
                {
                    field: "notes",
                    title: "Notes",
                    width: 250
                }
            ]
        }).data("kendoGrid");
    }

    function refreshGrid(data) {
        if (grid) {
            grid.dataSource.data(data);
        }
    }
</script>
}
```

## Deliverables

- [ ] EfModelColumnDto for EF model metadata
- [ ] ComparisonResultViewModel and ComparisonItemViewModel
- [ ] IEfModelService interface
- [ ] EfModelService implementation using EF Core IModel
- [ ] IComparisonService interface
- [ ] ComparisonService with type mapping logic
- [ ] ComparisonController
- [ ] Comparison view with Kendo Grid and filtering
- [ ] SourceDbContext registered and working

## Verification

1. Scaffold source database models into SourceModels project
2. Build solution successfully
3. Navigate to /Comparison
4. Click "Run Comparison"
5. Should see summary statistics
6. Grid should show all comparison items
7. Filter buttons should filter results by status
8. Kendo Grid filtering/sorting should work
