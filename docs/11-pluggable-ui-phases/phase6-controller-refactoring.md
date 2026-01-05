# Phase 6: Controller Refactoring

## Status: Complete

## Overview

Refactor all controllers to support Area-based routing with proper attributes and updated route references.

## Goals

1. Create base controller with ViewBag.RoutePrefix injection
2. All controllers inherit from `DataDictionaryControllerBase`
3. Rename controllers where needed
4. Update all action return URLs
5. Update service injections with new namespaces

## Base Controller

All controllers inherit from `DataDictionaryControllerBase` which injects `ViewBag.RoutePrefix`:

```csharp
[Area("DataDictionary")]
public abstract class DataDictionaryControllerBase : Controller
{
    private readonly DataDictionaryOptions _options;

    protected DataDictionaryControllerBase(DataDictionaryOptions options)
    {
        _options = options;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var prefix = _options.RoutePrefix.TrimStart('/').TrimEnd('/');
        ViewBag.RoutePrefix = "/" + prefix;
        base.OnActionExecuting(context);
    }
}
```

## Controller Updates

### 1. HomeController

```csharp
using DataDictionary.AspNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

public class HomeController : DataDictionaryControllerBase
{
    public HomeController(DataDictionaryOptions options) : base(options)
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? message = null, string? correlationId = null)
    {
        ViewBag.Message = message;
        ViewBag.CorrelationId = correlationId;
        return View();
    }
}
```

### 2. DictionaryController (renamed from DataDictionaryController)

```csharp
using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

public class DictionaryController : DataDictionaryControllerBase
{
    private readonly IDataDictionaryService _service;
    private readonly ILogger<DictionaryController> _logger;

    public DictionaryController(
        DataDictionaryOptions options,
        IDataDictionaryService service,
        ILogger<DictionaryController> logger) : base(options)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Servers = await _service.GetDistinctServersAsync();
        return View();
    }

    // ... other actions
}
```

### 3. SyncController

```csharp
using DataDictionary.AspNetCore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class SyncController : Controller
{
    private readonly IDatabaseSyncService _syncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IDatabaseSyncService syncService,
        IConfiguration configuration,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _configuration = configuration;
        _logger = logger;
    }

    // ... actions with updated namespaces
}
```

### 4. ComparisonController

```csharp
using DataDictionary.AspNetCore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class ComparisonController : Controller
{
    private readonly IComparisonService _comparisonService;
    private readonly IEfModelSourceService _sourceService;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        IComparisonService comparisonService,
        IEfModelSourceService sourceService,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _sourceService = sourceService;
        _logger = logger;
    }

    // ... actions with updated namespaces
}
```

### 5. SourcesController (renamed from EfModelSourcesController)

```csharp
using DataDictionary.AspNetCore.Core.Services;
using DataDictionary.AspNetCore.Core.Services.DbContextProviders;
using DataDictionary.AspNetCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class SourcesController : Controller
{
    private readonly IEfModelSourceService _sourceService;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<SourcesController> _logger;

    public SourcesController(
        IEfModelSourceService sourceService,
        IDbContextProviderFactory providerFactory,
        ILogger<SourcesController> logger)
    {
        _sourceService = sourceService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    // ... actions with updated namespaces
}
```

## Route Updates

### Controller Renames Impact

| Old Route | New Route |
|-----------|-----------|
| `/DataDictionary/Index` | `/tools/datadictionary/Dictionary/Index` |
| `/DataDictionary/Details/1` | `/tools/datadictionary/Dictionary/Details/1` |
| `/EfModelSources/Index` | `/tools/datadictionary/Sources/Index` |
| `/EfModelSources/Create` | `/tools/datadictionary/Sources/Create` |
| `/EfModelSources/Edit/1` | `/tools/datadictionary/Sources/Edit/1` |

### API Route Updates

| Old API Route | New API Route |
|---------------|---------------|
| `/api/DataDictionary/Read` | `/api/tools/datadictionary/Dictionary/Read` |
| `/api/Comparison/CompareSource` | `/api/tools/datadictionary/Comparison/CompareSource` |
| `/api/EfModelSources/GetSources` | `/api/tools/datadictionary/Sources/GetSources` |

## Redirect and URL Updates

Update all redirects in controllers:

```csharp
// Before
return RedirectToAction("Index", "DataDictionary");

// After
return RedirectToAction("Index", "Dictionary", new { area = "DataDictionary" });
```

Update TempData redirects:

```csharp
// Before
TempData["SuccessMessage"] = "Source created successfully.";
return RedirectToAction("Index");

// After (same, but ensure area routing works)
TempData["SuccessMessage"] = "Source created successfully.";
return RedirectToAction("Index");  // Area is implicit from controller
```

## JavaScript AJAX URL Updates

All AJAX URLs use `ViewBag.RoutePrefix` for consistency with navigation:

```javascript
// Use ViewBag.RoutePrefix for all AJAX URLs
$.ajax({
    url: '@ViewBag.RoutePrefix/Dictionary/Read',
    type: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(requestData),
    success: function(result) { ... }
});

// Form actions also use ViewBag.RoutePrefix
<form action="@ViewBag.RoutePrefix/Sources/Create" method="post">
```

This ensures AJAX calls honor the configured `DataDictionary:RoutePrefix`.

## Verification Steps

1. All controllers compile with Area attribute
2. All routes resolve correctly
3. All redirects work
4. All AJAX calls work
5. All links navigate correctly

## Dependencies

- Phase 1-5 complete

## Next Phase

[Phase 7: Middleware Integration](phase7-middleware.md)
