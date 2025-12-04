# Phase 4: UI Layer

## Overview

This phase adds the user interface for managing EF Model Sources and modifies the Comparison page to support source selection. Uses Kendo UI components consistent with the existing application style.

## Goals

- Create CRUD pages for EF Model Sources management
- Add DbContext discovery functionality in the UI
- Modify Comparison page to select from configured sources
- Maintain backward compatibility with existing comparison flow

## Prerequisites

- Phases 1-3 completed
- Kendo UI components available

## Implementation Steps

### Step 4.1: Create EfModelSourcesController

**New File:** `src/NetSqlDataDicV2.Web/Controllers/EfModelSourcesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Web.Controllers;

public class EfModelSourcesController : Controller
{
    private readonly IEfModelSourceService _sourceService;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<EfModelSourcesController> _logger;

    public EfModelSourcesController(
        IEfModelSourceService sourceService,
        IDbContextProviderFactory providerFactory,
        ILogger<EfModelSourcesController> logger)
    {
        _sourceService = sourceService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// List all EF Model Sources.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sources = await _sourceService.GetAllAsync(ct);
        return View(sources);
    }

    /// <summary>
    /// Get sources for Kendo Grid (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetSources(CancellationToken ct)
    {
        var sources = await _sourceService.GetAllAsync(ct);
        return Json(new { Data = sources, Total = sources.Count });
    }

    /// <summary>
    /// Show create form.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new EfModelSourceCreateViewModel
        {
            ProviderType = "DynamicDll"
        };

        ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
        return View(model);
    }

    /// <summary>
    /// Create new source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EfModelSourceCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }

        try
        {
            await _sourceService.CreateAsync(model, ct);
            TempData["SuccessMessage"] = $"Source '{model.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create source");
            ModelState.AddModelError("", $"Failed to create source: {ex.Message}");
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Show edit form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var source = await _sourceService.GetByIdAsync(id, ct);

        if (source == null)
        {
            return NotFound();
        }

        var model = new EfModelSourceEditViewModel
        {
            Id = source.Id,
            Name = source.Name,
            ProviderType = source.ProviderType,
            AssemblyPath = source.AssemblyPath,
            DbContextTypeName = source.DbContextTypeName,
            TargetServer = source.TargetServer,
            TargetDatabase = source.TargetDatabase,
            Description = source.Description,
            IsActive = source.IsActive,
            HasConnectionString = !string.IsNullOrEmpty(source.ConnectionString)
        };

        ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
        return View(model);
    }

    /// <summary>
    /// Update source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EfModelSourceEditViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }

        try
        {
            await _sourceService.UpdateAsync(id, model, ct);
            TempData["SuccessMessage"] = $"Source '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update source {Id}", id);
            ModelState.AddModelError("", $"Failed to update source: {ex.Message}");
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Delete source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _sourceService.DeleteAsync(id, ct);
            return Json(new { success = true, message = "Source deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete source {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle active status.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        try
        {
            var newStatus = await _sourceService.ToggleActiveAsync(id, ct);
            return Json(new { success = true, isActive = newStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle active status for {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Validate source configuration.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Validate(int id, CancellationToken ct)
    {
        var result = await _sourceService.ValidateSourceAsync(id, ct);
        return Json(result);
    }

    /// <summary>
    /// Discover DbContexts in an assembly.
    /// </summary>
    [HttpPost]
    public IActionResult DiscoverDbContexts([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrEmpty(request?.AssemblyPath))
        {
            return Json(new { success = false, message = "Assembly path is required." });
        }

        try
        {
            var dbContexts = _providerFactory.DiscoverDbContexts(request.AssemblyPath);
            return Json(new { success = true, dbContexts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover DbContexts in {Path}", request.AssemblyPath);
            return Json(new { success = false, message = ex.Message });
        }
    }

    public class DiscoverRequest
    {
        public string? AssemblyPath { get; set; }
    }
}
```

### Step 4.2: Create EfModelSources Index View

**New File:** `src/NetSqlDataDicV2.Web/Views/EfModelSources/Index.cshtml`

