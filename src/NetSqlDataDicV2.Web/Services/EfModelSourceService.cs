using Microsoft.EntityFrameworkCore;
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
