using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Web.Controllers;

public class EfModelSourcesController : Controller
{
    private readonly IEfModelSourceService _sourceService;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<EfModelSourcesController> _logger;

    public EfModelSourcesController(
        IEfModelSourceService sourceService,
        IDbContextProviderFactory providerFactory,
        ILogger<EfModelSourcesController> logger)
    {
        _sourceService = sourceService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// List all EF Model Sources.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sources = await _sourceService.GetAllAsync(ct);
        return View(sources);
    }

    /// <summary>
    /// Get sources for Kendo Grid (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetSources(CancellationToken ct)
    {
        var sources = await _sourceService.GetAllAsync(ct);
        return Json(new { Data = sources, Total = sources.Count });
    }

    /// <summary>
    /// Show create form.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new EfModelSourceCreateViewModel
        {
            ProviderType = "DynamicDll"
        };

        ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
        return View(model);
    }

    /// <summary>
    /// Create new source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EfModelSourceCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }

        try
        {
            await _sourceService.CreateAsync(model, ct);
            TempData["SuccessMessage"] = $"Source '{model.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create source");
            ModelState.AddModelError("", $"Failed to create source: {ex.Message}");
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Show edit form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var source = await _sourceService.GetByIdAsync(id, ct);

        if (source == null)
        {
            return NotFound();
        }

        var model = new EfModelSourceEditViewModel
        {
            Id = source.Id,
            Name = source.Name,
            ProviderType = source.ProviderType,
            AssemblyPath = source.AssemblyPath,
            DbContextTypeName = source.DbContextTypeName,
            TargetServer = source.TargetServer,
            TargetDatabase = source.TargetDatabase,
            Description = source.Description,
            IsActive = source.IsActive,
            HasConnectionString = !string.IsNullOrEmpty(source.ConnectionString)
        };

        ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
        return View(model);
    }

    /// <summary>
    /// Update source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EfModelSourceEditViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }

        try
        {
            await _sourceService.UpdateAsync(id, model, ct);
            TempData["SuccessMessage"] = $"Source '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update source {Id}", id);
            ModelState.AddModelError("", $"Failed to update source: {ex.Message}");
            ViewBag.ProviderTypes = _providerFactory.GetAvailableProviderTypes().ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Delete source.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _sourceService.DeleteAsync(id, ct);
            return Json(new { success = true, message = "Source deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete source {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle active status.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        try
        {
            var newStatus = await _sourceService.ToggleActiveAsync(id, ct);
            return Json(new { success = true, isActive = newStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle active status for {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Validate source configuration.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Validate(int id, CancellationToken ct)
    {
        var result = await _sourceService.ValidateSourceAsync(id, ct);
        return Json(result);
    }

    /// <summary>
    /// Discover DbContexts in an assembly.
    /// </summary>
    [HttpPost]
    public IActionResult DiscoverDbContexts([FromBody] DiscoverRequest request)
    {
        if (string.IsNullOrEmpty(request?.AssemblyPath))
        {
            return Json(new { success = false, message = "Assembly path is required." });
        }

        try
        {
            var dbContexts = _providerFactory.DiscoverDbContexts(request.AssemblyPath);
            return Json(new { success = true, dbContexts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover DbContexts in {Path}", request.AssemblyPath);
            return Json(new { success = false, message = ex.Message });
        }
    }

    public class DiscoverRequest
    {
        public string? AssemblyPath { get; set; }
    }
}