```html
@model List<NetSqlDataDicV2.Web.Models.ViewModels.EfModelSourceViewModel>
@{
    ViewData["Title"] = "EF Model Sources";
}

<div class="container-fluid py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>EF Model Sources</h2>
        <a asp-action="Create" class="btn btn-primary">
            <i class="fas fa-plus"></i> Add Source
        </a>
    </div>

    @if (TempData["SuccessMessage"] != null)
    {
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            @TempData["SuccessMessage"]
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }

    <div class="card">
        <div class="card-body">
            @(Html.Kendo().Grid<NetSqlDataDicV2.Web.Models.ViewModels.EfModelSourceViewModel>()
                .Name("sourcesGrid")
                .Columns(columns =>
                {
                    columns.Bound(c => c.Name).Width(200);
                    columns.Bound(c => c.ProviderType).Width(120)
                        .ClientTemplate("#= providerTypeBadge(ProviderType) #");
                    columns.Bound(c => c.TargetDisplay).Title("Target Database").Width(200);
                    columns.Bound(c => c.DbContextTypeName).Title("DbContext").Width(200)
                        .ClientTemplate("#= DbContextTypeName || '-' #");
                    columns.Bound(c => c.IsActive).Width(100)
                        .ClientTemplate("#= activeBadge(IsActive) #");
                    columns.Bound(c => c.LastComparedDisplay).Title("Last Compared").Width(150);
                    columns.Command(command =>
                    {
                        command.Custom("validate").Text("Validate").Click("onValidate");
                        command.Custom("compare").Text("Compare").Click("onCompare");
                        command.Custom("edit").Text("Edit").Click("onEdit");
                        command.Custom("delete").Text("Delete").Click("onDelete");
                    }).Width(300);
                })
                .Pageable(p => p.PageSizes(new[] { 10, 25, 50 }))
                .Sortable()
                .Filterable()
                .DataSource(ds => ds
                    .Ajax()
                    .Read(read => read.Action("GetSources", "EfModelSources").Type(HttpVerbs.Post))
                    .PageSize(25)
                )
            )
        </div>
    </div>
</div>

@section Scripts {
    <script>
        function providerTypeBadge(type) {
            if (type === 'DynamicDll') {
                return '<span class="badge bg-info">Dynamic DLL</span>';
            }
            return '<span class="badge bg-secondary">Direct</span>';
        }

        function activeBadge(isActive) {
            if (isActive) {
                return '<span class="badge bg-success">Active</span>';
            }
            return '<span class="badge bg-warning">Inactive</span>';
        }

        function onValidate(e) {
            e.preventDefault();
            var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

            kendo.ui.progress($("#sourcesGrid"), true);

            $.post('@Url.Action("Validate")', { id: dataItem.Id })
                .done(function(result) {
                    if (result.isValid) {
                        kendo.alert("Validation successful: " + result.message);
                    } else {
                        kendo.alert("Validation failed: " + result.errorMessage);
                    }
                })
                .fail(function() {
                    kendo.alert("Error during validation");
                })
                .always(function() {
                    kendo.ui.progress($("#sourcesGrid"), false);
                });
        }

        function onCompare(e) {
            e.preventDefault();
            var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
            window.location.href = '@Url.Action("Index", "Comparison")?sourceId=' + dataItem.Id;
        }

        function onEdit(e) {
            e.preventDefault();
            var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
            window.location.href = '@Url.Action("Edit")/' + dataItem.Id;
        }

        function onDelete(e) {
            e.preventDefault();
            var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

            kendo.confirm("Are you sure you want to delete '" + dataItem.Name + "'?")
                .done(function() {
                    $.post('@Url.Action("Delete")', {
                        id: dataItem.Id,
                        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    })
                    .done(function(result) {
                        if (result.success) {
                            $("#sourcesGrid").data("kendoGrid").dataSource.read();
                        } else {
                            kendo.alert("Delete failed: " + result.message);
                        }
                    });
                });
        }
    </script>
    @Html.AntiForgeryToken()
}
```

### Step 4.3: Create EfModelSources Create View

**New File:** `src/NetSqlDataDicV2.Web/Views/EfModelSources/Create.cshtml`

