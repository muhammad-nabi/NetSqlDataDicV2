# Phase 3: Data Layer

## Overview

This phase adds the database migration for the `EfModelSources` table and implements the `IEfModelSourceService` for CRUD operations on EF model source configurations.

## Goals

- Create database migration for `EfModelSources` table
- Implement `IEfModelSourceService` for managing source configurations
- Add view models for source management UI
- Register the entity configuration with DbContext

## Prerequisites

- Phase 1 and Phase 2 completed
- EF Core migrations tooling available

## Implementation Steps

### Step 3.1: Register Entity Configuration in DbContext

**Modified File:** `src/NetSqlDataDicV2.Web/Data/DataDictionaryDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data.Configurations;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data;

public class DataDictionaryDbContext : DbContext
{
    public DataDictionaryDbContext(DbContextOptions<DataDictionaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataElement> DataElements => Set<DataElement>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    public DbSet<SourceConnection> SourceConnections => Set<SourceConnection>();

    // New DbSet for EF Model Sources
    public DbSet<EfModelSource> EfModelSources => Set<EfModelSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply existing configurations
        modelBuilder.ApplyConfiguration(new DataElementConfiguration());
        modelBuilder.ApplyConfiguration(new SyncHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new SourceConnectionConfiguration());

        // Apply new configuration
        modelBuilder.ApplyConfiguration(new EfModelSourceConfiguration());
    }
}
```

### Step 3.2: Create Database Migration

Run the following command from `src/NetSqlDataDicV2.Web`:

```bash
dotnet ef migrations add AddEfModelSources
```

This will generate a migration similar to:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetSqlDataDicV2.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEfModelSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EfModelSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssemblyPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DbContextTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetServer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetDatabase = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastComparedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfModelSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EfModelSources_IsActive",
                table: "EfModelSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EfModelSources_TargetServer_TargetDatabase",
                table: "EfModelSources",
                columns: new[] { "TargetServer", "TargetDatabase" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EfModelSources");
        }
    }
}
```

### Step 3.3: Create IEfModelSourceService Interface

**New File:** `src/NetSqlDataDicV2.Web/Services/IEfModelSourceService.cs`

```csharp
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

