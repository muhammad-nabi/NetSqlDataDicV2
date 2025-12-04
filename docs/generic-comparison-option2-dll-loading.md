# Option 2: Runtime DLL Loading for Generic EF Model Comparison

## Overview

This approach allows users to compare any EF Core DbContext by loading assemblies (DLLs) at runtime, eliminating the need for compile-time project references.

## Goals

- Load any DLL containing an EF Core DbContext without recompiling the web application
- Support multiple DbContext sources simultaneously
- Maintain backward compatibility with the existing direct-reference approach
- Provide a user-friendly configuration interface

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Web Application                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ComparisonController                                           │
│       │                                                         │
│       ▼                                                         │
│  IComparisonService (modified)                                  │
│       │                                                         │
│       ├──► IDataDictionaryService (unchanged)                   │
│       │                                                         │
│       └──► IEfModelService (modified to use provider)           │
│                 │                                               │
│                 ▼                                               │
│            IDbContextProvider (NEW)                             │
│                 │                                               │
│        ┌───────┴────────────────┐                               │
│        ▼                        ▼                               │
│  DirectReferenceProvider    DynamicDllProvider                  │
│  (existing behavior)        (NEW - loads from path)             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Implementation Plan

### Phase 1: Core Infrastructure

#### 1.1 Create DbContext Provider Abstraction

**New Files:**
- `Services/DbContextProviders/IDbContextProvider.cs`
- `Services/DbContextProviders/DbContextProviderResult.cs`

```csharp
public interface IDbContextProvider
{
    string ProviderName { get; }
    bool CanProvide(EfModelSource source);
    DbContextProviderResult GetDbContext(EfModelSource source);
    void Dispose();
}

public class DbContextProviderResult : IDisposable
{
    public DbContext Context { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public AssemblyLoadContext LoadContext { get; set; } // For unloading
}
```

#### 1.2 Create EF Model Source Configuration

**New Files:**
- `Models/Entities/EfModelSource.cs`
- `Data/Configurations/EfModelSourceConfiguration.cs`

```csharp
public class EfModelSource
{
    public int Id { get; set; }
    public string Name { get; set; }                    // Display name
    public string ProviderType { get; set; }            // "Direct" or "DynamicDll"
    public string AssemblyPath { get; set; }            // Path to DLL (for DynamicDll)
    public string DbContextTypeName { get; set; }       // Full type name of DbContext
    public string ConnectionString { get; set; }        // Connection string (encrypted)
    public string TargetServer { get; set; }            // Server to compare against
    public string TargetDatabase { get; set; }          // Database to compare against
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastComparedAt { get; set; }
}
```

#### 1.3 Implement Dynamic DLL Loader

**New Files:**
- `Services/DbContextProviders/DynamicDllProvider.cs`
- `Services/DbContextProviders/PluginLoadContext.cs`

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}

public class DynamicDllProvider : IDbContextProvider
{
    public string ProviderName => "DynamicDll";

    public DbContextProviderResult GetDbContext(EfModelSource source)
    {
        // 1. Create isolated AssemblyLoadContext
        // 2. Load assembly from path
        // 3. Find DbContext type
        // 4. Create instance with connection string
        // 5. Return wrapped result
    }
}
```

### Phase 2: Service Layer Modifications

#### 2.1 Modify IEfModelService

**Modified Files:**
- `Services/IEfModelService.cs`
- `Services/EfModelService.cs`

```csharp
public interface IEfModelService
{
    // Existing method (backward compatible)
    List<EfModelColumnDto> GetEfModelColumns();

    // New method for generic sources
    List<EfModelColumnDto> GetEfModelColumns(EfModelSource source);

    // Get available DbContext types from a DLL
    List<DbContextInfo> DiscoverDbContexts(string assemblyPath);
}
```

#### 2.2 Modify IComparisonService

**Modified Files:**
- `Services/IComparisonService.cs`
- `Services/ComparisonService.cs`

```csharp
public interface IComparisonService
{
    // Existing method (backward compatible)
    Task<ComparisonResultViewModel> CompareAsync(
        string server,
        string database,
        CancellationToken ct = default);

