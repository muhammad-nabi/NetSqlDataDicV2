# Phase 1: Remove Direct Reference Provider

**Status:** ✅ Complete
**Complexity:** Low
**Completed:** December 2024

## Overview

Remove the `DirectReferenceProvider` and entire `NetSqlDataDicV2.SourceModels` project. This eliminates project-specific coupling and forces all EF model comparisons to use the more flexible DynamicDll provider.

## Why Remove This?

1. **Redundant functionality** - DynamicDllProvider does everything DirectReferenceProvider does, plus more
2. **Project-specific coupling** - SourceModels contains AdventureWorks entities specific to sample database
3. **Simpler architecture** - One provider pattern is easier to maintain
4. **Better reusability** - Users must explicitly configure their EF models via DLL loading

## Files to DELETE

### 1. DirectReferenceProvider.cs
**Path:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DirectReferenceProvider.cs`

### 2. Entire SourceModels Project (16 files)
**Path:** `src/NetSqlDataDicV2.SourceModels/`

| File | Description |
|------|-------------|
| `SourceDbContext.cs` | DbContext with entity configurations |
| `Address.cs` | Entity model |
| `BuildVersion.cs` | Entity model |
| `Customer.cs` | Entity model |
| `CustomerAddress.cs` | Entity model |
| `ErrorLog.cs` | Entity model |
| `Product.cs` | Entity model |
| `ProductCategory.cs` | Entity model |
| `ProductDescription.cs` | Entity model |
| `ProductModel.cs` | Entity model |
| `ProductModelProductDescription.cs` | Entity model |
| `SalesOrderDetail.cs` | Entity model |
| `SalesOrderHeader.cs` | Entity model |
| `VGetAllCategory.cs` | View model |
| `VProductAndDescription.cs` | View model |
| `VProductModelCatalogDescription.cs` | View model |
| `NetSqlDataDicV2.SourceModels.csproj` | Project file |

## Files to MODIFY

### 1. DbContextProviderFactory.cs

**Path:** `src/NetSqlDataDicV2.Web/Services/DbContextProviders/DbContextProviderFactory.cs`

**Changes:**
```csharp
// REMOVE these lines:
private readonly ILogger<DirectReferenceProvider> _directProviderLogger;  // line 20

// REMOVE from constructor parameter:
ILogger<DirectReferenceProvider> directProviderLogger  // line 31

// REMOVE assignment:
_directProviderLogger = directProviderLogger;

// UPDATE ProviderTypes property - remove "Direct":
public IEnumerable<string> ProviderTypes => new[] { "DynamicDll" };  // was: { "Direct", "DynamicDll" }

// UPDATE GetProvider switch - remove Direct case:
return source.ProviderType switch
{
    "DynamicDll" => new DynamicDllProvider(...),
    _ => throw new ArgumentException($"Unknown provider type: {source.ProviderType}")
};
```

### 2. Program.cs

**Path:** `src/NetSqlDataDicV2.Web/Program.cs`

**Changes:**
```csharp
// REMOVE line 3:
using NetSqlDataDicV2.SourceModels;

// REMOVE lines 17-21 (SourceDatabase config check):
var sourceConnectionString = builder.Configuration.GetConnectionString("SourceDatabase");
if (string.IsNullOrEmpty(sourceConnectionString))
{
    Console.WriteLine("WARNING: SourceDatabase connection string not configured...");
}

// REMOVE lines 55-59 (SourceDbContext registration):
if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddDbContext<SourceDbContext>(options =>
        options.UseSqlServer(sourceConnectionString));
}
```

### 3. EfModelService.cs

**Path:** `src/NetSqlDataDicV2.Web/Services/EfModelService.cs`

**Changes:**
```csharp
// REMOVE line 3:
using NetSqlDataDicV2.SourceModels;

// REMOVE field (line 12):
private readonly SourceDbContext? _sourceContext;

// REMOVE constructor parameter (line 19):
SourceDbContext? sourceContext = null

// REMOVE assignment (line 21):
_sourceContext = sourceContext;