/// <summary>
/// Service for managing EF Model Source configurations.
/// </summary>
public interface IEfModelSourceService
{
    /// <summary>
    /// Gets all configured EF model sources.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all active EF model sources.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an EF model source by ID.
    /// </summary>
    Task<EfModelSource?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets EF model sources for a specific target database.
    /// </summary>
    Task<List<EfModelSourceViewModel>> GetByTargetDatabaseAsync(
        string server,
        string database,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new EF model source.
    /// </summary>
    Task<EfModelSource> CreateAsync(EfModelSourceCreateViewModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing EF model source.
    /// </summary>
    Task<EfModelSource> UpdateAsync(int id, EfModelSourceEditViewModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes an EF model source.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Updates the last compared timestamp for a source.
    /// </summary>
    Task UpdateLastComparedAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Toggles the active status of a source.
    /// </summary>
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Validates that the assembly path and DbContext are accessible.
    /// </summary>
    Task<ValidationResultViewModel> ValidateSourceAsync(int id, CancellationToken ct = default);
}
```

### Step 3.4: Implement EfModelSourceService

**New File:** `src/NetSqlDataDicV2.Web/Services/EfModelSourceService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Web.Services;

public class EfModelSourceService : IEfModelSourceService
{
    private readonly DataDictionaryDbContext _context;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<EfModelSourceService> _logger;

    public EfModelSourceService(
        DataDictionaryDbContext context,
        IDbContextProviderFactory providerFactory,
        ILogger<EfModelSourceService> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<List<EfModelSourceViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var sources = await _context.EfModelSources
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sources.Select(MapToViewModel).ToList();
    }

    public async Task<List<EfModelSourceViewModel>> GetActiveAsync(CancellationToken ct = default)
    {
        var sources = await _context.EfModelSources
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sources.Select(MapToViewModel).ToList();
    }

    public async Task<EfModelSource?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.EfModelSources.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<EfModelSourceViewModel>> GetByTargetDatabaseAsync(
        string server,
        string database,
        CancellationToken ct = default)
    {
        var sources = await _context.EfModelSources
            .Where(s => s.TargetServer == server && s.TargetDatabase == database && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sources.Select(MapToViewModel).ToList();
    }

    public async Task<EfModelSource> CreateAsync(
        EfModelSourceCreateViewModel model,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new EF model source: {Name}", model.Name);

        var source = new EfModelSource
        {
            Name = model.Name,
            ProviderType = model.ProviderType,
            AssemblyPath = model.AssemblyPath,
            DbContextTypeName = model.DbContextTypeName,
            ConnectionString = model.ConnectionString, // TODO: Encrypt in Phase 5
            TargetServer = model.TargetServer,
            TargetDatabase = model.TargetDatabase,
            Description = model.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created EF model source with ID: {Id}", source.Id);

        return source;
    }

    public async Task<EfModelSource> UpdateAsync(
        int id,
        EfModelSourceEditViewModel model,
        CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source == null)
        {
            throw new ArgumentException($"EfModelSource with ID {id} not found.");
        }

        _logger.LogInformation("Updating EF model source: {Id} - {Name}", id, model.Name);

        source.Name = model.Name;
        source.ProviderType = model.ProviderType;
        source.AssemblyPath = model.AssemblyPath;
        source.DbContextTypeName = model.DbContextTypeName;
        source.TargetServer = model.TargetServer;
        source.TargetDatabase = model.TargetDatabase;
        source.Description = model.Description;
        source.IsActive = model.IsActive;

        // Only update connection string if provided (to avoid overwriting with empty)
        if (!string.IsNullOrEmpty(model.ConnectionString))
        {
            source.ConnectionString = model.ConnectionString; // TODO: Encrypt in Phase 5
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated EF model source: {Id}", id);

        return source;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source == null)
        {
            throw new ArgumentException($"EfModelSource with ID {id} not found.");
        }

        _logger.LogInformation("Deleting EF model source: {Id} - {Name}", id, source.Name);

        _context.EfModelSources.Remove(source);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted EF model source: {Id}", id);
    }

    public async Task UpdateLastComparedAsync(int id, CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source != null)
        {
            source.LastComparedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source == null)
        {
            throw new ArgumentException($"EfModelSource with ID {id} not found.");
        }

        source.IsActive = !source.IsActive;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Toggled active status for source {Id} to {Status}",
            id, source.IsActive);

        return source.IsActive;
    }

    public async Task<ValidationResultViewModel> ValidateSourceAsync(int id, CancellationToken ct = default)
    {
        var source = await _context.EfModelSources.FindAsync(new object[] { id }, ct);

        if (source == null)
        {
            return new ValidationResultViewModel
            {
                IsValid = false,
                ErrorMessage = $"EfModelSource with ID {id} not found."
            };
        }

        return ValidateSource(source);
    }

    private ValidationResultViewModel ValidateSource(EfModelSource source)
    {
        _logger.LogInformation("Validating source: {Name}", source.Name);

        // For DynamicDll provider, check the assembly
        if (source.ProviderType == "DynamicDll")
        {
            if (string.IsNullOrEmpty(source.AssemblyPath))
            {
                return new ValidationResultViewModel
                {
                    IsValid = false,
                    ErrorMessage = "Assembly path is required for DynamicDll provider."
                };
            }

            if (!File.Exists(source.AssemblyPath))
            {
                return new ValidationResultViewModel
                {
                    IsValid = false,
                    ErrorMessage = $"Assembly file not found: {source.AssemblyPath}"
                };
            }

            // Try to discover DbContexts
            var dbContexts = _providerFactory.DiscoverDbContexts(source.AssemblyPath);

            if (dbContexts.Count == 0)
            {
                return new ValidationResultViewModel
                {
                    IsValid = false,
                    ErrorMessage = "No DbContext types found in the assembly."
                };
            }

            // If specific DbContext is specified, check it exists
            if (!string.IsNullOrEmpty(source.DbContextTypeName))
            {
                var found = dbContexts.Any(c =>
                    c.FullName == source.DbContextTypeName ||
                    c.Name == source.DbContextTypeName);

                if (!found)
                {
                    return new ValidationResultViewModel
                    {
                        IsValid = false,
                        ErrorMessage = $"DbContext '{source.DbContextTypeName}' not found. " +
                            $"Available: {string.Join(", ", dbContexts.Select(c => c.Name))}"
                    };
                }
            }

            // Try to create DbContext instance
            try
            {
                using var provider = _providerFactory.GetProvider(source);
                using var result = provider.GetDbContext(source);

                if (!result.Success)
                {
                    return new ValidationResultViewModel
                    {
                        IsValid = false,
                        ErrorMessage = result.ErrorMessage ?? "Failed to create DbContext instance."
                    };
                }

                return new ValidationResultViewModel
                {
                    IsValid = true,
                    Message = $"Successfully loaded {result.DbContextTypeName}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for source {Name}", source.Name);
                return new ValidationResultViewModel
                {
                    IsValid = false,
                    ErrorMessage = $"Failed to load DbContext: {ex.Message}"
                };
            }
        }

        // For Direct provider, just return valid (it's managed by DI)
        return new ValidationResultViewModel
        {
            IsValid = true,
            Message = "Direct reference provider configured."
        };
    }

    private static EfModelSourceViewModel MapToViewModel(EfModelSource source)
    {
        return new EfModelSourceViewModel
        {
            Id = source.Id,
            Name = source.Name,
            ProviderType = source.ProviderType,
            AssemblyPath = source.AssemblyPath,
            DbContextTypeName = source.DbContextTypeName,
            TargetServer = source.TargetServer,
            TargetDatabase = source.TargetDatabase,
            Description = source.Description,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            LastComparedAt = source.LastComparedAt,
            // Don't expose connection string in view model
            HasConnectionString = !string.IsNullOrEmpty(source.ConnectionString)
        };
    }
}
```

### Step 3.5: Create View Models

**New File:** `src/NetSqlDataDicV2.Web/Models/ViewModels/EfModelSourceViewModel.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

/// <summary>
/// View model for displaying EF model source information.
/// </summary>
public class EfModelSourceViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string? AssemblyPath { get; set; }
    public string? DbContextTypeName { get; set; }
    public string TargetServer { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastComparedAt { get; set; }
    public bool HasConnectionString { get; set; }

    /// <summary>
    /// Display string for the target database.
    /// </summary>
    public string TargetDisplay => $"{TargetServer}/{TargetDatabase}";

    /// <summary>
    /// Display string showing last compared time or "Never".
    /// </summary>
    public string LastComparedDisplay => LastComparedAt?.ToString("g") ?? "Never";
}
```

**New File:** `src/NetSqlDataDicV2.Web/Models/ViewModels/EfModelSourceCreateViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace NetSqlDataDicV2.Web.Models.ViewModels;

/// <summary>
/// View model for creating a new EF model source.
/// </summary>
public class EfModelSourceCreateViewModel
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Provider Type")]
    public string ProviderType { get; set; } = "DynamicDll";

    [MaxLength(500)]
    [Display(Name = "Assembly Path")]
    public string? AssemblyPath { get; set; }

    [MaxLength(500)]
    [Display(Name = "DbContext Type Name")]
    public string? DbContextTypeName { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Connection String")]
    [DataType(DataType.Password)]
    public string? ConnectionString { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Server")]
    public string TargetServer { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Database")]
    public string TargetDatabase { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
```

**New File:** `src/NetSqlDataDicV2.Web/Models/ViewModels/EfModelSourceEditViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace NetSqlDataDicV2.Web.Models.ViewModels;

/// <summary>
/// View model for editing an EF model source.
/// </summary>
public class EfModelSourceEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Provider Type")]
    public string ProviderType { get; set; } = "DynamicDll";

    [MaxLength(500)]
    [Display(Name = "Assembly Path")]
    public string? AssemblyPath { get; set; }

    [MaxLength(500)]
    [Display(Name = "DbContext Type Name")]
    public string? DbContextTypeName { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Connection String")]
    [DataType(DataType.Password)]
    public string? ConnectionString { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Server")]
    public string TargetServer { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Database")]
    public string TargetDatabase { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if a connection string is already stored (for UI display).
    /// </summary>
    public bool HasConnectionString { get; set; }
}
```

**New File:** `src/NetSqlDataDicV2.Web/Models/ViewModels/ValidationResultViewModel.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResultViewModel
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## Apply Migration

After creating the migration, apply it:

```bash
cd src/NetSqlDataDicV2.Web
dotnet ef database update
```

## Testing Checklist

- [ ] Migration creates `EfModelSources` table with correct schema
- [ ] Migration creates indexes on `IsActive` and `TargetServer/TargetDatabase`
- [ ] `EfModelSourceService.GetAllAsync()` returns all sources
- [ ] `EfModelSourceService.GetActiveAsync()` returns only active sources
- [ ] `EfModelSourceService.GetByIdAsync()` returns correct source
- [ ] `EfModelSourceService.CreateAsync()` creates new source
- [ ] `EfModelSourceService.UpdateAsync()` updates existing source
- [ ] `EfModelSourceService.DeleteAsync()` removes source
- [ ] `EfModelSourceService.ToggleActiveAsync()` toggles status
- [ ] `EfModelSourceService.ValidateSourceAsync()` validates DynamicDll sources
- [ ] View models correctly map entity properties

## Files Created

| File | Purpose |
|------|---------|
| `Services/IEfModelSourceService.cs` | Service interface |
| `Services/EfModelSourceService.cs` | Service implementation |
| `Models/ViewModels/EfModelSourceViewModel.cs` | Display view model |
| `Models/ViewModels/EfModelSourceCreateViewModel.cs` | Create view model |
| `Models/ViewModels/EfModelSourceEditViewModel.cs` | Edit view model |
| `Models/ViewModels/ValidationResultViewModel.cs` | Validation result |
| `Migrations/[timestamp]_AddEfModelSources.cs` | Database migration |

## Files Modified

| File | Change Type |
|------|-------------|
| `Data/DataDictionaryDbContext.cs` | Added DbSet and configuration |

## Next Phase

Phase 4 will add the UI layer including the EfModelSources management pages and modifications to the Comparison page to support source selection.