```html
@model NetSqlDataDicV2.Web.Models.ViewModels.EfModelSourceCreateViewModel
@{
    ViewData["Title"] = "Add EF Model Source";
    var providerTypes = ViewBag.ProviderTypes as List<string> ?? new List<string>();
}

<div class="container py-4">
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index">EF Model Sources</a></li>
            <li class="breadcrumb-item active">Add New</li>
        </ol>
    </nav>

    <div class="card">
        <div class="card-header">
            <h4 class="mb-0">Add EF Model Source</h4>
        </div>
        <div class="card-body">
            <form asp-action="Create" method="post">
                <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>

                <div class="row mb-3">
                    <div class="col-md-6">
                        <label asp-for="Name" class="form-label"></label>
                        <input asp-for="Name" class="form-control" placeholder="e.g., Sales API DbContext" />
                        <span asp-validation-for="Name" class="text-danger"></span>
                    </div>
                    <div class="col-md-6">
                        <label asp-for="ProviderType" class="form-label"></label>
                        @(Html.Kendo().DropDownListFor(m => m.ProviderType)
                            .DataSource(providerTypes)
                            .Events(e => e.Change("onProviderTypeChange"))
                        )
                        <span asp-validation-for="ProviderType" class="text-danger"></span>
                    </div>
                </div>

                <div id="dynamicDllFields">
                    <div class="row mb-3">
                        <div class="col-md-8">
                            <label asp-for="AssemblyPath" class="form-label"></label>
                            <div class="input-group">
                                <input asp-for="AssemblyPath" class="form-control"
                                    placeholder="e.g., C:\Projects\MyApp\bin\Debug\net9.0\MyApp.dll" />
                                <button type="button" class="btn btn-outline-secondary" onclick="discoverDbContexts()">
                                    <i class="fas fa-search"></i> Discover
                                </button>
                            </div>
                            <span asp-validation-for="AssemblyPath" class="text-danger"></span>
                        </div>
                        <div class="col-md-4">
                            <label asp-for="DbContextTypeName" class="form-label"></label>
                            @(Html.Kendo().DropDownListFor(m => m.DbContextTypeName)
                                .Name("DbContextTypeName")
                                .OptionLabel("-- Select DbContext --")
                                .DataTextField("FullName")
                                .DataValueField("FullName")
                                .Enable(false)
                            )
                            <span asp-validation-for="DbContextTypeName" class="text-danger"></span>
                        </div>
                    </div>

                    <div id="discoveryResults" class="mb-3" style="display: none;">
                        <div class="alert alert-info">
                            <strong>Discovered DbContexts:</strong>
                            <ul id="discoveryList" class="mb-0 mt-2"></ul>
                        </div>
                    </div>

                    <div class="row mb-3">
                        <div class="col-md-12">
                            <label asp-for="ConnectionString" class="form-label"></label>
                            <input asp-for="ConnectionString" class="form-control"
                                placeholder="Connection string for DbContext instantiation (optional)" />
                            <small class="text-muted">Required if DbContext doesn't have a parameterless constructor</small>
                            <span asp-validation-for="ConnectionString" class="text-danger"></span>
                        </div>
                    </div>
                </div>

                <hr />

                <h5>Target Database (for comparison)</h5>

                <div class="row mb-3">
                    <div class="col-md-6">
                        <label asp-for="TargetServer" class="form-label"></label>
                        <input asp-for="TargetServer" class="form-control" placeholder="e.g., localhost" />
                        <span asp-validation-for="TargetServer" class="text-danger"></span>
                    </div>
                    <div class="col-md-6">
                        <label asp-for="TargetDatabase" class="form-label"></label>
                        <input asp-for="TargetDatabase" class="form-control" placeholder="e.g., AdventureWorks" />
                        <span asp-validation-for="TargetDatabase" class="text-danger"></span>
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-12">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="2"
                            placeholder="Optional description"></textarea>
                        <span asp-validation-for="Description" class="text-danger"></span>
                    </div>
                </div>

                <div class="d-flex justify-content-between">
                    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
                    <button type="submit" class="btn btn-primary">Create Source</button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        function onProviderTypeChange(e) {
            var value = e.sender.value();
            if (value === 'DynamicDll') {
                $('#dynamicDllFields').show();
            } else {
                $('#dynamicDllFields').hide();
            }
        }

        function discoverDbContexts() {
            var assemblyPath = $('#AssemblyPath').val();
            if (!assemblyPath) {
                kendo.alert('Please enter an assembly path first.');
                return;
            }

            kendo.ui.progress($('body'), true);

            $.ajax({
                url: '@Url.Action("DiscoverDbContexts")',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ assemblyPath: assemblyPath }),
                success: function(result) {
                    if (result.success) {
                        var dropdown = $('#DbContextTypeName').data('kendoDropDownList');

                        if (result.dbContexts.length > 0) {
                            dropdown.setDataSource(result.dbContexts);
                            dropdown.enable(true);

                            // Show discovery results
                            var list = $('#discoveryList');
                            list.empty();
                            result.dbContexts.forEach(function(ctx) {
                                list.append('<li><strong>' + ctx.Name + '</strong> - ' +
                                    ctx.EntityCount + ' entities ' +
                                    (ctx.HasParameterlessConstructor ? '(parameterless ctor)' : '(requires options)') +
                                    '</li>');
                            });
                            $('#discoveryResults').show();
                        } else {
                            dropdown.setDataSource([]);
                            dropdown.enable(false);
                            $('#discoveryResults').hide();
                            kendo.alert('No DbContext types found in the assembly.');
                        }
                    } else {
                        kendo.alert('Discovery failed: ' + result.message);
                    }
                },
                error: function() {
                    kendo.alert('Error discovering DbContexts');
                },
                complete: function() {
                    kendo.ui.progress($('body'), false);
                }
            });
        }

        $(document).ready(function() {
            onProviderTypeChange({ sender: { value: function() { return '@Model.ProviderType'; } } });
        });
    </script>
}
```

