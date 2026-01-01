# Phase 1: Create RCL Project Structure

## Status: Pending

## Overview

Create the new project structure with two new projects: the Core library and the Razor Class Library (RCL).

## Goals

1. Create `DataDictionary.AspNetCore.Core` project for shared entities, services, and DbContext
2. Create `DataDictionary.AspNetCore` Razor Class Library for UI components
3. Update solution file to include new projects
4. Configure project references

## New Directory Structure

```
src/
├── NetSqlDataDicV2.Web/              (existing - becomes sample/demo app)
├── DataDictionary.AspNetCore.Core/   (NEW - shared entities, services, interfaces)
│   └── DataDictionary.AspNetCore.Core.csproj
└── DataDictionary.AspNetCore/        (NEW - Razor Class Library)
    └── DataDictionary.AspNetCore.csproj
```

## Files to Create

### 1. DataDictionary.AspNetCore.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackageId>DataDictionary.AspNetCore.Core</PackageId>
    <Version>1.0.0</Version>
    <Description>Core library for Data Dictionary - entities, services, and DbContext</Description>
    <Authors>Your Name</Authors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
  </ItemGroup>
</Project>
```

**Location:** `src/DataDictionary.AspNetCore.Core/DataDictionary.AspNetCore.Core.csproj`

### 2. DataDictionary.AspNetCore.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    <PackageId>DataDictionary.AspNetCore</PackageId>
    <Version>1.0.0</Version>
    <Description>Pluggable Data Dictionary UI for ASP.NET Core MVC</Description>
    <Authors>Your Name</Authors>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DataDictionary.AspNetCore.Core\DataDictionary.AspNetCore.Core.csproj" />
  </ItemGroup>
</Project>
```

**Location:** `src/DataDictionary.AspNetCore/DataDictionary.AspNetCore.csproj`

### 3. Update Solution File

Add the new projects to `NetSqlDataDicV2.sln`:

```bash
dotnet sln add src/DataDictionary.AspNetCore.Core/DataDictionary.AspNetCore.Core.csproj
dotnet sln add src/DataDictionary.AspNetCore/DataDictionary.AspNetCore.csproj
```

## Verification Steps

1. Run `dotnet build` to ensure projects compile
2. Verify project references resolve correctly
3. Check solution explorer shows new projects

## Dependencies

- None (first phase)

## Next Phase

[Phase 2: Extract Core Library](phase2-core-library.md)
