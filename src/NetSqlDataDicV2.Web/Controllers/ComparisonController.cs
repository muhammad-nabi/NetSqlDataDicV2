using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class ComparisonController : Controller
{
    private readonly IComparisonService _comparisonService;
    private readonly IEfModelSourceService _sourceService;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        IComparisonService comparisonService,
        IEfModelSourceService sourceService,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _sourceService = sourceService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? sourceId, CancellationToken ct)
    {
        // Get all active sources for dropdown
        var sources = await _sourceService.GetActiveAsync(ct);
        ViewBag.EfModelSources = sources;
        ViewBag.SelectedSourceId = sourceId;

        // If sourceId provided, use that source's target for display
        if (sourceId.HasValue)
        {
            var source = await _sourceService.GetByIdAsync(sourceId.Value, ct);
            if (source != null)
            {
                ViewBag.SourceServer = source.TargetServer;
                ViewBag.SourceDatabase = source.TargetDatabase;
                ViewBag.SourceName = source.Name;
            }
        }

        return View(new ComparisonResultViewModel());
    }

    /// <summary>
    /// Compare using configured EF Model Source.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CompareSource(int sourceId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _comparisonService.CompareAsync(sourceId, cancellationToken);

            _logger.LogInformation(
                "Comparison executed for source {SourceId}: {Total} items, {Matches} matches, {MissingInEf} missing in EF, {TypeMismatches} type mismatches",
                sourceId, result.TotalItems, result.TotalMatches, result.TotalMissingInEf, result.TotalTypeMismatches);

            return Json(new
            {
                success = true,
                totalItems = result.TotalItems,
                totalMatches = result.TotalMatches,
                totalMissingInEf = result.TotalMissingInEf,
                totalMissingInDb = result.TotalMissingInDb,
                totalTypeMismatches = result.TotalTypeMismatches,
                totalConstraintMismatches = result.TotalConstraintMismatches,
                items = result.Items,
                skippedTables = result.SkippedTables,
                totalSkippedTables = result.TotalSkippedTables,
                totalSkippedColumns = result.TotalSkippedColumns
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed for source {SourceId}", sourceId);
            return Json(new { success = false, error = ex.Message });
        }
    }
}
