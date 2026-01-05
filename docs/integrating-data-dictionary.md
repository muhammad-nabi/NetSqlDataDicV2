# Integrating Data Dictionary RCL into .NET 8 MVC Application

## Overview
Step-by-step guide to integrate the Data Dictionary Razor Class Library into an existing .NET 8 MVC application.

## Prerequisites
- .NET 8 SDK installed
- SQL Server database available
- Existing ASP.NET Core MVC application (.NET 8)

---

## Step 1: Add Package References

Add to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="DataDictionary.AspNetCore" Version="1.0.0" />
  <PackageReference Include="DataDictionary.AspNetCore.Core" Version="1.0.0" />
</ItemGroup>
```

Or via CLI:
```bash
dotnet add package DataDictionary.AspNetCore
dotnet add package DataDictionary.AspNetCore.Core
```

---

## Step 2: Configure appsettings.json

Add connection string and configuration:

```json
{
  "ConnectionStrings": {
    "DataDictionary": "Server=your-server;Database=DataDictionary;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "DataDictionary": {
    "RoutePrefix": "tools/datadictionary",
    "AutoMigrate": true
  },
  "DllSecurity": {
    "AllowedDirectories": ["C:\\PluginDlls"],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600
  }
}
```

---

## Step 3: Update Program.cs

```csharp
using DataDictionary.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Your existing services
builder.Services.AddControllersWithViews();

// Add Data Dictionary services
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Option A: Basic setup (auto-migrates database)
app.UseDataDictionary();

// Option B: With optional middleware
app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
    middleware.IncludeStackTraceInErrors = app.Environment.IsDevelopment();
});

app.UseRouting();
app.UseAuthorization();

// Your existing routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Data Dictionary routes
app.MapDataDictionary();

app.Run();
```

---

## Step 4: Add Navigation Link (Optional)

In your `_Layout.cshtml`:

```html
<li class="nav-item">
    <a class="nav-link" href="/tools/datadictionary">Data Dictionary</a>
</li>
```

> **Note:** Use direct `href` with the configured route prefix. The Data Dictionary uses `ViewBag.RoutePrefix` internally for URL generation to ensure all links honor the configured `DataDictionary:RoutePrefix`.

---

## Step 5: Run the Application

```bash
dotnet run
```

Navigate to: `https://localhost:{port}/tools/datadictionary`

---

## What Happens on First Run

1. **Database Created** - DataDictionary database auto-created via EF migrations
2. **Tables Created** - DataElements, SyncHistory, EfModelSources, DataElementAudits, DataElementNotes
3. **UI Available** - Full Data Dictionary UI at `/tools/datadictionary`

---

## Available Features

| Feature | URL |
|---------|-----|
| Dashboard | `/tools/datadictionary` |
| Data Dictionary Grid | `/tools/datadictionary/Dictionary` |
| Database Sync | `/tools/datadictionary/Sync` |
| EF Model Comparison | `/tools/datadictionary/Comparison` |
| EF Model Sources | `/tools/datadictionary/Sources` |

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `RoutePrefix` | `tools/datadictionary` | URL prefix for all routes |
| `AutoMigrate` | `true` | Auto-apply EF migrations on startup |
| `ConnectionStringName` | `DataDictionary` | Connection string name |

---

---

# Publishing to NuGet

## Option 1: Publish to NuGet.org (Public)

### Step 1: Create NuGet Account
1. Go to https://www.nuget.org/
2. Create account or sign in
3. Go to Account → API Keys
4. Create new API key with "Push" scope

### Step 2: Update .csproj with Package Metadata

**DataDictionary.AspNetCore.Core.csproj:**
```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
  <PackageId>DataDictionary.AspNetCore.Core</PackageId>
  <Version>1.0.0</Version>
  <Authors>YourName</Authors>
  <Company>YourCompany</Company>
  <Description>Core library for Data Dictionary - entities, services, and DbContext</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/yourrepo</PackageProjectUrl>
  <RepositoryUrl>https://github.com/yourrepo</RepositoryUrl>
  <PackageTags>data-dictionary;ef-core;sql-server;aspnetcore</PackageTags>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### Step 3: Create NuGet Packages
```bash
# From solution root
dotnet pack -c Release -o ./nupkg
```

This creates:
- `nupkg/DataDictionary.AspNetCore.Core.1.0.0.nupkg`
- `nupkg/DataDictionary.AspNetCore.1.0.0.nupkg`

### Step 4: Push to NuGet.org
```bash
# Push Core library first (dependency)
dotnet nuget push ./nupkg/DataDictionary.AspNetCore.Core.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# Push RCL package
dotnet nuget push ./nupkg/DataDictionary.AspNetCore.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## Option 2: Private NuGet Feed (Azure DevOps / GitHub)

### Azure DevOps Artifacts
```bash
# Add Azure feed source
dotnet nuget add source https://pkgs.dev.azure.com/YOUR_ORG/_packaging/YOUR_FEED/nuget/v3/index.json \
  --name AzureDevOps \
  --username YOUR_USERNAME \
  --password YOUR_PAT

# Push packages
dotnet nuget push ./nupkg/*.nupkg --source AzureDevOps
```

### GitHub Packages
```bash
# Add GitHub source
dotnet nuget add source https://nuget.pkg.github.com/YOUR_USERNAME/index.json \
  --name GitHub \
  --username YOUR_USERNAME \
  --password YOUR_GITHUB_TOKEN

# Push packages
dotnet nuget push ./nupkg/*.nupkg --source GitHub
```

---

## Option 3: Local NuGet Feed (Development/Testing)

### Create Local Feed Folder
```bash
# Create local feed directory
mkdir C:\LocalNuGet

# Pack and copy to local feed
dotnet pack -c Release -o C:\LocalNuGet
```

### Configure Consumer Project
In consumer's `NuGet.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="LocalFeed" value="C:\LocalNuGet" />
  </packageSources>
</configuration>
```

---

## Quick Start: Local Development Testing

For testing before publishing:

```bash
# 1. Pack the libraries
dotnet pack -c Release -o ./nupkg

# 2. In consumer project, add local source
dotnet nuget add source /path/to/NetSqlDataDicV2/nupkg --name LocalDataDict

# 3. Install packages
dotnet add package DataDictionary.AspNetCore --source LocalDataDict
dotnet add package DataDictionary.AspNetCore.Core --source LocalDataDict
```

---

## Version Management

Update version in both .csproj files before each release:
```xml
<Version>1.0.1</Version>
```

Or use CI/CD to auto-increment:
```bash
dotnet pack -c Release -p:Version=1.0.1 -o ./nupkg
```
