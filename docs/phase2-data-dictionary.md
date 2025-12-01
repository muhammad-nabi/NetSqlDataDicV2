# Phase 2: Data Dictionary Core

## Objective
Implement the data dictionary viewing and editing functionality with Kendo UI Grid, including server-side paging, filtering, sorting, and inline editing.

## Tasks

### 2.1 Create DTOs and ViewModels

**src/NetSqlDataDicV2.Web/Models/ViewModels/DataElementViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementViewModel
{
    public int DataElementId { get; set; }
    public string DataElementName { get; set; } = string.Empty;
    public string? DataElementType { get; set; }
    public string? DataType { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
    public string? ForeignKeyTo { get; set; }
    public long? RowCount { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int? MaxLength { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public DateTime? LastSyncTime { get; set; }

    // Computed display properties
    public string FullyQualifiedName =>
        string.IsNullOrEmpty(ColumnName)
            ? $"[{SchemaName}].[{TableName}]"
            : $"[{SchemaName}].[{TableName}].[{ColumnName}]";

    public string DisplayDataType
    {
        get
        {
            if (string.IsNullOrEmpty(DataType)) return string.Empty;

            var baseType = DataType.ToUpperInvariant();
            if (MaxLength.HasValue && (baseType.Contains("VARCHAR") || baseType.Contains("CHAR")))
            {
                return MaxLength == -1 ? $"{DataType}(MAX)" : $"{DataType}({MaxLength})";
            }
            return DataType;
        }
    }
}
```

**src/NetSqlDataDicV2.Web/Models/ViewModels/DataElementUpdateViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementUpdateViewModel
{
    public int DataElementId { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
}
```

### 2.2 Create Service Interface and Implementation

**src/NetSqlDataDicV2.Web/Services/IDataDictionaryService.cs:**
```csharp
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IDataDictionaryService
{
    IQueryable<DataElement> GetQueryable();
    Task<DataElementViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<DataElementViewModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<DataElementViewModel>> GetByDatabaseAsync(string server, string database, CancellationToken cancellationToken = default);
    Task<DataElementViewModel> UpdateAsync(DataElementUpdateViewModel model, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctServersAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctDatabasesAsync(string? server = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctTablesAsync(string? server = null, string? database = null, CancellationToken cancellationToken = default);
}
```

**src/NetSqlDataDicV2.Web/Services/DataDictionaryService.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public class DataDictionaryService : IDataDictionaryService
{
    private readonly DataDictionaryDbContext _context;
    private readonly ILogger<DataDictionaryService> _logger;

    public DataDictionaryService(
        DataDictionaryDbContext context,
        ILogger<DataDictionaryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<DataElement> GetQueryable()
    {
        return _context.DataElements.AsNoTracking();
    }

    public async Task<DataElementViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataElements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DataElementId == id, cancellationToken);

        return entity is null ? null : MapToViewModel(entity);
    }

    public async Task<List<DataElementViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataElements
            .AsNoTracking()
            .OrderBy(e => e.DatabaseServer)
            .ThenBy(e => e.DatabaseName)
            .ThenBy(e => e.SchemaName)
            .ThenBy(e => e.TableName)
            .ThenBy(e => e.ColumnName)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToViewModel).ToList();
    }

    public async Task<List<DataElementViewModel>> GetByDatabaseAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataElements
            .AsNoTracking()
            .Where(e => e.DatabaseServer == server && e.DatabaseName == database)
            .OrderBy(e => e.SchemaName)
            .ThenBy(e => e.TableName)
            .ThenBy(e => e.ColumnName)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToViewModel).ToList();
    }

    public async Task<DataElementViewModel> UpdateAsync(
        DataElementUpdateViewModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataElements
            .FirstOrDefaultAsync(e => e.DataElementId == model.DataElementId, cancellationToken)
            ?? throw new InvalidOperationException($"DataElement with ID {model.DataElementId} not found");

        // Only update user-editable fields
        entity.DataPurpose = model.DataPurpose;
        entity.EntityPurpose = model.EntityPurpose;
        entity.OriginalDataSource = model.OriginalDataSource;
        entity.Notes = model.Notes;
        entity.LastUpdateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated DataElement {Id}: {Table}.{Column}",
            entity.DataElementId, entity.TableName, entity.ColumnName);

        return MapToViewModel(entity);
    }

    public async Task<List<string>> GetDistinctServersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.DataElements
            .AsNoTracking()
            .Select(e => e.DatabaseServer)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctDatabasesAsync(
        string? server = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataElements.AsNoTracking();

        if (!string.IsNullOrEmpty(server))
        {
            query = query.Where(e => e.DatabaseServer == server);
        }

        return await query
            .Select(e => e.DatabaseName)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctTablesAsync(
        string? server = null,
        string? database = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataElements.AsNoTracking();

        if (!string.IsNullOrEmpty(server))
        {
            query = query.Where(e => e.DatabaseServer == server);
        }

        if (!string.IsNullOrEmpty(database))
        {
            query = query.Where(e => e.DatabaseName == database);
        }

        return await query
            .Select(e => e.TableName)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    private static DataElementViewModel MapToViewModel(DataElement entity) => new()
    {
        DataElementId = entity.DataElementId,
        DataElementName = entity.DataElementName,
        DataElementType = entity.DataElementType,
        DataType = entity.DataType,
        DataPurpose = entity.DataPurpose,
        EntityPurpose = entity.EntityPurpose,
        DatabaseServer = entity.DatabaseServer,
        DatabaseName = entity.DatabaseName,
        SchemaName = entity.SchemaName,
        TableName = entity.TableName,
        ColumnName = entity.ColumnName,
        OriginalDataSource = entity.OriginalDataSource,
        Notes = entity.Notes,
        ForeignKeyTo = entity.ForeignKeyTo,
        RowCount = entity.RowCount,
        IsNullable = entity.IsNullable,
        IsPrimaryKey = entity.IsPrimaryKey,
        MaxLength = entity.MaxLength,
        CreateTime = entity.CreateTime,
        LastUpdateTime = entity.LastUpdateTime,
        LastSyncTime = entity.LastSyncTime
    };
}
```

### 2.3 Register Service in Program.cs

Add to **src/NetSqlDataDicV2.Web/Program.cs** (before `var app = builder.Build();`):
```csharp
// Add application services
builder.Services.AddScoped<IDataDictionaryService, DataDictionaryService>();
```

### 2.4 Create Controller

**src/NetSqlDataDicV2.Web/Controllers/DataDictionaryController.cs:**
```csharp
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class DataDictionaryController : Controller
{
    private readonly IDataDictionaryService _service;
    private readonly ILogger<DataDictionaryController> _logger;

    public DataDictionaryController(
        IDataDictionaryService service,
        ILogger<DataDictionaryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Servers = await _service.GetDistinctServersAsync();
        ViewBag.Databases = await _service.GetDistinctDatabasesAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var query = _service.GetQueryable()
                .Select(e => new DataElementViewModel
                {
                    DataElementId = e.DataElementId,
                    DataElementName = e.DataElementName,
                    DataElementType = e.DataElementType,
                    DataType = e.DataType,
                    DataPurpose = e.DataPurpose,
                    EntityPurpose = e.EntityPurpose,
                    DatabaseServer = e.DatabaseServer,
                    DatabaseName = e.DatabaseName,
                    SchemaName = e.SchemaName,
                    TableName = e.TableName,
                    ColumnName = e.ColumnName,
                    OriginalDataSource = e.OriginalDataSource,
                    Notes = e.Notes,
                    ForeignKeyTo = e.ForeignKeyTo,
                    RowCount = e.RowCount,
                    IsNullable = e.IsNullable,
                    IsPrimaryKey = e.IsPrimaryKey,
                    MaxLength = e.MaxLength,
                    CreateTime = e.CreateTime,
                    LastUpdateTime = e.LastUpdateTime,
                    LastSyncTime = e.LastSyncTime
                });

            var result = await query.ToDataSourceResultAsync(request);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading data elements");
            return StatusCode(500, new { error = "An error occurred while loading data" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(
        [DataSourceRequest] DataSourceRequest request,
        DataElementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        try
        {
            var updateModel = new DataElementUpdateViewModel
            {
                DataElementId = model.DataElementId,
                DataPurpose = model.DataPurpose,
                EntityPurpose = model.EntityPurpose,
                OriginalDataSource = model.OriginalDataSource,
                Notes = model.Notes
            };

            var updated = await _service.UpdateAsync(updateModel);
            return Json(new[] { updated }.ToDataSourceResult(request, ModelState));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data element {Id}", model.DataElementId);
            ModelState.AddModelError("", "An error occurred while saving");
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDatabases(string server)
    {
        var databases = await _service.GetDistinctDatabasesAsync(server);
        return Json(databases);
    }

    [HttpGet]
    public async Task<IActionResult> GetTables(string server, string database)
    {
        var tables = await _service.GetDistinctTablesAsync(server, database);
        return Json(tables);
    }
}
```

### 2.5 Create View with Kendo Grid

**src/NetSqlDataDicV2.Web/Views/DataDictionary/Index.cshtml:**
```html
@{
    ViewData["Title"] = "Data Dictionary";
}

<div class="d-flex justify-content-between align-items-center mb-3">
    <h2>Data Dictionary</h2>
    <div>
        <span id="recordCount" class="text-muted me-3"></span>
        <button type="button" class="btn btn-outline-secondary btn-sm" onclick="clearFilters()">
            Clear Filters
        </button>
    </div>
</div>

<div id="grid"></div>

@section Scripts {
<script>
    $(document).ready(function () {
        var grid = $("#grid").kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "@Url.Action("Read", "DataDictionary")",
                        type: "POST",
                        dataType: "json"
                    },
                    update: {
                        url: "@Url.Action("Update", "DataDictionary")",
                        type: "POST",
                        dataType: "json"
                    },
                    parameterMap: function (data, operation) {
                        return kendo.stringify(data);
                    }
                },
                requestStart: function() {
                    kendo.ui.progress($("#grid"), true);
                },
                requestEnd: function(e) {
                    kendo.ui.progress($("#grid"), false);
                    if (e.response && e.response.total !== undefined) {
                        $("#recordCount").text(e.response.total + " records");
                    }
                },
                schema: {
                    data: "data",
                    total: "total",
                    model: {
                        id: "dataElementId",
                        fields: {
                            dataElementId: { editable: false, type: "number" },
                            dataElementName: { editable: false, type: "string" },
                            dataElementType: { editable: false, type: "string" },
                            dataType: { editable: false, type: "string" },
                            dataPurpose: { type: "string" },
                            entityPurpose: { type: "string" },
                            databaseServer: { editable: false, type: "string" },
                            databaseName: { editable: false, type: "string" },
                            schemaName: { editable: false, type: "string" },
                            tableName: { editable: false, type: "string" },
                            columnName: { editable: false, type: "string" },
                            originalDataSource: { type: "string" },
                            notes: { type: "string" },
                            foreignKeyTo: { editable: false, type: "string" },
                            rowCount: { editable: false, type: "number" },
                            isNullable: { editable: false, type: "boolean" },
                            isPrimaryKey: { editable: false, type: "boolean" },
                            maxLength: { editable: false, type: "number" },
                            lastUpdateTime: { editable: false, type: "date" },
                            lastSyncTime: { editable: false, type: "date" }
                        }
                    }
                },
                pageSize: 50,
                serverPaging: true,
                serverFiltering: true,
                serverSorting: true
            },
            height: 650,
            sortable: {
                mode: "multiple",
                allowUnsort: true
            },
            filterable: {
                mode: "row"
            },
            pageable: {
                refresh: true,
                pageSizes: [25, 50, 100, 200],
                buttonCount: 5
            },
            groupable: true,
            resizable: true,
            reorderable: true,
            columnMenu: true,
            editable: "inline",
            toolbar: [
                { template: '<input type="search" id="globalSearch" class="k-textbox" placeholder="Search all columns..." style="width: 300px;" />' }
            ],
            columns: [
                {
                    field: "databaseServer",
                    title: "Server",
                    width: 120,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "databaseName",
                    title: "Database",
                    width: 120,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "schemaName",
                    title: "Schema",
                    width: 80,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "eq"
                        }
                    }
                },
                {
                    field: "tableName",
                    title: "Table",
                    width: 150,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "columnName",
                    title: "Column",
                    width: 150,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "dataType",
                    title: "Data Type",
                    width: 110,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "isNullable",
                    title: "Nullable",
                    width: 80,
                    template: "#= isNullable ? 'Yes' : 'No' #",
                    filterable: {
                        cell: {
                            template: function(args) {
                                args.element.kendoDropDownList({
                                    dataSource: [
                                        { text: "All", value: "" },
                                        { text: "Yes", value: "true" },
                                        { text: "No", value: "false" }
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
                    field: "isPrimaryKey",
                    title: "PK",
                    width: 60,
                    template: "#= isPrimaryKey ? '✓' : '' #",
                    filterable: false
                },
                {
                    field: "foreignKeyTo",
                    title: "FK To",
                    width: 180,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "dataPurpose",
                    title: "Purpose",
                    width: 200,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "notes",
                    title: "Notes",
                    width: 200,
                    filterable: {
                        cell: {
                            showOperators: false,
                            operator: "contains"
                        }
                    }
                },
                {
                    field: "lastUpdateTime",
                    title: "Updated",
                    width: 140,
                    format: "{0:yyyy-MM-dd HH:mm}",
                    filterable: false
                },
                {
                    command: ["edit"],
                    title: "Actions",
                    width: 100
                }
            ]
        }).data("kendoGrid");

        // Global search functionality
        $("#globalSearch").on("keyup", function () {
            var value = $(this).val();
            if (value.length >= 2 || value.length === 0) {
                var filter = { logic: "or", filters: [] };

                if (value) {
                    filter.filters = [
                        { field: "tableName", operator: "contains", value: value },
                        { field: "columnName", operator: "contains", value: value },
                        { field: "dataPurpose", operator: "contains", value: value },
                        { field: "notes", operator: "contains", value: value },
                        { field: "dataType", operator: "contains", value: value }
                    ];
                }

                grid.dataSource.filter(filter.filters.length > 0 ? filter : {});
            }
        });
    });

    function clearFilters() {
        var grid = $("#grid").data("kendoGrid");
        grid.dataSource.filter({});
        $("#globalSearch").val("");

        // Clear row filter inputs
        $(".k-filtercell input").val("");
        $(".k-filtercell .k-dropdown").each(function() {
            var ddl = $(this).data("kendoDropDownList");
            if (ddl) ddl.value("");
        });
    }
</script>
}
```

## Deliverables

- [ ] DataElementViewModel and DataElementUpdateViewModel
- [ ] IDataDictionaryService interface
- [ ] DataDictionaryService implementation
- [ ] DataDictionaryController with Kendo Grid support
- [ ] Index view with full-featured Kendo Grid
- [ ] Server-side paging, filtering, sorting working
- [ ] Inline editing for editable fields (Purpose, Notes, etc.)
- [ ] Global search functionality

## Verification

1. Navigate to /DataDictionary
2. Grid should load (initially empty until sync is run)
3. Filtering on each column should work
4. Sorting should work (click column headers)
5. Paging should work
6. Global search should filter across multiple columns
7. Inline edit should save changes (once data exists)
