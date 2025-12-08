using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Services;

public interface IEfModelService
{
    /// <summary>
    /// Gets EF model columns from a configured EfModelSource.
    /// </summary>
    List<EfModelColumnDto> GetEfModelColumns(EfModelSource source);

    /// <summary>
    /// Discovers available DbContext types in an assembly.
    /// </summary>
    List<DbContextInfo> DiscoverDbContexts(string assemblyPath);
}
