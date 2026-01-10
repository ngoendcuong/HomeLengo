using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeLengo.Models;
using Microsoft.EntityFrameworkCore;

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
        // PropertyTypes (GIỮ NGUYÊN)
        var propertyTypes = _context.PropertyTypes
            .Select(pt => new
            {
                pt.PropertyTypeId,
                pt.Name,
                pt.IconClass,
                PropertyCount = _context.Properties.Count(p => p.PropertyTypeId == pt.PropertyTypeId)
            }).ToList();

        // ===== CITIES =====
        var cities = _context.Cities
            .Select(c => new
            {
                c.CityId,
                c.Name,
                ImageUrl = c.ImageUrl,
                PropertyCount = _context.Properties.Count(p => p.CityId == c.CityId),
            }).ToList();

        // ===== REVIEWS =====
        var reviews = _context.Reviews
      .Where(r => r.IsApproved == false)
      .OrderByDescending(r => r.CreatedAt)
      .Take(6)
      .ToList();



        ViewBag.PropertyTypes = propertyTypes;
        ViewBag.Cities = cities;
        ViewBag.Reviews = reviews;

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
