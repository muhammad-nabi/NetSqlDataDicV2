# Phase 2: Extract Core Library

## Status: Pending

## Overview

Move shared entities, services, DbContext, and related components to the `DataDictionary.AspNetCore.Core` project.

## Goals

1. Move all entities to Core project
2. Move DbContext and configurations
3. Move all services (interfaces and implementations)
4. Move exceptions and helpers
5. Update namespaces throughout
6. Ensure all references compile

## File Movements

### Entities (Models/Entities/ → Core/Entities/)

| Source | Destination |
|--------|-------------|
| `Models/Entities/DataElement.cs` | `Core/Entities/DataElement.cs` |
| `Models/Entities/DataElementAudit.cs` | `Core/Entities/DataElementAudit.cs` |
| `Models/Entities/DataElementNote.cs` | `Core/Entities/DataElementNote.cs` |
| `Models/Entities/SyncHistory.cs` | `Core/Entities/SyncHistory.cs` |
| `Models/Entities/SourceConnection.cs` | `Core/Entities/SourceConnection.cs` |
| `Models/Entities/EfModelSource.cs` | `Core/Entities/EfModelSource.cs` |

### Data Layer (Data/ → Core/Data/)

| Source | Destination |
|--------|-------------|
| `Data/DataDictionaryDbContext.cs` | `Core/Data/DataDictionaryDbContext.cs` |
| `Data/Configurations/DataElementConfiguration.cs` | `Core/Data/Configurations/DataElementConfiguration.cs` |
| `Data/Configurations/DataElementAuditConfiguration.cs` | `Core/Data/Configurations/DataElementAuditConfiguration.cs` |
| `Data/Configurations/DataElementNoteConfiguration.cs` | `Core/Data/Configurations/DataElementNoteConfiguration.cs` |
| `Data/Configurations/SyncHistoryConfiguration.cs` | `Core/Data/Configurations/SyncHistoryConfiguration.cs` |
| `Data/Configurations/EfModelSourceConfiguration.cs` | `Core/Data/Configurations/EfModelSourceConfiguration.cs` |
| `Data/Configurations/SourceConnectionConfiguration.cs` | `Core/Data/Configurations/SourceConnectionConfiguration.cs` |

### Services (Services/ → Core/Services/)

| Source | Destination |
|--------|-------------|
| `Services/IDataDictionaryService.cs` | `Core/Services/IDataDictionaryService.cs` |
| `Services/DataDictionaryService.cs` | `Core/Services/DataDictionaryService.cs` |
| `Services/IDatabaseSyncService.cs` | `Core/Services/IDatabaseSyncService.cs` |
| `Services/DatabaseSyncService.cs` | `Core/Services/DatabaseSyncService.cs` |
| `Services/IComparisonService.cs` | `Core/Services/IComparisonService.cs` |
| `Services/ComparisonService.cs` | `Core/Services/ComparisonService.cs` |
| `Services/IEfModelService.cs` | `Core/Services/IEfModelService.cs` |
| `Services/EfModelService.cs` | `Core/Services/EfModelService.cs` |
| `Services/IEfModelSourceService.cs` | `Core/Services/IEfModelSourceService.cs` |
| `Services/EfModelSourceService.cs` | `Core/Services/EfModelSourceService.cs` |

### Security Services (Services/Security/ → Core/Services/Security/)

| Source | Destination |
|--------|-------------|
| `Services/Security/IDllValidatorService.cs` | `Core/Services/Security/IDllValidatorService.cs` |
| `Services/Security/DllValidatorService.cs` | `Core/Services/Security/DllValidatorService.cs` |
| `Services/Security/IConnectionStringProtector.cs` | `Core/Services/Security/IConnectionStringProtector.cs` |
| `Services/Security/ConnectionStringProtector.cs` | `Core/Services/Security/ConnectionStringProtector.cs` |
| `Services/Security/ISecurityAuditService.cs` | `Core/Services/Security/ISecurityAuditService.cs` |
| `Services/Security/SecurityAuditService.cs` | `Core/Services/Security/SecurityAuditService.cs` |

### DbContext Providers (Services/DbContextProviders/ → Core/Services/DbContextProviders/)

