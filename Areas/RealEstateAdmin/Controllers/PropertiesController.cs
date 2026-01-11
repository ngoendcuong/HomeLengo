// Areas/Admin/Controllers/PropertiesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class PropertiesController : Controller
    {
        private readonly HomeLengoContext _context;

        public PropertiesController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var properties = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.PropertyPhotos.Where(pp => pp.IsPrimary == true))
                .Select(p => new
                {
                    Id = p.PropertyId,
                    Title = p.Title,
                    Type = p.PropertyType.Name,
                    Price = p.Price,
                    Status = p.Status.Name,
                    IsVip = p.IsFeatured ?? false,
                    Image = p.PropertyPhotos.Where(pp => pp.IsPrimary == true).FirstOrDefault() != null 
                        ? p.PropertyPhotos.Where(pp => pp.IsPrimary == true).First().FilePath 
                        : "https://via.placeholder.com/100x80"
                })
                .ToList();

            return View(properties);
        }

        public IActionResult Create()
        {
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name");
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name");
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User).Select(a => new { 
                AgentId = a.AgentId, 
                Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A" 
            }), "AgentId", "Name");
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property)
        {
            if (ModelState.IsValid)
            {
                property.CreatedAt = DateTime.UtcNow;
                property.ModifiedAt = DateTime.UtcNow;
                _context.Add(property);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User).Select(a => new { 
                AgentId = a.AgentId, 
                Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A" 
            }), "AgentId", "Name", property.AgentId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            return View(property);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PropertyId == id);
            
            if (property == null)
            {
                return NotFound();
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User).Select(a => new { 
                AgentId = a.AgentId, 
                Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A" 
            }), "AgentId", "Name", property.AgentId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.PropertyId = id;
            
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Property property)
        {
            if (id != property.PropertyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    property.ModifiedAt = DateTime.UtcNow;
                    _context.Update(property);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User).Select(a => new { 
                AgentId = a.AgentId, 
                Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A" 
            }), "AgentId", "Name", property.AgentId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            return View(property);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}