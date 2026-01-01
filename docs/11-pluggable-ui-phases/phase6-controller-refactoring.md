# Phase 6: Controller Refactoring

## Status: Pending

## Overview

Refactor all controllers to support Area-based routing with proper attributes and updated route references.

## Goals

1. Add `[Area("DataDictionary")]` attribute to all controllers
2. Update route attributes for Area compatibility
3. Rename controllers where needed
4. Update all action return URLs
5. Update service injections with new namespaces

## Controller Updates

### 1. HomeController

```csharp
using DataDictionary.AspNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataDictionaryOptions _options;

    public HomeController(
        ILogger<HomeController> logger,
        DataDictionaryOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public IActionResult Index()
    {
        ViewBag.EnableSync = _options.EnableSyncFeature;
        ViewBag.EnableComparison = _options.EnableComparisonFeature;
        ViewBag.EnableSources = _options.EnableEfModelSources;
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
using DataDictionary.AspNetCore.Core.Services;
using DataDictionary.AspNetCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class DictionaryController : Controller
{
    private readonly IDataDictionaryService _service;
    private readonly ILogger<DictionaryController> _logger;

    public DictionaryController(
        IDataDictionaryService service,
        ILogger<DictionaryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Servers = await _service.GetDistinctServersAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Read([FromBody] PaginationRequest request)
    {
        // ... existing implementation
    }

    public async Task<IActionResult> Details(int id)
    {
        var details = await _service.GetDetailsAsync(id);
        if (details == null)
            return NotFound();
        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] DataElementUpdateViewModel model)
    {
        // ... existing implementation
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

Update all AJAX URLs in views:

```javascript
// Before
url: '/api/DataDictionary/Read'

// After
url: '@Url.Action("Read", "Dictionary", new { area = "DataDictionary" })'
// Or use data attributes for JavaScript
```

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