// REMOVE entire parameterless GetEfModelColumns method (lines 30-39):
public List<EfModelColumnDto> GetEfModelColumns()
{
    if (_sourceContext == null)
    {
        _logger.LogWarning("SourceDbContext is not available");
        return new List<EfModelColumnDto>();
    }
    return ExtractColumnsFromContext(_sourceContext);
}
```

### 4. IEfModelService.cs

**Path:** `src/NetSqlDataDicV2.Web/Services/IEfModelService.cs`

**Changes:**
```csharp
// REMOVE parameterless method declaration:
List<EfModelColumnDto> GetEfModelColumns();
```

### 5. ComparisonService.cs

**Path:** `src/NetSqlDataDicV2.Web/Services/ComparisonService.cs`

**Changes:**
```csharp
// UPDATE error message (line 83) - remove mention of "Direct Reference":
throw new InvalidOperationException(
    "Please select an EF Model Source from the dropdown to run a comparison.", ex);

// OR remove the entire fallback try-catch block if parameterless comparison is removed
```

### 6. NetSqlDataDicV2.Web.csproj

**Path:** `src/NetSqlDataDicV2.Web/NetSqlDataDicV2.Web.csproj`

**Changes:**
```xml
<!-- REMOVE line 10: -->
<ProjectReference Include="..\NetSqlDataDicV2.SourceModels\NetSqlDataDicV2.SourceModels.csproj" />
```

### 7. appsettings.json

**Path:** `src/NetSqlDataDicV2.Web/appsettings.json`

**Changes:**
```json
// REMOVE SourceDatabase from ConnectionStrings:
"SourceDatabase": "Server=localhost,1433;Database=YourSourceDb;..."

// REMOVE SourceDatabase section:
"SourceDatabase": {
  "Server": "localhost",
  "Database": "YourSourceDb"
}
```

### 8. appsettings.Development.json

**Path:** `src/NetSqlDataDicV2.Web/appsettings.Development.json`

**Changes:**
```json
// REMOVE SourceDatabase from ConnectionStrings:
"SourceDatabase": "Server=localhost,1433;Database=AdventureWorks;..."

// REMOVE SourceDatabase section:
"SourceDatabase": {
  "Server": "localhost",
  "Database": "AdventureWorks"
}
```

## Implementation Steps

### Step 1: Update DbContextProviderFactory
1. Remove DirectReferenceProvider logger field and constructor parameter
2. Remove "Direct" from ProviderTypes array
3. Remove "Direct" case from GetProvider switch statement

### Step 2: Update EfModelService
1. Remove SourceDbContext import
2. Remove SourceDbContext field and constructor parameter
3. Remove parameterless GetEfModelColumns() method

### Step 3: Update IEfModelService Interface
1. Remove parameterless GetEfModelColumns() method declaration

### Step 4: Update ComparisonService
1. Update or remove fallback error message

### Step 5: Update Program.cs
1. Remove SourceModels import
2. Remove SourceDatabase configuration check
3. Remove SourceDbContext DI registration

### Step 6: Update Project References
1. Remove ProjectReference to SourceModels from .csproj

### Step 7: Delete Files
1. Delete DirectReferenceProvider.cs
2. Delete entire NetSqlDataDicV2.SourceModels folder

### Step 8: Update Configuration
1. Remove SourceDatabase from appsettings.json
2. Remove SourceDatabase from appsettings.Development.json

### Step 9: Build and Test
1. Run `dotnet build` - verify no errors
2. Run `dotnet test` - verify tests pass
3. Manual test: Create EfModelSource, run comparison

## Testing Checklist

- [x] Solution builds without errors
- [x] No references to DirectReferenceProvider remain
- [x] No references to SourceDbContext remain
- [x] No references to NetSqlDataDicV2.SourceModels remain
- [x] EfModelSources page works (CRUD operations)
- [x] Comparison works with DynamicDll provider
- [x] Provider dropdown only shows "DynamicDll"
- [x] Error messages are clear when no source selected

## Rollback Plan

If issues arise:
1. Restore deleted files from git: `git checkout HEAD -- src/NetSqlDataDicV2.SourceModels/`
2. Revert modified files: `git checkout HEAD -- <file>`
3. Or revert entire phase: `git reset --hard HEAD~1`

## Post-Phase Cleanup

After successful completion:
1. Update CLAUDE.md to remove SourceModels references
2. Update any other documentation mentioning Direct Reference
3. Consider updating ComparisonController to require sourceId parameter
