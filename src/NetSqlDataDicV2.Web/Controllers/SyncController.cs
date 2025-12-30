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

            _logger.LogInformation(
                "Sync executed for {Server}/{Database}: {Added} added, {Updated} updated, {Removed} deleted in {Duration:F1}s",
                serverName, databaseName,
                result.ColumnsAdded, result.ColumnsUpdated, result.ColumnsRemoved,
                result.Duration.TotalSeconds);

            return Json(new
            {
                success = result.Success,
                error = result.ErrorMessage,
                syncHistoryId = result.SyncHistoryId,
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

    /// <summary>
    /// Displays detailed results for a specific sync operation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Results(int id, CancellationToken cancellationToken)
    {
        var results = await _syncService.GetSyncResultsAsync(id, cancellationToken);

        if (results == null)
        {
            _logger.LogWarning("Sync history {SyncHistoryId} not found", id);
            return NotFound();
        }

        return View(results);
    }
}
