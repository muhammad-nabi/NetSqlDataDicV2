using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

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
}
