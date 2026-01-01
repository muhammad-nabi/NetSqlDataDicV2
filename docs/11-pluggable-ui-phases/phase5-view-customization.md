# Phase 5: View Customization Support

## Status: Pending

## Overview

Configure views to support customization by consumers, including layout overrides and integration with host application layouts.

## Goals

1. Create standalone package layout with CDN dependencies
2. Support layout override by consumers
3. Configure ViewImports for proper namespace resolution
4. Support embedding in host application layouts

## Layout Override Strategy

ASP.NET Core RCL views can be overridden by consumers simply by creating a file at the same path in their application. The consumer's file takes precedence.

### Consumer Override Example

```
ConsumerApp/
└── Areas/
    └── DataDictionary/
        └── Views/
            └── Shared/
                └── _Layout.cshtml   (Overrides package layout)
```

## Files to Create/Update

### 1. Package Layout (_Layout.cshtml)

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Data Dictionary</title>

    <!-- Bootstrap CSS (CDN) -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"
          rel="stylesheet"
          integrity="sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN"
          crossorigin="anonymous">

    <!-- DataTables CSS (CDN) -->
    <link href="https://cdn.datatables.net/1.13.7/css/dataTables.bootstrap5.min.css" rel="stylesheet">

    <!-- Package CSS -->
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datadictionary.css" />
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datatables-custom.css" />

    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" asp-area="DataDictionary" asp-controller="Home" asp-action="Index">
                Data Dictionary
            </a>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                <ul class="navbar-nav">
                    <li class="nav-item">
                        <a class="nav-link" asp-area="DataDictionary" asp-controller="Dictionary" asp-action="Index">
                            Dictionary
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-area="DataDictionary" asp-controller="Sync" asp-action="Index">
                            Sync
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-area="DataDictionary" asp-controller="Comparison" asp-action="Index">
                            Comparison
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-area="DataDictionary" asp-controller="Sources" asp-action="Index">
                            EF Sources
                        </a>
                    </li>
                </ul>
            </div>
        </div>
    </nav>

    <main class="container mt-4">
        @RenderBody()
    </main>

    <footer class="container mt-4 mb-3">
        <hr />
        <p class="text-muted">&copy; @DateTime.Now.Year Data Dictionary</p>
    </footer>

    <!-- jQuery (CDN) -->
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"
            integrity="sha256-/JqT3SQfawRcv/BIHPThkBvs0OEvtFFmqPF/lYI/Cxo="
            crossorigin="anonymous"></script>

    <!-- Bootstrap JS (CDN) -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"
            integrity="sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL"
            crossorigin="anonymous"></script>

    <!-- DataTables JS (CDN) -->
    <script src="https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js"></script>
    <script src="https://cdn.datatables.net/1.13.7/js/dataTables.bootstrap5.min.js"></script>

    <!-- Package JS -->
    <script src="~/_content/DataDictionary.AspNetCore/js/datatables-helpers.js"></script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**Location:** `Areas/DataDictionary/Views/Shared/_Layout.cshtml`

### 2. _ViewStart.cshtml

```razor
@{
    Layout = "_Layout";
}
```

**Location:** `Areas/DataDictionary/Views/_ViewStart.cshtml`

### 3. _ViewImports.cshtml

```razor
@using DataDictionary.AspNetCore
@using DataDictionary.AspNetCore.Configuration
@using DataDictionary.AspNetCore.Models.ViewModels
@using DataDictionary.AspNetCore.Core.Entities
@using DataDictionary.AspNetCore.Core.Models
@using DataDictionary.AspNetCore.Areas.DataDictionary.Controllers

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, DataDictionary.AspNetCore
```

**Location:** `Areas/DataDictionary/Views/_ViewImports.cshtml`

## Layout Integration Options

### Option A: Standalone (Default)

Package provides complete layout. Consumer does nothing.

### Option B: Use Host Application Layout

Consumer creates `_ViewStart.cshtml` override:

```razor
@* Areas/DataDictionary/Views/_ViewStart.cshtml *@
@{
    Layout = "~/Views/Shared/_Layout.cshtml";  // Host app layout
}
```

**Requirements for host layout:**
- Include Bootstrap 5.3+ CSS/JS
- Include DataTables 1.13+ CSS/JS
- Include jQuery 3.7+
- Include package CSS: `~/_content/DataDictionary.AspNetCore/css/datadictionary.css`
- Include package JS: `~/_content/DataDictionary.AspNetCore/js/datatables-helpers.js`

### Option C: Partial Layout Override

Consumer overrides just the layout:

```html
@* Areas/DataDictionary/Views/Shared/_Layout.cshtml *@
@{
    Layout = "~/Views/Shared/_Layout.cshtml";  // Inherit from host
}

@section Styles {
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datadictionary.css" />
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datatables-custom.css" />
}

@RenderBody()

@section Scripts {
    <script src="~/_content/DataDictionary.AspNetCore/js/datatables-helpers.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
}
```

## Navigation Updates

All views must use area-aware tag helpers:

```html
<!-- Before -->
<a asp-controller="DataDictionary" asp-action="Index">Dictionary</a>

<!-- After -->
<a asp-area="DataDictionary" asp-controller="Dictionary" asp-action="Index">Dictionary</a>
```

## View Updates Required

Update all navigation links in views to include `asp-area="DataDictionary"`:

| View | Links to Update |
|------|-----------------|
| `_Layout.cshtml` | All navbar links |
| `Dictionary/Index.cshtml` | Details links, filter forms |
| `Dictionary/Details.cshtml` | Back links, related links |
| `Sync/Index.cshtml` | Results links, action buttons |
| `Comparison/Index.cshtml` | Source selection, details links |
| `Sources/Index.cshtml` | CRUD action links |

## Verification Steps

1. Package layout renders correctly standalone
2. Consumer can override layout
3. Consumer can use host layout
4. All navigation works with area routing
5. Static assets load from `/_content/`

## Dependencies

- Phase 1-4 complete

## Next Phase

[Phase 6: Controller Refactoring](phase6-controller-refactoring.md)
