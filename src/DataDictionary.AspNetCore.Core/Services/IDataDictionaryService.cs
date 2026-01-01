using DataDictionary.AspNetCore.Core.Entities;
using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

public interface IDataDictionaryService
{
    IQueryable<DataElement> GetQueryable();
    Task<DataElementViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<DataElementViewModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<DataElementViewModel>> GetByDatabaseAsync(string server, string database, CancellationToken cancellationToken = default);
    Task<DataElementViewModel> UpdateAsync(DataElementUpdateViewModel model, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctServersAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctDatabasesAsync(string? server = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctTablesAsync(string? server = null, string? database = null, CancellationToken cancellationToken = default);
    Task<DataElementDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<List<DataElementAuditViewModel>> GetAuditHistoryAsync(int dataElementId, CancellationToken cancellationToken = default);
    IQueryable<DataElement> GetDeletedQueryable();

    // Note operations
    Task<DataElementNoteViewModel> AddNoteAsync(int dataElementId, string noteText, CancellationToken cancellationToken = default);
    Task<List<DataElementNoteViewModel>> GetNotesAsync(int dataElementId, CancellationToken cancellationToken = default);
}
