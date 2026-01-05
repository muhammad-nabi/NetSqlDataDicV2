using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DataDictionary.AspNetCore.Configuration;
using DataDictionary.AspNetCore.Models;

namespace DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;

public class HomeController : DataDictionaryControllerBase
{
    public HomeController(DataDictionaryOptions options) : base(options)
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
