using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class ComparisonController : Controller
{
    private readonly IComparisonService _comparisonService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        IComparisonService comparisonService,
        IConfiguration configuration,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewBag.SourceServer = _configuration["SourceDatabase:Server"] ?? "localhost";
        ViewBag.SourceDatabase = _configuration["SourceDatabase:Database"] ?? "";

        return View(new ComparisonResultViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Compare(CancellationToken cancellationToken)
    {
        try
        {
            var server = _configuration["SourceDatabase:Server"] ?? "localhost";
            var database = _configuration["SourceDatabase:Database"] ?? "";

            var result = await _comparisonService.CompareAsync(server, database, cancellationToken);

            return Json(new
            {
                success = true,
                totalItems = result.TotalItems,
                totalMatches = result.TotalMatches,
                totalMissingInEf = result.TotalMissingInEf,
                totalMissingInDb = result.TotalMissingInDb,
                totalTypeMismatches = result.TotalTypeMismatches,
                items = result.Items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CompareGrid(
        [DataSourceRequest] DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var server = _configuration["SourceDatabase:Server"] ?? "localhost";
            var database = _configuration["SourceDatabase:Database"] ?? "";

            var result = await _comparisonService.CompareAsync(server, database, cancellationToken);

            return Json(result.Items.ToDataSourceResult(request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison grid failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
