using NetSqlDataDicV2.Web.Models.Dto;

namespace NetSqlDataDicV2.Web.Services;

public interface IEfModelService
{
    List<EfModelColumnDto> GetEfModelColumns();
}
