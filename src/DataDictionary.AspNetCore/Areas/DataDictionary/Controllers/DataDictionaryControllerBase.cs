using DataDictionary.AspNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

/// <summary>
/// Base controller for all Data Dictionary controllers.
/// Provides common functionality like route prefix injection.
/// </summary>
[Area("DataDictionary")]
public abstract class DataDictionaryControllerBase : Controller
{
    private readonly DataDictionaryOptions _options;

    protected DataDictionaryControllerBase(DataDictionaryOptions options)
    {
        _options = options;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Inject route prefix for use in views
        var prefix = _options.RoutePrefix.TrimStart('/').TrimEnd('/');
        ViewBag.RoutePrefix = "/" + prefix;

        // Inject consumer application URL for Exit link
        ViewBag.ConsumerApplicationUrl = _options.ConsumerApplicationUrl;

        base.OnActionExecuting(context);
    }
}
