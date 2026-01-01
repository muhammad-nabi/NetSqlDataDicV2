using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataDictionary.AspNetCore.Core.Models;
using DataDictionary.AspNetCore.Core.Models.ViewModels;
using DataDictionary.AspNetCore.Core.Services;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class DictionaryController : Controller
{
    private readonly IDataDictionaryService _service;
    private readonly ILogger<DictionaryController> _logger;

    public DictionaryController(
        IDataDictionaryService service,
        ILogger<DictionaryController> logger)
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
    public async Task<IActionResult> Read([FromBody] PaginationRequest request, CancellationToken ct)
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
                    LastSyncTime = e.LastSyncTime,
                    NoteCount = e.DataElementNotes.Count
                });

            // Apply filters
            if (request.Filters != null)
            {
                if (request.Filters.TryGetValue("server", out var serverFilter) && !string.IsNullOrEmpty(serverFilter))
                {
                    query = query.Where(e => e.DatabaseServer == serverFilter);
                }
                if (request.Filters.TryGetValue("database", out var databaseFilter) && !string.IsNullOrEmpty(databaseFilter))
                {
                    query = query.Where(e => e.DatabaseName == databaseFilter);
                }
            }

            // Apply search
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(e =>
                    (e.ColumnName != null && e.ColumnName.ToLower().Contains(term)) ||
                    (e.TableName != null && e.TableName.ToLower().Contains(term)) ||
                    (e.DataPurpose != null && e.DataPurpose.ToLower().Contains(term)) ||
                    (e.Notes != null && e.Notes.ToLower().Contains(term)));
            }

            // Apply sorting
            query = ApplySorting(query, request.SortField, request.SortDirection);

            var total = await query.CountAsync(ct);
            var data = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(ct);

            return Json(PaginationResponse<DataElementViewModel>.Create(data, total, request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading data elements");
            return StatusCode(500, new { error = "An error occurred while loading data" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] DataElementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
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

            _logger.LogInformation("DataElement {Id} updated successfully", model.DataElementId);

            return Json(new { success = true, data = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data element {Id}", model.DataElementId);
            return Json(new { success = false, errors = new[] { "An error occurred while saving" } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await _service.GetDetailsAsync(id, cancellationToken);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddNote([FromBody] AddNoteViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return Json(new { success = false, errors });
        }

        try
        {
            var note = await _service.AddNoteAsync(model.DataElementId, model.NoteText, ct);

            _logger.LogInformation("Note added to DataElement {Id}", model.DataElementId);

            return Json(new { success = true, data = note });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to add note - DataElement not found");
            return Json(new { success = false, errors = new[] { ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to DataElement {Id}", model.DataElementId);
            return Json(new { success = false, errors = new[] { "Failed to add note. Please try again." } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetNotes(int id, CancellationToken ct)
    {
        var notes = await _service.GetNotesAsync(id, ct);
        return Json(notes);
    }

    public IActionResult Deleted()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ReadDeleted([FromBody] PaginationRequest request, CancellationToken ct)
    {
        try
        {
            var query = _service.GetDeletedQueryable()
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
                    LastSyncTime = e.LastSyncTime,
                    IsDeleted = e.IsDeleted
                });

            // Apply filters
            if (request.Filters != null)
            {
                if (request.Filters.TryGetValue("server", out var serverFilter) && !string.IsNullOrEmpty(serverFilter))
                {
                    query = query.Where(e => e.DatabaseServer == serverFilter);
                }
                if (request.Filters.TryGetValue("database", out var databaseFilter) && !string.IsNullOrEmpty(databaseFilter))
                {
                    query = query.Where(e => e.DatabaseName == databaseFilter);
                }
            }

            // Apply search
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(e =>
                    (e.ColumnName != null && e.ColumnName.ToLower().Contains(term)) ||
                    (e.TableName != null && e.TableName.ToLower().Contains(term)));
            }

            // Apply sorting - default to LastUpdateTime (deleted date) descending
            query = ApplySorting(query, request.SortField ?? "lastupdatetime", request.SortDirection ?? "desc");

            var total = await query.CountAsync(ct);
            var data = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(ct);

            return Json(PaginationResponse<DataElementViewModel>.Create(data, total, request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading deleted data elements");
            return StatusCode(500, new { error = "An error occurred while loading data" });
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

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? server = null, string? database = null)
    {
        var data = string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database)
            ? await _service.GetAllAsync()
            : await _service.GetByDatabaseAsync(server, database);

        var csv = new StringBuilder();
        csv.AppendLine("Server,Database,Schema,Table,Column,DataType,Nullable,PrimaryKey,ForeignKeyTo,Purpose,Notes");

        foreach (var item in data)
        {
            csv.AppendLine($"\"{EscapeCsv(item.DatabaseServer)}\",\"{EscapeCsv(item.DatabaseName)}\",\"{EscapeCsv(item.SchemaName)}\",\"{EscapeCsv(item.TableName)}\",\"{EscapeCsv(item.ColumnName)}\",\"{EscapeCsv(item.DataType)}\",{item.IsNullable},{item.IsPrimaryKey},\"{EscapeCsv(item.ForeignKeyTo)}\",\"{EscapeCsv(item.DataPurpose)}\",\"{EscapeCsv(item.Notes)}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        _logger.LogInformation(
            "CSV export generated: {RowCount} rows, filters: Server={Server}, Database={Database}",
            data.Count(), server ?? "all", database ?? "all");

        return File(bytes, "text/csv", $"data-dictionary-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }

    private static IQueryable<DataElementViewModel> ApplySorting(IQueryable<DataElementViewModel> query, string? field, string? direction)
    {
        var isDesc = direction?.ToLower() == "desc";
        return field?.ToLower() switch
        {
            "databaseserver" => isDesc ? query.OrderByDescending(e => e.DatabaseServer) : query.OrderBy(e => e.DatabaseServer),
            "databasename" => isDesc ? query.OrderByDescending(e => e.DatabaseName) : query.OrderBy(e => e.DatabaseName),
            "schemaname" => isDesc ? query.OrderByDescending(e => e.SchemaName) : query.OrderBy(e => e.SchemaName),
            "tablename" => isDesc ? query.OrderByDescending(e => e.TableName) : query.OrderBy(e => e.TableName),
            "columnname" => isDesc ? query.OrderByDescending(e => e.ColumnName) : query.OrderBy(e => e.ColumnName),
            "datatype" => isDesc ? query.OrderByDescending(e => e.DataType) : query.OrderBy(e => e.DataType),
            "isnullable" => isDesc ? query.OrderByDescending(e => e.IsNullable) : query.OrderBy(e => e.IsNullable),
            "isprimarykey" => isDesc ? query.OrderByDescending(e => e.IsPrimaryKey) : query.OrderBy(e => e.IsPrimaryKey),
            "datapurpose" => isDesc ? query.OrderByDescending(e => e.DataPurpose) : query.OrderBy(e => e.DataPurpose),
            "notecount" => isDesc ? query.OrderByDescending(e => e.NoteCount) : query.OrderBy(e => e.NoteCount),
            "lastupdatetime" => isDesc ? query.OrderByDescending(e => e.LastUpdateTime) : query.OrderBy(e => e.LastUpdateTime),
            _ => query.OrderBy(e => e.TableName).ThenBy(e => e.ColumnName)
        };
    }
}
