using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class DataDictionaryController : Controller
{
    private readonly IDataDictionaryService _service;
    private readonly ILogger<DataDictionaryController> _logger;

    public DataDictionaryController(
        IDataDictionaryService service,
        ILogger<DataDictionaryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Servers = await _service.GetDistinctServersAsync();
        ViewBag.Databases = await _service.GetDistinctDatabasesAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var query = _service.GetQueryable()
                .Select(e => new DataElementViewModel
                {
                    DataElementId = e.DataElementId,
                    DataElementName = e.DataElementName,
                    DataElementType = e.DataElementType,
                    DataType = e.DataType,
                    DataPurpose = e.DataPurpose,
                    EntityPurpose = e.EntityPurpose,
                    DatabaseServer = e.DatabaseServer,
                    DatabaseName = e.DatabaseName,
                    SchemaName = e.SchemaName,
                    TableName = e.TableName,
                    ColumnName = e.ColumnName,
                    OriginalDataSource = e.OriginalDataSource,
                    Notes = e.Notes,
                    ForeignKeyTo = e.ForeignKeyTo,
                    RowCount = e.RowCount,
                    IsNullable = e.IsNullable,
                    IsPrimaryKey = e.IsPrimaryKey,
                    MaxLength = e.MaxLength,
                    CreateTime = e.CreateTime,
                    LastUpdateTime = e.LastUpdateTime,
                    LastSyncTime = e.LastSyncTime
                });

            var result = await query.ToDataSourceResultAsync(request);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading data elements");
            return StatusCode(500, new { error = "An error occurred while loading data" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(
        [DataSourceRequest] DataSourceRequest request,
        DataElementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        try
        {
            var updateModel = new DataElementUpdateViewModel
            {
                DataElementId = model.DataElementId,
                DataPurpose = model.DataPurpose,
                EntityPurpose = model.EntityPurpose,
                OriginalDataSource = model.OriginalDataSource,
                Notes = model.Notes
            };

            var updated = await _service.UpdateAsync(updateModel);
            return Json(new[] { updated }.ToDataSourceResult(request, ModelState));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data element {Id}", model.DataElementId);
            ModelState.AddModelError("", "An error occurred while saving");
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDatabases(string server)
    {
        var databases = await _service.GetDistinctDatabasesAsync(server);
        return Json(databases);
    }

    [HttpGet]
    public async Task<IActionResult> GetTables(string server, string database)
    {
        var tables = await _service.GetDistinctTablesAsync(server, database);
        return Json(tables);
    }
}
