# Pluggable UI Package Conversion

## Overview

Convert the Data Dictionary solution into a **Razor Class Library (RCL)** NuGet package that provides full UI functionality out-of-the-box for any ASP.NET Core MVC application.

## Architecture Decision: Razor Class Library (RCL)

**Why RCL?**
- Native ASP.NET Core support for packaging Views, Controllers, Static Assets
- Views can be overridden by consuming application
- Static assets served via `/_content/{PackageName}/`
- Tag Helpers and View Components supported
- Works with Areas for route isolation

## User Decisions

| Decision | Choice |
|----------|--------|
| **Route Prefix** | `/tools/datadictionary` |
| **Authentication** | None - consumer handles their own auth policies |
| **Migration Strategy** | Auto-migrate on startup |
| **Package Naming** | `DataDictionary.AspNetCore` (with `DataDictionary.AspNetCore.Core` for core library) |

## Package Structure

```
DataDictionary.AspNetCore/
├── Areas/
│   └── DataDictionary/
│       ├── Controllers/
│       ├── Views/
│       └── wwwroot/
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Configuration/
│   └── DataDictionaryOptions.cs
└── DataDictionary.AspNetCore.csproj (Sdk: Microsoft.NET.Sdk.Razor)
```

## Final Solution Structure

```
NetSqlDataDicV2/
├── src/
│   ├── DataDictionary.AspNetCore.Core/    # Entities, Services, DbContext
│   │   ├── Data/
│   │   ├── Entities/
│   │   ├── Services/
│   │   ├── Exceptions/
│   │   ├── Configuration/
│   │   └── DataDictionary.AspNetCore.Core.csproj
│   │
│   ├── DataDictionary.AspNetCore/         # Razor Class Library (NuGet Package)
│   │   ├── Areas/DataDictionary/
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   ├── wwwroot/
│   │   ├── Extensions/
│   │   ├── Configuration/
│   │   └── DataDictionary.AspNetCore.csproj
│   │
│   └── NetSqlDataDicV2.Web/               # Sample/Demo Application
│       ├── Program.cs                      # Shows package integration
│       └── NetSqlDataDicV2.Web.csproj
│
├── tests/
│   └── NetSqlDataDicV2.Tests/
│
└── docs/
```

## Phase Status

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 1 | [Create RCL Project Structure](phase1-project-structure.md) | **Complete** | RCL project created |
| 2 | [Extract Core Library](phase2-core-library.md) | **Complete** | Core library extracted |
| 3 | [Create UI Package](phase3-ui-package.md) | **Complete** | Controllers/Views moved to RCL Areas |
| 4 | [Extension Methods](phase4-extension-methods.md) | **Complete** | AddDataDictionary, UseDataDictionary, MapDataDictionary |
| 5 | [View Customization](phase5-view-customization.md) | **Complete** | Layout with CDN deps, _ViewImports configured |
| 6 | [Controller Refactoring](phase6-controller-refactoring.md) | **Complete** | Area attributes, renamed controllers |
| 7 | [Middleware Integration](phase7-middleware.md) | **Complete** | RequestLogging & ExceptionHandling moved to RCL |
| 8 | [Database Migrations](phase8-migrations.md) | **Complete** | Auto-migrate in UseDataDictionary() |
| 9 | [Static Asset Delivery](phase9-static-assets.md) | **Complete** | Assets at /_content/DataDictionary.AspNetCore/ |
| 10 | [Test Project Migration](phase10-test-migration.md) | **Complete** | Tests updated for new namespaces |

**Overall Status:** 10/10 phases complete. Package is fully functional.

## Consumer Quick Start (Target Experience)

```csharp
// 1. Install NuGet package
// dotnet add package DataDictionary.AspNetCore

// 2. Configure in Program.cs
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

// Basic setup - auto-migrates database by default
app.UseDataDictionary();

// Or with optional middleware
app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
    middleware.IncludeStackTraceInErrors = app.Environment.IsDevelopment();
});

app.MapDataDictionary();

// 3. Add connection string in appsettings.json
// "ConnectionStrings": { "DataDictionary": "..." }

// 4. Navigate to /tools/datadictionary
```

## Edge Cases & Considerations

1. **Authentication/Authorization** - Package does NOT enforce auth; consumer handles
2. **Multiple DbContext** - Package DbContext is isolated with separate connection string
3. **Route Conflicts** - Area-based routing prevents conflicts
4. **Hot-Reload** - RCL views support hot-reload in development
5. **Versioning** - Semantic versioning with migration guides
6. **Logging** - Uses `ILogger<T>` throughout; consumer controls log levels
7. **Localization** - Support for resource files and culture formatting
