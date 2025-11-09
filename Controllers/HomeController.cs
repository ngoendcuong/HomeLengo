using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeLengo.Models;

namespace HomeLengo.Controllers;

public class HomeController : Controller
{
    private readonly HomeLengoContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(HomeLengoContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var propertyTypes = _context.PropertyTypes
    .Select(pt => new
    {
        pt.PropertyTypeId,
        pt.Name,
        pt.IconClass,
        PropertyCount = _context.Properties.Count(p => p.PropertyTypeId == pt.PropertyTypeId)
    }).ToList();

        ViewBag.PropertyTypes = propertyTypes;
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
