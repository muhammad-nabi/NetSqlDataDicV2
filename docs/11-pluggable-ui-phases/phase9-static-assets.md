# Phase 9: Static Asset Delivery

## Status: Pending

## Overview

Configure static assets (CSS, JS) to be embedded in the RCL package and served correctly to consumers.

## Goals

1. Configure project for static web assets
2. Move and rename CSS/JS files
3. Update all view references to use package content path
4. Verify assets load correctly in consumer applications

## Project Configuration

### DataDictionary.AspNetCore.csproj Updates

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>

    <!-- Enable static web assets -->
    <StaticWebAssetBasePath>/</StaticWebAssetBasePath>

    <!-- Generate manifest for embedded files -->
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>

    <!-- Package metadata -->
    <PackageId>DataDictionary.AspNetCore</PackageId>
    <Version>1.0.0</Version>
    <Description>Pluggable Data Dictionary UI for ASP.NET Core MVC</Description>
    <Authors>Your Name</Authors>
    <PackageTags>aspnetcore;mvc;data-dictionary;database;schema</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DataDictionary.AspNetCore.Core\DataDictionary.AspNetCore.Core.csproj" />
  </ItemGroup>

  <!-- Include package for embedded file manifest -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="9.0.0" />
  </ItemGroup>
</Project>
```

## Static Asset Structure

```
DataDictionary.AspNetCore/
└── wwwroot/
    ├── css/
    │   ├── datadictionary.css          (renamed from site.css)
    │   └── datatables-custom.css
    └── js/
        └── datatables-helpers.js
```

## File Movements & Renames

| Source | Destination | Notes |
|--------|-------------|-------|
| `wwwroot/css/site.css` | `wwwroot/css/datadictionary.css` | Rename |
| `wwwroot/css/datatables-custom.css` | `wwwroot/css/datatables-custom.css` | Keep as-is |
| `wwwroot/js/site.js` | `wwwroot/js/datadictionary-core.js` | Rename, contains global JS |
| `wwwroot/js/datatables-helpers.js` | `wwwroot/js/datatables-helpers.js` | Keep as-is |
| `Views/Shared/_Layout.cshtml.css` | Merge into `wwwroot/css/datadictionary.css` | CSS isolation file |
| `wwwroot/favicon.ico` | `wwwroot/favicon.ico` | Optional - consumers may provide own |

**Note:** `wwwroot/lib/` folder (Bootstrap, jQuery fallbacks) is NOT included - CDN is preferred.

## Static Asset URLs

Assets are served at: `/_content/DataDictionary.AspNetCore/{path}`

| File | URL |
|------|-----|
| `wwwroot/css/datadictionary.css` | `/_content/DataDictionary.AspNetCore/css/datadictionary.css` |
| `wwwroot/css/datatables-custom.css` | `/_content/DataDictionary.AspNetCore/css/datatables-custom.css` |
| `wwwroot/js/datadictionary-core.js` | `/_content/DataDictionary.AspNetCore/js/datadictionary-core.js` |
| `wwwroot/js/datatables-helpers.js` | `/_content/DataDictionary.AspNetCore/js/datatables-helpers.js` |

## View Reference Updates

### _Layout.cshtml

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

    <!-- Package CSS (from RCL) -->
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datadictionary.css" />
    <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datatables-custom.css" />

    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
    <!-- ... body content ... -->

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

    <!-- Package JS (from RCL) -->
    <script src="~/_content/DataDictionary.AspNetCore/js/datadictionary-core.js"></script>
    <script src="~/_content/DataDictionary.AspNetCore/js/datatables-helpers.js"></script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

## CDN Dependencies (Not Bundled)

The package relies on CDN-hosted libraries:

| Library | Version | CDN URL |
|---------|---------|---------|
| Bootstrap CSS | 5.3.2 | `cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css` |
| Bootstrap JS | 5.3.2 | `cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js` |
| jQuery | 3.7.1 | `code.jquery.com/jquery-3.7.1.min.js` |
| DataTables CSS | 1.13.7 | `cdn.datatables.net/1.13.7/css/dataTables.bootstrap5.min.css` |
| DataTables JS | 1.13.7 | `cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js` |
| DataTables Bootstrap | 1.13.7 | `cdn.datatables.net/1.13.7/js/dataTables.bootstrap5.min.js` |

## Consumer Requirements

If consumer overrides layout, they must include:

1. **Bootstrap 5.3+** - CSS and JS bundle
2. **jQuery 3.7+** - Required for DataTables
3. **DataTables 1.13+** - CSS and JS with Bootstrap integration
4. **Package Assets:**
   ```html
   <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datadictionary.css" />
   <link rel="stylesheet" href="~/_content/DataDictionary.AspNetCore/css/datatables-custom.css" />
   <script src="~/_content/DataDictionary.AspNetCore/js/datatables-helpers.js"></script>
   ```

## CSS Content (datadictionary.css)

```css
/* Data Dictionary Package Styles */

/* Table styling */
.datadictionary-grid {
    font-size: 0.875rem;
}

/* Status badges */
.badge-match { background-color: #198754; }
.badge-mismatch { background-color: #dc3545; }
.badge-missing { background-color: #ffc107; color: #000; }
.badge-constraint { background-color: #6f42c1; }

/* Details page */
.audit-history-card { max-height: 400px; overflow-y: auto; }
.notes-card { max-height: 300px; overflow-y: auto; }

/* Form styling */
.form-label-required::after {
    content: " *";
    color: #dc3545;
}

/* Loading overlay */
.loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(255, 255, 255, 0.8);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}
```

## Verification Steps

1. Run `dotnet build` - assets included in output
2. Run sample app - assets load at `/_content/` path
3. Check browser DevTools - no 404 errors for CSS/JS
4. Verify styles apply correctly
5. Verify DataTables helpers work

## Consumer Testing

Create a simple consumer app to verify:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();  // Required for RCL static assets
app.UseRouting();
app.UseDataDictionary();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapDataDictionary();

app.Run();
```

## Troubleshooting

### Assets Not Loading (404)

1. Ensure `app.UseStaticFiles()` is called
2. Check path is `/_content/DataDictionary.AspNetCore/...`
3. Verify RCL project reference in consumer

### Styles Not Applying

1. Check CSS load order (Bootstrap before package CSS)
2. Verify no CSS caching issues (clear browser cache)
3. Check for CSS specificity conflicts

## Dependencies

- Phase 1-8 complete
- All views updated with area routing

## Completion

After Phase 9, the package is ready for:
1. Local testing in sample app
2. NuGet package creation
3. Publishing to NuGet feed
