# Phase 3: Create UI Package (RCL)

## Status: Pending

## Overview

Move controllers, views, and static assets to the Razor Class Library with proper Area structure.

## Goals

1. Set up Areas/DataDictionary folder structure
2. Move and rename controllers
3. Move views to Area structure
4. Move static assets to wwwroot
5. Update namespaces and references

## Target Structure

```
DataDictionary.AspNetCore/
├── Areas/
│   └── DataDictionary/
│       ├── Controllers/
│       │   ├── HomeController.cs
│       │   ├── DictionaryController.cs      (renamed)
│       │   ├── SyncController.cs
│       │   ├── ComparisonController.cs
│       │   └── SourcesController.cs         (renamed)
│       └── Views/
│           ├── _ViewStart.cshtml
│           ├── _ViewImports.cshtml
│           ├── Shared/
│           │   ├── _Layout.cshtml
│           │   └── Error.cshtml
│           ├── Home/
│           │   └── Index.cshtml
│           ├── Dictionary/
│           │   ├── Index.cshtml
│           │   ├── Details.cshtml
│           │   └── Deleted.cshtml
│           ├── Sync/
│           │   ├── Index.cshtml
│           │   └── Results.cshtml
│           ├── Comparison/
│           │   └── Index.cshtml
│           └── Sources/
│               ├── Index.cshtml
│               ├── Create.cshtml
│               └── Edit.cshtml
├── wwwroot/
│   ├── css/
│   │   ├── datadictionary.css
│   │   └── datatables-custom.css
│   └── js/
│       └── datatables-helpers.js
└── DataDictionary.AspNetCore.csproj
```

## Controller Movements & Renames

| Source | Destination | Rename |
|--------|-------------|--------|
| `Controllers/HomeController.cs` | `Areas/DataDictionary/Controllers/HomeController.cs` | No |
| `Controllers/DataDictionaryController.cs` | `Areas/DataDictionary/Controllers/DictionaryController.cs` | **Yes** |
| `Controllers/SyncController.cs` | `Areas/DataDictionary/Controllers/SyncController.cs` | No |
| `Controllers/ComparisonController.cs` | `Areas/DataDictionary/Controllers/ComparisonController.cs` | No |
| `Controllers/EfModelSourcesController.cs` | `Areas/DataDictionary/Controllers/SourcesController.cs` | **Yes** |

## View Movements

| Source | Destination |
|--------|-------------|
| `Views/Home/Index.cshtml` | `Areas/DataDictionary/Views/Home/Index.cshtml` |
| `Views/DataDictionary/Index.cshtml` | `Areas/DataDictionary/Views/Dictionary/Index.cshtml` |
| `Views/DataDictionary/Details.cshtml` | `Areas/DataDictionary/Views/Dictionary/Details.cshtml` |
| `Views/DataDictionary/Deleted.cshtml` | `Areas/DataDictionary/Views/Dictionary/Deleted.cshtml` |
| `Views/Sync/Index.cshtml` | `Areas/DataDictionary/Views/Sync/Index.cshtml` |
| `Views/Sync/Results.cshtml` | `Areas/DataDictionary/Views/Sync/Results.cshtml` |
| `Views/Comparison/Index.cshtml` | `Areas/DataDictionary/Views/Comparison/Index.cshtml` |
| `Views/EfModelSources/Index.cshtml` | `Areas/DataDictionary/Views/Sources/Index.cshtml` |
| `Views/EfModelSources/Create.cshtml` | `Areas/DataDictionary/Views/Sources/Create.cshtml` |
| `Views/EfModelSources/Edit.cshtml` | `Areas/DataDictionary/Views/Sources/Edit.cshtml` |
| `Views/Shared/_Layout.cshtml` | `Areas/DataDictionary/Views/Shared/_Layout.cshtml` |
| `Views/Shared/Error.cshtml` | `Areas/DataDictionary/Views/Shared/Error.cshtml` |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | `Areas/DataDictionary/Views/Shared/_ValidationScriptsPartial.cshtml` |

## Static Asset Movements

| Source | Destination |
|--------|-------------|
| `wwwroot/css/site.css` | `wwwroot/css/datadictionary.css` |
| `wwwroot/css/datatables-custom.css` | `wwwroot/css/datatables-custom.css` |
| `wwwroot/js/datatables-helpers.js` | `wwwroot/js/datatables-helpers.js` |

## New Files to Create

### _ViewStart.cshtml

```razor
@{
    Layout = "_Layout";
}
```

**Location:** `Areas/DataDictionary/Views/_ViewStart.cshtml`

### _ViewImports.cshtml

```razor
@using DataDictionary.AspNetCore.Core.Entities
@using DataDictionary.AspNetCore.Core.Models
@using DataDictionary.AspNetCore.Core.Models.ViewModels
@using DataDictionary.AspNetCore.Areas.DataDictionary
@using DataDictionary.AspNetCore.Areas.DataDictionary.Controllers
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, DataDictionary.AspNetCore
```

**Location:** `Areas/DataDictionary/Views/_ViewImports.cshtml`

## ViewModels Location

ViewModels stay with the UI package since they're presentation-specific:

```
DataDictionary.AspNetCore/
└── Models/
    └── ViewModels/
        ├── DataElementViewModel.cs
        ├── DataElementDetailsViewModel.cs
        ├── DataElementUpdateViewModel.cs
        ├── DataElementAuditViewModel.cs
        ├── DataElementNoteViewModel.cs
        ├── AddNoteViewModel.cs
        ├── ComparisonResultViewModel.cs
        ├── EfModelSourceViewModel.cs
        ├── EfModelSourceCreateViewModel.cs
        ├── EfModelSourceEditViewModel.cs
        ├── SyncHistoryViewModel.cs
        ├── SyncResultViewModel.cs
        ├── SyncResultsViewModel.cs
        └── ValidationResultViewModel.cs
```

## Namespace Updates

| Old Namespace | New Namespace |
|---------------|---------------|
| `NetSqlDataDicV2.Web.Controllers` | `DataDictionary.AspNetCore.Areas.DataDictionary.Controllers` |
| `NetSqlDataDicV2.Web.Models.ViewModels` | `DataDictionary.AspNetCore.Models.ViewModels` |

## Verification Steps

1. All controllers in Areas folder
2. All views in Areas folder with correct structure
3. Static assets in wwwroot
4. `dotnet build` succeeds
5. Views compile without errors

## Dependencies

- Phase 1 complete (project structure)
- Phase 2 complete (Core library for entity references)

## Next Phase

[Phase 4: Extension Methods](phase4-extension-methods.md)
