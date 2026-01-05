# Data Dictionary Package Installation & Configuration Guide

## Overview

The Data Dictionary solution provides two NuGet packages for adding database schema documentation and EF Core comparison features to ASP.NET Core applications.

| Package | Purpose |
|---------|---------|
| `DataDictionary.AspNetCore.Core` | Core library (entities, services, DbContext) |
| `DataDictionary.AspNetCore` | UI package (controllers, views, extension methods) |

---

## Step 1: Install NuGet Package

```bash
dotnet add package DataDictionary.AspNetCore
```

> The Core package is installed automatically as a dependency.

---

## Step 2: Configure Connection String

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DataDictionary": "Server=localhost;Database=DataDictionary;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

## Step 3: Register Services in Program.cs

```csharp
using DataDictionary.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Data Dictionary services
builder.Services.AddDataDictionary(builder.Configuration);

// ... your other services
```

---

## Step 4: Configure Middleware Pipeline

```csharp
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/tools/datadictionary/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable Data Dictionary (auto-migrates database by default)
app.UseDataDictionary();

app.UseRouting();
app.UseAuthorization();

// Map Data Dictionary routes
app.MapDataDictionary();

app.Run();
```

---

## Step 5: Access the UI

Navigate to: `https://your-app/tools/datadictionary`

---

## Optional Configuration

### Custom Route Prefix

In `appsettings.json`:

```json
{
  "DataDictionary": {
    "RoutePrefix": "admin/datadictionary"
  }
}
```

### Require Authentication

```json
{
  "DataDictionary": {
    "RequireAuthorization": true
  }
}
```

### Require Specific Roles

```json
{
  "DataDictionary": {
    "RequireAuthorization": true,
    "RequiredRoles": ["Admin", "DataAdmin"]
  }
}
```

### Use Named Authorization Policy

```json
{
  "DataDictionary": {
    "RequireAuthorization": true,
    "AuthorizationPolicy": "DataDictionaryPolicy"
  }
}
```

### Disable Auto-Migration

```json
{
  "DataDictionary": {
    "AutoMigrate": false
  }
}
```

### Enable Request Logging & Exception Handling

```csharp
app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
    middleware.IncludeStackTraceInErrors = app.Environment.IsDevelopment();
});
```

### DLL Security for EF Model Comparison

```json
{
  "DllSecurity": {
    "AllowedDirectories": ["C:\\PluginDlls"],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600
  }
}
```

---

## Complete Configuration Reference

### DataDictionaryOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RoutePrefix` | string | `"tools/datadictionary"` | URL path prefix |
| `ConnectionStringName` | string | `"DataDictionary"` | Connection string key |
| `AutoMigrate` | bool | `true` | Apply migrations on startup |
| `EnableSyncFeature` | bool | `true` | Enable database sync feature |
| `EnableComparisonFeature` | bool | `true` | Enable EF model comparison |
| `EnableEfModelSources` | bool | `true` | Enable model source management |
| `RequireAuthorization` | bool | `false` | Require authentication |
| `AuthorizationPolicy` | string? | `null` | Named auth policy |
| `RequiredRoles` | string[]? | `null` | Required roles |

### DataDictionaryMiddlewareOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseRequestLogging` | bool | `false` | Log HTTP requests |
| `UseExceptionHandling` | bool | `false` | Global exception handler |
| `IncludeStackTraceInErrors` | bool | `false` | Show stack traces |

---

## Full Example

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DataDictionary": "Server=localhost;Database=DataDictionary;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "DataDictionary": {
    "RoutePrefix": "tools/datadictionary",
    "AutoMigrate": true,
    "RequireAuthorization": true,
    "RequiredRoles": ["Admin"]
  }
}
```

**Program.cs:**
```csharp
using DataDictionary.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/tools/datadictionary/Home/Error");
}

app.UseHttpsRedirection();
app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
});
app.UseRouting();
app.UseAuthorization();
app.MapDataDictionary();

app.Run();
```

---

## Available Routes

| Route | Description |
|-------|-------------|
| `/tools/datadictionary` | Home dashboard |
| `/tools/datadictionary/Dictionary` | Data dictionary grid |
| `/tools/datadictionary/Sync` | Database sync |
| `/tools/datadictionary/Comparison` | EF Core comparison |
| `/tools/datadictionary/Sources` | EF model source management |

---

## View Customization

Override any view by placing your version in:
```
/Areas/DataDictionary/Views/{Controller}/{View}.cshtml
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Routes not found | Ensure `MapDataDictionary()` is called after `UseRouting()` |
| Static assets missing | Check `UseDataDictionary()` is called before routing |
| Migration errors | Set `AutoMigrate: false` and apply manually |
| Auth not working | Ensure `UseAuthorization()` is in pipeline |