### Step 4.4: Create EfModelSources Edit View

**New File:** `src/NetSqlDataDicV2.Web/Views/EfModelSources/Edit.cshtml`

```html
@model NetSqlDataDicV2.Web.Models.ViewModels.EfModelSourceEditViewModel
@{
    ViewData["Title"] = "Edit EF Model Source";
    var providerTypes = ViewBag.ProviderTypes as List<string> ?? new List<string>();
}

<div class="container py-4">
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index">EF Model Sources</a></li>
            <li class="breadcrumb-item active">Edit</li>
        </ol>
    </nav>

    <div class="card">
        <div class="card-header d-flex justify-content-between align-items-center">
            <h4 class="mb-0">Edit EF Model Source</h4>
            <div>
                <span class="badge @(Model.IsActive ? "bg-success" : "bg-warning")">
                    @(Model.IsActive ? "Active" : "Inactive")
                </span>
            </div>
        </div>
        <div class="card-body">
            <form asp-action="Edit" method="post">
                <input type="hidden" asp-for="Id" />
                <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>

                <div class="row mb-3">
                    <div class="col-md-6">
                        <label asp-for="Name" class="form-label"></label>
                        <input asp-for="Name" class="form-control" />
                        <span asp-validation-for="Name" class="text-danger"></span>
                    </div>
                    <div class="col-md-3">
                        <label asp-for="ProviderType" class="form-label"></label>
                        @(Html.Kendo().DropDownListFor(m => m.ProviderType)
                            .DataSource(providerTypes)
                            .Events(e => e.Change("onProviderTypeChange"))
                        )
                    </div>
                    <div class="col-md-3">
                        <label asp-for="IsActive" class="form-label d-block"></label>
                        @(Html.Kendo().SwitchFor(m => m.IsActive))
                    </div>
                </div>

                <div id="dynamicDllFields">
                    <div class="row mb-3">
                        <div class="col-md-8">
                            <label asp-for="AssemblyPath" class="form-label"></label>
                            <div class="input-group">
                                <input asp-for="AssemblyPath" class="form-control" />
                                <button type="button" class="btn btn-outline-secondary" onclick="discoverDbContexts()">
                                    <i class="fas fa-search"></i> Discover
                                </button>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <label asp-for="DbContextTypeName" class="form-label"></label>
                            <input asp-for="DbContextTypeName" class="form-control" />
                        </div>
                    </div>

                    <div class="row mb-3">
                        <div class="col-md-12">
                            <label asp-for="ConnectionString" class="form-label"></label>
                            <input asp-for="ConnectionString" class="form-control" />
                            @if (Model.HasConnectionString)
                            {
                                <small class="text-muted">A connection string is already stored. Leave blank to keep existing.</small>
                            }
                        </div>
                    </div>
                </div>

                <hr />

                <h5>Target Database</h5>

                <div class="row mb-3">
                    <div class="col-md-6">
                        <label asp-for="TargetServer" class="form-label"></label>
                        <input asp-for="TargetServer" class="form-control" />
                    </div>
                    <div class="col-md-6">
                        <label asp-for="TargetDatabase" class="form-label"></label>
                        <input asp-for="TargetDatabase" class="form-control" />
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-12">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="2"></textarea>
                    </div>
                </div>

                <div class="d-flex justify-content-between">
                    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
                    <div>
                        <button type="button" class="btn btn-info" onclick="validateSource()">
                            <i class="fas fa-check-circle"></i> Validate
                        </button>
                        <button type="submit" class="btn btn-primary">Save Changes</button>
                    </div>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        function onProviderTypeChange(e) {
            var value = e.sender.value();
            $('#dynamicDllFields').toggle(value === 'DynamicDll');
        }

        function discoverDbContexts() {
            var assemblyPath = $('#AssemblyPath').val();
            if (!assemblyPath) {
                kendo.alert('Please enter an assembly path first.');
                return;
            }

            kendo.ui.progress($('body'), true);

            $.ajax({
                url: '@Url.Action("DiscoverDbContexts")',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ assemblyPath: assemblyPath }),
                success: function(result) {
                    if (result.success && result.dbContexts.length > 0) {
                        var names = result.dbContexts.map(c => c.FullName).join('\n');
                        kendo.alert('Found DbContexts:\n' + names);
                    } else {
                        kendo.alert(result.message || 'No DbContext types found.');
                    }
                },
                complete: function() {
                    kendo.ui.progress($('body'), false);
                }
            });
        }

        function validateSource() {
            kendo.ui.progress($('body'), true);

            $.post('@Url.Action("Validate")', { id: @Model.Id })
                .done(function(result) {
                    if (result.isValid) {
                        kendo.alert('Validation successful: ' + result.message);
                    } else {
                        kendo.alert('Validation failed: ' + result.errorMessage);
                    }
                })
                .always(function() {
                    kendo.ui.progress($('body'), false);
                });
        }

        $(document).ready(function() {
            onProviderTypeChange({ sender: { value: function() { return '@Model.ProviderType'; } } });
        });
    </script>
}
```

