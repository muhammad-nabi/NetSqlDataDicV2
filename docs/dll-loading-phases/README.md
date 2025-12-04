# Runtime DLL Loading Implementation Plan

## Overview

This folder contains the phase-by-phase implementation plan for adding runtime DLL loading capability to the Data Dictionary EF Model Comparison feature.

**Goal**: Allow users to compare any EF Core DbContext by loading assemblies (DLLs) at runtime, eliminating the need for compile-time project references.

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                        Web Application                          │
├─────────────────────────────────────────────────────────────────┤
│  ComparisonController                                           │
│       │                                                         │
│       ▼                                                         │
│  IComparisonService                                             │
│       │                                                         │
│       ├──► IDataDictionaryService (unchanged)                   │
│       │                                                         │
│       └──► IEfModelService (modified to use provider)           │
│                 │                                               │
│                 ▼                                               │
│            IDbContextProviderFactory                            │
│                 │                                               │
│        ┌────────┴───────────────┐                               │
│        ▼                        ▼                               │
│  DirectReferenceProvider    DynamicDllProvider                  │
│  (existing behavior)        (NEW - loads from path)             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Phases

| Phase | Name | Complexity | Status | Description |
|-------|------|------------|--------|-------------|
| [1](phase1-core-infrastructure.md) | Core Infrastructure | High | ✅ Complete | Provider abstraction, PluginLoadContext, EfModelSource entity |
| [2](phase2-service-layer.md) | Service Layer | Medium | ⏳ Pending | Factory pattern, service modifications, backward compatibility |
| [3](phase3-data-layer.md) | Data Layer | Low | ⏳ Pending | Database migration, CRUD service for sources |
| [4](phase4-ui-layer.md) | UI Layer | Medium | ⏳ Pending | Management pages, comparison integration |
| [5](phase5-security.md) | Security & Validation | Medium | ⏳ Pending | DLL validation, encryption, path restrictions |
| [6](phase6-error-handling.md) | Error Handling | Low | ⏳ Pending | Custom exceptions, user-friendly messages |

## Quick Reference: Files to Create/Modify

### New Files (22 files)

**Services/DbContextProviders/** ✅ Phase 1 Complete
- `IDbContextProvider.cs` ✅
- `IDbContextProviderFactory.cs`
- `DbContextProviderFactory.cs`
- `DbContextProviderResult.cs` ✅
- `DirectReferenceProvider.cs` ✅
- `DynamicDllProvider.cs` ✅
- `PluginLoadContext.cs` ✅

**Services/Security/**
- `IDllValidatorService.cs`
- `DllValidatorService.cs`
- `IConnectionStringProtector.cs`
- `ConnectionStringProtector.cs`
- `ISecurityAuditService.cs`
- `SecurityAuditService.cs`

**Services/**
- `IEfModelSourceService.cs`
- `EfModelSourceService.cs`

**Models/** ✅ Phase 1 Complete
- `Entities/EfModelSource.cs` ✅
- `Dto/DbContextInfo.cs` ✅
- `ViewModels/EfModelSourceViewModel.cs`
- `ViewModels/EfModelSourceCreateViewModel.cs`
- `ViewModels/EfModelSourceEditViewModel.cs`
- `ViewModels/ValidationResultViewModel.cs`
- `OperationResult.cs`

**Configuration/**
- `DllSecurityOptions.cs`
- `Data/Configurations/EfModelSourceConfiguration.cs` ✅

**Exceptions/**
- `DllLoadException.cs`
- `DbContextCreationException.cs`
- `DependencyResolutionException.cs`
- `EfModelSourceException.cs`

**Helpers/**
- `ErrorMessages.cs`

**Middleware/**
- `ExceptionHandlingMiddleware.cs`

**Controllers/**
- `EfModelSourcesController.cs`

**Views/EfModelSources/**
- `Index.cshtml`
- `Create.cshtml`
- `Edit.cshtml`

### Modified Files (9 files)

- `Services/IEfModelService.cs`
- `Services/EfModelService.cs`
- `Services/IComparisonService.cs`
- `Services/ComparisonService.cs`
- `Controllers/ComparisonController.cs`
- `Views/Comparison/Index.cshtml`
- `Data/DataDictionaryDbContext.cs` ✅
- `Program.cs`
- `Views/Shared/_Layout.cshtml`

## Implementation Order

```
Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4 ──► Phase 5 ──► Phase 6
  │           │           │           │           │           │
  │           │           │           │           │           └─ Error handling
  │           │           │           │           └─ Security (can be parallel with 4)
  │           │           │           └─ UI (depends on 1-3)
  │           │           └─ Database (depends on 1)
  │           └─ Services (depends on 1)
  └─ Core (must be first)
```

## Dependencies Required

All dependencies are already in the project or included in .NET:
- `System.Runtime.Loader` (built-in)
- `Microsoft.EntityFrameworkCore` (existing)
- `Microsoft.AspNetCore.DataProtection` (existing)

## Configuration Required

Add to `appsettings.json`:

```json
{
  "DllSecurity": {
    "AllowedDirectories": [
      "C:\\DataDictionary\\Plugins"
    ],
    "AllowedExtensions": [".dll"],
    "RequireSignedAssemblies": false,
    "MaxFileSizeBytes": 104857600
  }
}
```

## Testing Strategy

### Unit Tests
- Provider factory selection
- DLL validation logic
- Path security checks
- Connection string encryption

### Integration Tests
- End-to-end comparison with dynamic DLL
- Multiple DbContext sources
- Error scenarios

### Manual Testing
- Various DbContext patterns
- Different EF Core versions
- Large models (100+ entities)

## Success Criteria

- [ ] Load DbContext from external DLL without code changes
- [ ] Support multiple EF Model Sources simultaneously
- [ ] Graceful error handling for missing dependencies
- [ ] No memory leaks after unloading assemblies
- [ ] Backward compatible with existing direct reference
- [ ] Secure against path traversal and malicious DLLs
- [ ] Clear user feedback for configuration errors

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Dependency conflicts | Isolated `AssemblyLoadContext` |
| Malicious DLLs | Path whitelisting, validation |
| Memory leaks | Collectible context, proper disposal |
| Complex errors | Comprehensive logging, clear messages |

## Related Documentation

- [Original Option 2 Plan](../generic-comparison-option2-dll-loading.md)
- [Option 5 Alternative (NuGet Package)](../generic-comparison-option5-nuget-package.md)
- [Phase 4 EF Comparison](../phase4-ef-comparison.md)