    // New method for configured sources
    Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken ct = default);
}
```

#### 2.3 Create DbContext Provider Factory

**New Files:**
- `Services/DbContextProviders/IDbContextProviderFactory.cs`
- `Services/DbContextProviders/DbContextProviderFactory.cs`

```csharp
public interface IDbContextProviderFactory
{
    IDbContextProvider GetProvider(EfModelSource source);
    IEnumerable<string> GetAvailableProviderTypes();
}
```

### Phase 3: Data Layer

#### 3.1 Database Migration

Create migration to add `EfModelSources` table:

```sql
CREATE TABLE EfModelSources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    ProviderType NVARCHAR(50) NOT NULL,
    AssemblyPath NVARCHAR(500) NULL,
    DbContextTypeName NVARCHAR(500) NULL,
    ConnectionString NVARCHAR(MAX) NULL,  -- Encrypted
    TargetServer NVARCHAR(200) NOT NULL,
    TargetDatabase NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    LastComparedAt DATETIME2 NULL
);
```

#### 3.2 Add Repository/Service for EfModelSource

**New Files:**
- `Services/IEfModelSourceService.cs`
- `Services/EfModelSourceService.cs`

### Phase 4: UI Layer

#### 4.1 EF Model Sources Management Page

**New Files:**
- `Views/EfModelSources/Index.cshtml` - List/grid of configured sources
- `Views/EfModelSources/Create.cshtml` - Add new source
- `Views/EfModelSources/Edit.cshtml` - Edit existing source
- `Controllers/EfModelSourcesController.cs`

**Features:**
- CRUD operations for EF Model Sources
- File upload for DLL (optional - or specify path)
- Test connection button
- Discover DbContexts in DLL button

#### 4.2 Modify Comparison Page

**Modified Files:**
- `Views/Comparison/Index.cshtml`
- `Controllers/ComparisonController.cs`

**Changes:**
- Add dropdown to select EF Model Source
- Show source details (DLL path, DbContext type)
- Maintain backward compatibility with direct reference

### Phase 5: Security & Validation

#### 5.1 DLL Validation

```csharp
public class DllValidator
{
    public ValidationResult Validate(string dllPath)
    {
        // 1. Check file exists
        // 2. Verify it's a valid .NET assembly
        // 3. Check for required EF Core dependencies
        // 4. Scan for DbContext types
        // 5. Optionally: virus scan, signature check
    }
}
```

#### 5.2 Path Restrictions

- Configure allowed directories for DLL loading
- Prevent path traversal attacks
- Whitelist approach for production

#### 5.3 Connection String Encryption

- Encrypt connection strings at rest
- Use Data Protection API or Azure Key Vault

### Phase 6: Error Handling & Logging

#### 6.1 Custom Exceptions

**New Files:**
- `Exceptions/DllLoadException.cs`
- `Exceptions/DbContextCreationException.cs`
- `Exceptions/DependencyResolutionException.cs`

#### 6.2 Comprehensive Logging

```csharp
_logger.LogInformation("Loading assembly from {Path}", dllPath);
_logger.LogWarning("Missing dependency {Dependency} for {Assembly}", dep, assembly);
_logger.LogError(ex, "Failed to create DbContext {Type}", contextType);
```

## File Structure

```
src/NetSqlDataDicV2.Web/
├── Services/
│   ├── DbContextProviders/
│   │   ├── IDbContextProvider.cs
│   │   ├── IDbContextProviderFactory.cs
│   │   ├── DbContextProviderFactory.cs
│   │   ├── DbContextProviderResult.cs
│   │   ├── DirectReferenceProvider.cs
│   │   ├── DynamicDllProvider.cs
│   │   └── PluginLoadContext.cs
│   ├── IEfModelSourceService.cs
│   ├── EfModelSourceService.cs
│   ├── IEfModelService.cs (modified)
│   ├── EfModelService.cs (modified)
│   ├── IComparisonService.cs (modified)
│   └── ComparisonService.cs (modified)
├── Models/
│   ├── Entities/
│   │   └── EfModelSource.cs
│   └── ViewModels/
│       ├── EfModelSourceViewModel.cs
│       └── DbContextDiscoveryViewModel.cs
├── Data/
│   └── Configurations/
│       └── EfModelSourceConfiguration.cs
├── Controllers/
│   ├── ComparisonController.cs (modified)
│   └── EfModelSourcesController.cs (new)
├── Views/
│   ├── Comparison/
│   │   └── Index.cshtml (modified)
│   └── EfModelSources/
│       ├── Index.cshtml
│       ├── Create.cshtml
│       └── Edit.cshtml
└── Exceptions/
    ├── DllLoadException.cs
    ├── DbContextCreationException.cs
    └── DependencyResolutionException.cs
