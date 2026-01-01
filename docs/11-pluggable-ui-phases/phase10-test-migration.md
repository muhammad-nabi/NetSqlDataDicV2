# Phase 10: Test Project Migration

## Status: Pending

## Overview

Update the test project to work with the new package structure after namespace changes.

## Goals

1. Update test project references to new packages
2. Update all namespace imports
3. Update controller test routing for Areas
4. Verify all tests pass

## Test Files to Update (15 files)

### Service Tests

| File | Namespace Changes |
|------|------------------|
| `DataDictionaryServiceTests.cs` | `NetSqlDataDicV2.Web.Services` → `DataDictionary.AspNetCore.Core.Services` |
| `DatabaseSyncServiceTests.cs` | Same as above |
| `ComparisonServiceTests.cs` | Same as above |
| `EfModelServiceTests.cs` | Same as above |
| `EfModelSourceServiceTests.cs` | Same as above |

### Security Service Tests

| File | Namespace Changes |
|------|------------------|
| `DllValidatorServiceTests.cs` | `NetSqlDataDicV2.Web.Services.Security` → `DataDictionary.AspNetCore.Core.Services.Security` |
| `ConnectionStringProtectorTests.cs` | Same as above |
| `SecurityAuditServiceTests.cs` | Same as above |

### Controller Tests

| File | Namespace Changes |
|------|------------------|
| `DataDictionaryControllerTests.cs` | `NetSqlDataDicV2.Web.Controllers` → `DataDictionary.AspNetCore.Areas.DataDictionary.Controllers` |
| `ComparisonControllerTests.cs` | Same as above |
| `EfModelSourcesControllerTests.cs` | Same as above, also rename to `SourcesControllerTests.cs` |
| `SyncControllerTests.cs` | Same as above |

### Middleware Tests

| File | Namespace Changes |
|------|------------------|
| `ExceptionHandlingMiddlewareTests.cs` | `NetSqlDataDicV2.Web.Middleware` → `DataDictionary.AspNetCore.Middleware` |
| `RequestLoggingMiddlewareTests.cs` | Same as above |

### Helper Tests

| File | Namespace Changes |
|------|------------------|
| `ErrorMessagesTests.cs` | `NetSqlDataDicV2.Web.Helpers` → `DataDictionary.AspNetCore.Core.Helpers` |

## Project Reference Updates

Update `NetSqlDataDicV2.Tests.csproj`:

```xml
<ItemGroup>
  <!-- Remove old reference -->
  <!-- <ProjectReference Include="..\..\src\NetSqlDataDicV2.Web\NetSqlDataDicV2.Web.csproj" /> -->

  <!-- Add new references -->
  <ProjectReference Include="..\..\src\DataDictionary.AspNetCore.Core\DataDictionary.AspNetCore.Core.csproj" />
  <ProjectReference Include="..\..\src\DataDictionary.AspNetCore\DataDictionary.AspNetCore.csproj" />
</ItemGroup>
```

## Controller Test Route Updates

Controller tests that verify routing need updates for Area-based routes:

```csharp
// Before
var result = await controller.Index() as ViewResult;
Assert.Equal("/DataDictionary/Details/1", url);

// After
var result = await controller.Index() as ViewResult;
Assert.Equal("/tools/datadictionary/Dictionary/Details/1", url);
// Or use RedirectToActionResult with area parameter verification
```

## Test Helper Updates

Update `TestDbContextFactory` if namespace changes:

```csharp
// Update using statements
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Core.Entities;
```

Update `TestDataBuilder` classes similarly.

## Verification Steps

1. `dotnet build` test project succeeds
2. `dotnet test` - all 380+ tests pass
3. No namespace compilation errors
4. Controller routing assertions updated

## Dependencies

- Phase 1-9 complete (all code moved/refactored)

## Completion Criteria

- All tests compile
- All tests pass
- Test coverage remains at ~39%

## Next Steps

After Phase 10, the package conversion is complete:
1. Local testing with sample app
2. NuGet package creation
3. Publishing to NuGet feed
