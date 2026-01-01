using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataDictionary.AspNetCore.Core.Exceptions;
using DataDictionary.AspNetCore.Core.Helpers;
using DataDictionary.AspNetCore.Core.Models.ViewModels;
using DataDictionary.AspNetCore.Core.Services;
using DataDictionary.AspNetCore.Core.Services.DbContextProviders;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

[Area("DataDictionary")]
public class SourcesController : Controller
{
    private readonly IEfModelSourceService _sourceService;
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<SourcesController> _logger;

    public SourcesController(
        IEfModelSourceService sourceService,
        IDbContextProviderFactory providerFactory,
        ILogger<SourcesController> logger)
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
    /// Get sources for Grid (AJAX).
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

            _logger.LogInformation("EF Model Source created: {Name} ({DllPath})", model.Name, model.AssemblyPath);

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

            _logger.LogInformation("EF Model Source updated: {Id} ({Name})", id, model.Name);

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

            _logger.LogInformation("EF Model Source deleted: {Id}", id);

            return Json(new { success = true, message = "Source deleted successfully." });
        }
        catch (ArgumentException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete source {Id}", id);
            return Json(new { success = false, message = ErrorMessages.UnexpectedError() });
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

            _logger.LogInformation("EF Model Source {Id} toggled to {Status}", id, newStatus ? "active" : "inactive");

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
        try
        {
            var result = await _sourceService.ValidateSourceAsync(id, ct);
            return Json(result);
        }
        catch (DllLoadException ex)
        {
            _logger.LogWarning(ex, "DLL validation failed for source {Id}", id);
            return Json(new ValidationResultViewModel
            {
                IsValid = false,
                ErrorMessage = ex.Message
            });
        }
        catch (DbContextCreationException ex)
        {
            _logger.LogWarning(ex, "DbContext creation failed for source {Id}", id);
            return Json(new ValidationResultViewModel
            {
                IsValid = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating source {Id}", id);
            return Json(new ValidationResultViewModel
            {
                IsValid = false,
                ErrorMessage = ErrorMessages.UnexpectedError()
            });
        }
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
        catch (DllLoadException ex)
        {
            _logger.LogWarning(ex, "DLL load failed during discovery: {Path}", request.AssemblyPath);
            return Json(new { success = false, message = ex.Message });
        }
        catch (DependencyResolutionException ex)
        {
            _logger.LogWarning(ex, "Missing dependencies during discovery: {Path}", request.AssemblyPath);
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover DbContexts in {Path}", request.AssemblyPath);
            return Json(new { success = false, message = ErrorMessages.UnexpectedError() });
        }
    }

    public class DiscoverRequest
    {
        public string? AssemblyPath { get; set; }
    }
}
