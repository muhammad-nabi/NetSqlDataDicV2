# Simplification Plan: Minimal Reusable Data Dictionary

## Overview

This folder contains the phase-by-phase plan for simplifying NetSqlDataDicV2 into a minimal, reusable Data Dictionary tool that can be deployed across multiple projects without project-specific dependencies or licensing requirements.

## Goals

1. **Remove licensing burden** - Eliminate Telerik Kendo UI dependency
2. **Remove project-specific coupling** - Delete SourceModels project and DirectReferenceProvider
3. **Simplify architecture** - Keep only DynamicDllProvider for EF model loading
4. **Reduce complexity** - Remove unused features (Privacy page, redundant navigation)
5. **Maximize reusability** - Tool should work with any EF Core project via DLL loading

## Current vs Target State

| Aspect | Current | Target |
|--------|---------|--------|
| UI Framework | Telerik Kendo UI (licensed) | Bootstrap 5 + DataTables (free) |
| EF Model Loading | DirectReference + DynamicDll | DynamicDll only |
| Project Dependencies | 2 projects (Web + SourceModels) | 1 project (Web only) |
| Sample Data | AdventureWorks entities | None (user provides DLL) |
| Complexity | ~100% | ~70-75% |

## Phases

| Phase | Name | Complexity | Status | Description |
|-------|------|------------|--------|-------------|
| [1](phase1-remove-direct-reference.md) | Remove Direct Reference | Low | Pending | Delete DirectReferenceProvider & SourceModels project |
| [2](phase2-replace-kendo-ui.md) | Replace Kendo UI | High | Pending | Replace Telerik Kendo with DataTables |
| [3](phase3-cleanup-navigation.md) | Cleanup Navigation | Low | Pending | Remove Privacy page, simplify Home |
| [4](phase4-configuration-cleanup.md) | Configuration Cleanup | Low | Pending | Consolidate configuration, final polish |

## Implementation Order

```
Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4
   │           │           │           │
   │           │           │           └─ Final polish, config consolidation
   │           │           └─ Quick UI cleanup (Privacy, Home)
   │           └─ Biggest change: Kendo → DataTables
   └─ Foundation: Remove project-specific code
```

**Why this order:**
1. Phase 1 first: Removes SourceModels dependency, simplifies project structure
2. Phase 2 second: Biggest change, benefits from cleaner architecture
3. Phase 3 third: Quick cleanup after major changes
4. Phase 4 last: Final polish when everything else is stable

## Success Criteria

- [ ] No Telerik/Kendo references in codebase
- [ ] Solution has only 1 project (Web) + Tests
- [ ] All grids work with DataTables
- [ ] EF comparison only works via DynamicDll provider
- [ ] No "SourceDatabase" configuration required
- [ ] Build succeeds with 0 errors
- [ ] All existing functionality preserved

## Estimated Impact

| Metric | Before | After | Reduction |
|--------|--------|-------|-----------|
| Projects | 2 | 1 | 50% |
| NuGet Packages | ~15 | ~12 | 20% |
| License Requirements | Telerik | None | 100% |
| Lines of Code | ~5000 | ~3750 | 25% |
| Complexity | High | Medium | ~30% |

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| DataTables feature gap | Use DataTables with Editor plugin for inline editing |
| Breaking existing functionality | Test each phase independently |
| Performance regression | DataTables supports server-side processing |
| User confusion | Update documentation, clear error messages |

## Core Features Preserved

These features remain fully functional after simplification:

1. ✅ Data Dictionary CRUD (view, edit metadata)
2. ✅ Database Sync from SQL Server
3. ✅ EF Model Comparison (via DLL loading)
4. ✅ EF Model Source Management
5. ✅ CSV Export
6. ✅ Security (DLL validation, connection string encryption)
7. ✅ Error Handling (custom exceptions, middleware)

## Features Removed

These features are intentionally removed:

1. ❌ Direct Reference comparison (use DLL loading instead)
2. ❌ SourceModels sample entities (user provides their own)
3. ❌ Telerik Kendo UI (replaced with DataTables)
4. ❌ Privacy page (placeholder content)
5. ❌ Complex Home page navigation cards

## Related Documentation

- [DLL Loading Phases](../dll-loading-phases/README.md) - Completed feature
- [Original Architecture](../architecture.md) - System design