| Source | Destination |
|--------|-------------|
| `Services/DbContextProviders/IDbContextProvider.cs` | `Core/Services/DbContextProviders/IDbContextProvider.cs` |
| `Services/DbContextProviders/IDbContextProviderFactory.cs` | `Core/Services/DbContextProviders/IDbContextProviderFactory.cs` |
| `Services/DbContextProviders/DbContextProviderFactory.cs` | `Core/Services/DbContextProviders/DbContextProviderFactory.cs` |
| `Services/DbContextProviders/DynamicDllProvider.cs` | `Core/Services/DbContextProviders/DynamicDllProvider.cs` |
| `Services/DbContextProviders/PluginLoadContext.cs` | `Core/Services/DbContextProviders/PluginLoadContext.cs` |
| `Services/DbContextProviders/DllShadowCopyService.cs` | `Core/Services/DbContextProviders/DllShadowCopyService.cs` |
| `Services/DbContextProviders/DbContextProviderResult.cs` | `Core/Services/DbContextProviders/DbContextProviderResult.cs` |

### Exceptions (Exceptions/ → Core/Exceptions/)

| Source | Destination |
|--------|-------------|
| `Exceptions/DllLoadException.cs` | `Core/Exceptions/DllLoadException.cs` |
| `Exceptions/DbContextCreationException.cs` | `Core/Exceptions/DbContextCreationException.cs` |
| `Exceptions/DependencyResolutionException.cs` | `Core/Exceptions/DependencyResolutionException.cs` |
| `Exceptions/EfModelSourceException.cs` | `Core/Exceptions/EfModelSourceException.cs` |

### Configuration (Configuration/ → Core/Configuration/)

| Source | Destination |
|--------|-------------|
| `Configuration/DllSecurityOptions.cs` | `Core/Configuration/DllSecurityOptions.cs` |

### Helpers (Helpers/ → Core/Helpers/)

| Source | Destination |
|--------|-------------|
| `Helpers/ErrorMessages.cs` | `Core/Helpers/ErrorMessages.cs` |

### Models (Models/ → Core/Models/)

| Source | Destination |
|--------|-------------|
| `Models/OperationResult.cs` | `Core/Models/OperationResult.cs` |
| `Models/PaginationRequest.cs` | `Core/Models/PaginationRequest.cs` |
| `Models/PaginationResponse.cs` | `Core/Models/PaginationResponse.cs` |
| `Models/Dto/EfModelColumnDto.cs` | `Core/Models/Dto/EfModelColumnDto.cs` |
| `Models/Dto/SourceColumnDto.cs` | `Core/Models/Dto/SourceColumnDto.cs` |
| `Models/Dto/DbContextInfo.cs` | `Core/Models/Dto/DbContextInfo.cs` |
| `Models/Dto/PropertyChange.cs` | `Core/Models/Dto/PropertyChange.cs` |

### Folders to Skip/Delete

| Folder | Action | Reason |
|--------|--------|--------|
| `Models/Enums/` | Delete | Empty folder, no files to move |

## Namespace Updates

All moved files need namespace changes:

| Old Namespace | New Namespace |
|---------------|---------------|
| `NetSqlDataDicV2.Web.Models.Entities` | `DataDictionary.AspNetCore.Core.Entities` |
| `NetSqlDataDicV2.Web.Data` | `DataDictionary.AspNetCore.Core.Data` |
| `NetSqlDataDicV2.Web.Services` | `DataDictionary.AspNetCore.Core.Services` |
| `NetSqlDataDicV2.Web.Exceptions` | `DataDictionary.AspNetCore.Core.Exceptions` |
| `NetSqlDataDicV2.Web.Configuration` | `DataDictionary.AspNetCore.Core.Configuration` |
| `NetSqlDataDicV2.Web.Helpers` | `DataDictionary.AspNetCore.Core.Helpers` |
| `NetSqlDataDicV2.Web.Models` | `DataDictionary.AspNetCore.Core.Models` |

## Core Service Registration Extension

Create `Core/Extensions/ServiceCollectionExtensions.cs`:

```csharp
namespace DataDictionary.AspNetCore.Core.Extensions;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddDataDictionaryCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Security services
        services.AddScoped<IDllValidatorService, DllValidatorService>();
        services.AddScoped<IConnectionStringProtector, ConnectionStringProtector>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddSingleton<IDllShadowCopyService, DllShadowCopyService>();

        // Core services
        services.AddScoped<IDataDictionaryService, DataDictionaryService>();
        services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
        services.AddScoped<IEfModelService, EfModelService>();
        services.AddScoped<IEfModelSourceService, EfModelSourceService>();
        services.AddScoped<IComparisonService, ComparisonService>();
        services.AddScoped<IDbContextProviderFactory, DbContextProviderFactory>();

        // Configuration
        services.Configure<DllSecurityOptions>(
            configuration.GetSection("DllSecurity"));

        return services;
    }
}
```

## Verification Steps

1. All files moved to correct locations
2. All namespaces updated
3. `dotnet build` succeeds for Core project
4. No circular dependencies

## Dependencies

- Phase 1 complete (project structure exists)

## Next Phase

[Phase 3: Create UI Package](phase3-ui-package.md)