### Step 4.5: Modify Comparison Controller

**Modified File:** `src/NetSqlDataDicV2.Web/Controllers/ComparisonController.cs`

Add source selection support:

```csharp
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class ComparisonController : Controller
{
    private readonly IComparisonService _comparisonService;
    private readonly IEfModelSourceService _sourceService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        IComparisonService comparisonService,
        IEfModelSourceService sourceService,
        IConfiguration configuration,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _sourceService = sourceService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? sourceId, CancellationToken ct)
    {
        // Get all active sources for dropdown
        var sources = await _sourceService.GetActiveAsync(ct);
        ViewBag.EfModelSources = sources;
        ViewBag.SelectedSourceId = sourceId;

        // Default values from configuration (backward compatible)
        ViewBag.Server = _configuration["SourceDatabase:Server"] ?? "localhost";
        ViewBag.Database = _configuration["SourceDatabase:Database"] ?? "";

        // If sourceId provided, use that source's target
        if (sourceId.HasValue)
        {
            var source = await _sourceService.GetByIdAsync(sourceId.Value, ct);
            if (source != null)
            {
                ViewBag.Server = source.TargetServer;
                ViewBag.Database = source.TargetDatabase;
                ViewBag.SourceName = source.Name;
            }
        }

        return View();
    }

    /// <summary>
    /// Compare using direct reference (backward compatible).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Compare(
        string server,
        string database,
        CancellationToken ct)
    {
        try
        {
            var result = await _comparisonService.CompareAsync(server, database, ct);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed for {Server}/{Database}", server, database);
            return Json(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Compare using configured source.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CompareSource(int sourceId, CancellationToken ct)
    {
        try
        {
            var result = await _comparisonService.CompareAsync(sourceId, ct);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed for source {SourceId}", sourceId);
            return Json(new { error = ex.Message });
        }
    }

    // ... existing CompareGrid method remains for backward compatibility ...
}
```

### Step 4.6: Modify Comparison View

**Modified File:** `src/NetSqlDataDicV2.Web/Views/Comparison/Index.cshtml`

Add source selection dropdown (at the top of the comparison panel):