```

## Technical Considerations

### Assembly Loading

| Consideration | Solution |
|---------------|----------|
| Dependency conflicts | Use `AssemblyLoadContext` isolation |
| Memory leaks | Make context collectible, dispose properly |
| Version mismatches | Load EF Core from plugin's dependencies |
| Missing dependencies | Validate and report missing assemblies |

### DbContext Instantiation

```csharp
// Option A: Parameterless constructor + OnConfiguring
var context = (DbContext)Activator.CreateInstance(contextType);

// Option B: Constructor with DbContextOptions
var optionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
var optionsBuilder = new DbContextOptionsBuilder();
optionsBuilder.UseSqlServer(connectionString);
var context = (DbContext)Activator.CreateInstance(contextType, optionsBuilder.Options);

// Option C: Try both approaches
```

### Performance Considerations

| Concern | Mitigation |
|---------|------------|
| DLL loading time | Cache loaded contexts |
| Model reflection | Cache EfModelColumnDto results |
| Memory usage | Unload contexts when done |
| Concurrent access | Thread-safe provider factory |

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Dependency version conflicts | High | Medium | Isolated AssemblyLoadContext |
| Malicious DLL execution | Medium | High | Path whitelisting, validation |
| Memory leaks from unloaded assemblies | Medium | Medium | Proper disposal, collectible contexts |
| Complex error scenarios | High | Low | Comprehensive logging, clear error messages |
| DbContext requires runtime services | Medium | Medium | Document requirements, fail gracefully |

## Testing Strategy

### Unit Tests
- DLL loading with valid assembly
- DLL loading with invalid file
- DbContext discovery
- Provider factory selection

### Integration Tests
- End-to-end comparison with dynamic DLL
- Multiple DbContext sources
- Concurrent comparisons

### Manual Testing
- Various DbContext patterns (scaffolded, code-first, mixed)
- Different EF Core versions
- Large models (100+ entities)

## Rollout Plan

1. **Development**: Implement core infrastructure
2. **Internal Testing**: Test with various internal projects
3. **Documentation**: User guide for DLL preparation
4. **Beta Release**: Limited users with feedback collection
5. **GA Release**: Full feature availability

## User Documentation Required

- How to prepare a DLL for loading
- Required dependencies to include
- Supported DbContext patterns
- Troubleshooting common issues
- Security best practices

## Estimated Effort

| Phase | Complexity | Notes |
|-------|------------|-------|
| Phase 1: Core Infrastructure | High | AssemblyLoadContext complexity |
| Phase 2: Service Modifications | Medium | Refactoring existing code |
| Phase 3: Data Layer | Low | Standard EF operations |
| Phase 4: UI Layer | Medium | New pages + modifications |
| Phase 5: Security | Medium | Critical for production |
| Phase 6: Error Handling | Low | Standard patterns |

## Dependencies

- .NET 9.0 (current)
- Microsoft.EntityFrameworkCore 9.0.0 (current)
- System.Runtime.Loader (for AssemblyLoadContext)
- Microsoft.AspNetCore.DataProtection (for encryption)

## Success Criteria

- [ ] Can load DbContext from external DLL without code changes
- [ ] Supports multiple EF Model Sources simultaneously
- [ ] Graceful error handling for missing dependencies
- [ ] No memory leaks after unloading assemblies
- [ ] Backward compatible with existing direct reference
- [ ] Secure against path traversal and malicious DLLs
- [ ] Clear user feedback for configuration errors
