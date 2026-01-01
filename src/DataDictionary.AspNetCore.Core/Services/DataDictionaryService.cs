using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Core.Entities;
using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

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

    public IQueryable<DataElement> GetDeletedQueryable()
    {
        return _context.DataElements
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .AsNoTracking();
    }

    public async Task<DataElementViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        // Use IgnoreQueryFilters to allow viewing details of deleted records
        var entity = await _context.DataElements
            .IgnoreQueryFilters()
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

    public async Task<DataElementDetailsViewModel?> GetDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var element = await GetByIdAsync(id, cancellationToken);
        if (element == null)
        {
            return null;
        }

        var auditHistory = await GetAuditHistoryAsync(id, cancellationToken);

        var notes = await GetNotesAsync(id, cancellationToken);

        return new DataElementDetailsViewModel
        {
            DataElement = element,
            AuditHistory = auditHistory,
            Notes = notes
        };
    }

    public async Task<List<DataElementAuditViewModel>> GetAuditHistoryAsync(
        int dataElementId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataElementAudits
            .AsNoTracking()
            .Where(a => a.DataElementId == dataElementId)
            .OrderByDescending(a => a.ChangeTime)
            .Select(a => new DataElementAuditViewModel
            {
                DataElementAuditId = a.DataElementAuditId,
                DataElementId = a.DataElementId,
                SyncHistoryId = a.SyncHistoryId,
                ChangeType = a.ChangeType,
                PropertyName = a.PropertyName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ChangeTime = a.ChangeTime
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DataElementNoteViewModel> AddNoteAsync(
        int dataElementId,
        string noteText,
        CancellationToken cancellationToken = default)
    {
        // Verify DataElement exists (allow notes on deleted columns too)
        var exists = await _context.DataElements
            .IgnoreQueryFilters()
            .AnyAsync(e => e.DataElementId == dataElementId, cancellationToken);

        if (!exists)
            throw new InvalidOperationException($"DataElement {dataElementId} not found");

        var note = new DataElementNote
        {
            DataElementId = dataElementId,
            NoteText = noteText.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.DataElementNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added note {NoteId} to DataElement {ElementId}",
            note.DataElementNoteId, dataElementId);

        return new DataElementNoteViewModel
        {
            DataElementNoteId = note.DataElementNoteId,
            DataElementId = note.DataElementId,
            NoteText = note.NoteText,
            CreatedAt = note.CreatedAt
        };
    }

    public async Task<List<DataElementNoteViewModel>> GetNotesAsync(
        int dataElementId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataElementNotes
            .AsNoTracking()
            .Where(n => n.DataElementId == dataElementId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new DataElementNoteViewModel
            {
                DataElementNoteId = n.DataElementNoteId,
                DataElementId = n.DataElementId,
                NoteText = n.NoteText,
                CreatedAt = n.CreatedAt
            })
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
        Precision = entity.Precision,
        Scale = entity.Scale,
        CreateTime = entity.CreateTime,
        LastUpdateTime = entity.LastUpdateTime,
        LastSyncTime = entity.LastSyncTime,
        IsDeleted = entity.IsDeleted
    };
}
