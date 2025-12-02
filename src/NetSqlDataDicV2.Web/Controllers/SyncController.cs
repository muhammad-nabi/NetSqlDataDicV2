using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class SyncController : Controller
{
    private readonly IDatabaseSyncService _syncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IDatabaseSyncService syncService,
        IConfiguration configuration,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var history = await _syncService.GetRecentSyncHistoryAsync(20);

        ViewBag.SourceServer = _configuration["SourceDatabase:Server"] ?? "localhost";
        ViewBag.SourceDatabase = _configuration["SourceDatabase:Database"] ?? "";

        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("SourceDatabase");
            var serverName = _configuration["SourceDatabase:Server"] ?? "localhost";
            var databaseName = _configuration["SourceDatabase:Database"] ?? "Unknown";

            if (string.IsNullOrEmpty(connectionString))
            {
                return Json(new { success = false, error = "Source database connection string not configured" });
            }

            var result = await _syncService.SyncDatabaseAsync(
                connectionString,
                serverName,
                databaseName,
                cancellationToken);

            return Json(new
            {
                success = result.Success,
                error = result.ErrorMessage,
                tablesProcessed = result.TablesProcessed,
                columnsProcessed = result.ColumnsProcessed,
                columnsAdded = result.ColumnsAdded,
                columnsUpdated = result.ColumnsUpdated,
                columnsRemoved = result.ColumnsRemoved,
                duration = result.Duration.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync execution failed");
            return Json(new { success = false, error = ex.Message });
        }
    }
}