```html
@* Add to existing view - source selection section *@

<div class="card mb-4">
    <div class="card-header">
        <h5 class="mb-0">EF Model Source</h5>
    </div>
    <div class="card-body">
        <div class="row align-items-end">
            <div class="col-md-4">
                <label class="form-label">Select Source</label>
                @(Html.Kendo().DropDownList()
                    .Name("sourceSelector")
                    .DataTextField("Name")
                    .DataValueField("Id")
                    .OptionLabel("-- Use Direct Reference --")
                    .BindTo(ViewBag.EfModelSources as IEnumerable<dynamic>)
                    .Value(ViewBag.SelectedSourceId?.ToString())
                    .Events(e => e.Change("onSourceChange"))
                )
            </div>
            <div class="col-md-3">
                <label class="form-label">Target Server</label>
                <input type="text" id="targetServer" class="form-control"
                    value="@ViewBag.Server" readonly />
            </div>
            <div class="col-md-3">
                <label class="form-label">Target Database</label>
                <input type="text" id="targetDatabase" class="form-control"
                    value="@ViewBag.Database" readonly />
            </div>
            <div class="col-md-2">
                <button type="button" class="btn btn-primary w-100" onclick="runComparison()">
                    <i class="fas fa-sync"></i> Compare
                </button>
            </div>
        </div>

        @if (ViewBag.SourceName != null)
        {
            <div class="mt-2">
                <small class="text-muted">Using source: <strong>@ViewBag.SourceName</strong></small>
            </div>
        }
    </div>
</div>

@* Add JavaScript for source selection *@
<script>
    function onSourceChange(e) {
        var sourceId = e.sender.value();
        if (sourceId) {
            // Get source details and update fields
            var dataItem = e.sender.dataItem();
            if (dataItem) {
                $('#targetServer').val(dataItem.TargetServer);
                $('#targetDatabase').val(dataItem.TargetDatabase);
            }
        } else {
            // Reset to default configuration values
            $('#targetServer').val('@ViewBag.Server');
            $('#targetDatabase').val('@ViewBag.Database');
        }
    }

    function runComparison() {
        var sourceId = $('#sourceSelector').val();

        kendo.ui.progress($('#comparisonGrid'), true);

        var url, data;
        if (sourceId) {
            url = '@Url.Action("CompareSource")';
            data = { sourceId: sourceId };
        } else {
            url = '@Url.Action("Compare")';
            data = {
                server: $('#targetServer').val(),
                database: $('#targetDatabase').val()
            };
        }

        $.post(url, data)
            .done(function(result) {
                if (result.error) {
                    kendo.alert('Comparison failed: ' + result.error);
                } else {
                    // Update grid with results
                    var grid = $('#comparisonGrid').data('kendoGrid');
                    grid.dataSource.data(result.items);

                    // Update summary
                    updateSummary(result);
                }
            })
            .fail(function() {
                kendo.alert('Error running comparison');
            })
            .always(function() {
                kendo.ui.progress($('#comparisonGrid'), false);
            });
    }

    function updateSummary(result) {
        $('#totalCount').text(result.totalCount);
        $('#matchCount').text(result.matchCount);
        $('#missingInEfCount').text(result.missingInEfModelCount);
        $('#missingInDbCount').text(result.missingInDatabaseCount);
        $('#typeMismatchCount').text(result.typeMismatchCount);
    }
</script>
```

### Step 4.7: Add Navigation Link

**Modified File:** `src/NetSqlDataDicV2.Web/Views/Shared/_Layout.cshtml`

Add navigation link to EF Model Sources:

```html
<li class="nav-item">
    <a class="nav-link" asp-controller="EfModelSources" asp-action="Index">
        <i class="fas fa-plug"></i> EF Sources
    </a>
</li>
```

## Testing Checklist

- [ ] EfModelSources Index page loads and displays grid
- [ ] Create form shows/hides DynamicDll fields based on provider type
- [ ] Discover button finds DbContexts in valid DLL
- [ ] Create saves new source to database
- [ ] Edit form loads existing source data
- [ ] Edit saves changes correctly
- [ ] Delete removes source from database
- [ ] Validate button tests source configuration
- [ ] Toggle active works from grid
- [ ] Compare button navigates to comparison with source
- [ ] Comparison page shows source dropdown
- [ ] Source selection updates target server/database fields
- [ ] Comparison works with selected source
- [ ] Comparison works with direct reference (backward compat)
- [ ] Navigation link appears in layout

## Files Created

| File | Purpose |
|------|---------|
| `Controllers/EfModelSourcesController.cs` | CRUD controller |
| `Views/EfModelSources/Index.cshtml` | List page with grid |
| `Views/EfModelSources/Create.cshtml` | Create form |
| `Views/EfModelSources/Edit.cshtml` | Edit form |

## Files Modified

| File | Change Type |
|------|-------------|
| `Controllers/ComparisonController.cs` | Added source selection |
| `Views/Comparison/Index.cshtml` | Added source dropdown |
| `Views/Shared/_Layout.cshtml` | Added nav link |

## Next Phase

Phase 5 will add security features including DLL validation, path restrictions, and connection string encryption.
