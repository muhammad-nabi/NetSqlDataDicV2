using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public class DataDictionaryService : IDataDictionaryService
{
    private readonly DataDictionaryDbContext _context;
    private readonly ILogger<DataDictionaryService> _logger;

    public DataDictionaryService(
        DataDictionaryDbContext context,
        ILogger<DataDictionaryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<DataElement> GetQueryable()
    {
        return _context.DataElements.AsNoTracking();
    }

    public async Task<DataElementViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataElements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DataElementId == id, cancellationToken);

        return entity is null ? null : MapToViewModel(entity);
    }

    public async Task<List<DataElementViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataElements
            .AsNoTracking()
            .OrderBy(e => e.DatabaseServer)
            .ThenBy(e => e.DatabaseName)
            .ThenBy(e => e.SchemaName)
            .ThenBy(e => e.TableName)
            .ThenBy(e => e.ColumnName)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToViewModel).ToList();
    }

    public async Task<List<DataElementViewModel>> GetByDatabaseAsync(
        string server,
        string database,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataElements
            .AsNoTracking()
            .Where(e => e.DatabaseServer == server && e.DatabaseName == database)
            .OrderBy(e => e.SchemaName)
            .ThenBy(e => e.TableName)
            .ThenBy(e => e.ColumnName)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToViewModel).ToList();
    }

    public async Task<DataElementViewModel> UpdateAsync(
        DataElementUpdateViewModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataElements
            .FirstOrDefaultAsync(e => e.DataElementId == model.DataElementId, cancellationToken)
            ?? throw new InvalidOperationException($"DataElement with ID {model.DataElementId} not found");

        // Only update user-editable fields
        entity.DataPurpose = model.DataPurpose;
        entity.EntityPurpose = model.EntityPurpose;
        entity.OriginalDataSource = model.OriginalDataSource;
        entity.Notes = model.Notes;
        entity.LastUpdateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated DataElement {Id}: {Table}.{Column}",
            entity.DataElementId, entity.TableName, entity.ColumnName);

        return MapToViewModel(entity);
    }

    public async Task<List<string>> GetDistinctServersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.DataElements
            .AsNoTracking()
            .Select(e => e.DatabaseServer)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctDatabasesAsync(
        string? server = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataElements.AsNoTracking();

        if (!string.IsNullOrEmpty(server))
        {
            query = query.Where(e => e.DatabaseServer == server);
        }

        return await query
            .Select(e => e.DatabaseName)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctTablesAsync(
        string? server = null,
        string? database = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataElements.AsNoTracking();

        if (!string.IsNullOrEmpty(server))
        {
            query = query.Where(e => e.DatabaseServer == server);
        }

        if (!string.IsNullOrEmpty(database))
        {
            query = query.Where(e => e.DatabaseName == database);
        }

        return await query
            .Select(e => e.TableName)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    private static DataElementViewModel MapToViewModel(DataElement entity) => new()
    {
        DataElementId = entity.DataElementId,
        DataElementName = entity.DataElementName,
        DataElementType = entity.DataElementType,
        DataType = entity.DataType,
        DataPurpose = entity.DataPurpose,
        EntityPurpose = entity.EntityPurpose,
        DatabaseServer = entity.DatabaseServer,
        DatabaseName = entity.DatabaseName,
        SchemaName = entity.SchemaName,
        TableName = entity.TableName,
        ColumnName = entity.ColumnName,
        OriginalDataSource = entity.OriginalDataSource,
        Notes = entity.Notes,
        ForeignKeyTo = entity.ForeignKeyTo,
        RowCount = entity.RowCount,
        IsNullable = entity.IsNullable,
        IsPrimaryKey = entity.IsPrimaryKey,
        MaxLength = entity.MaxLength,
        CreateTime = entity.CreateTime,
        LastUpdateTime = entity.LastUpdateTime,
        LastSyncTime = entity.LastSyncTime
    };
}
