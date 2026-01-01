using DataDictionary.AspNetCore.Core.Models.Dto;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Services;

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
