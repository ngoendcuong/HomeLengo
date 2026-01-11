using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class CategoriesController : BaseController
    {
        private readonly HomeLengoContext _context;

        public CategoriesController(HomeLengoContext context)
        {
            _context = context;
        }

        // Trang chính - Danh mục
        public IActionResult Index()
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return View();
        }

        // Loại bất động sản
        public IActionResult PropertyTypes()
        {
            var propertyTypes = _context.PropertyTypes
                .Select(pt => new
                {
                    Id = pt.PropertyTypeId,
                    Name = pt.Name,
                    Slug = pt.Name.ToLower().Replace(" ", "-"),
                    Order = pt.PropertyTypeId,
                    IsActive = true,
                    Icon = pt.IconClass ?? "fa-building"
                })
                .ToList();

            return View(propertyTypes);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePropertyType(PropertyType propertyType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(propertyType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PropertyTypes));
            }
            return View(propertyType);
        }

        [HttpPost]
        public async Task<IActionResult> EditPropertyType(int id, PropertyType propertyType)
        {
            if (id != propertyType.PropertyTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(propertyType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyTypeExists(propertyType.PropertyTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(PropertyTypes));
            }
            return View(propertyType);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePropertyType(int id)
        {
            var propertyType = await _context.PropertyTypes.FindAsync(id);
            if (propertyType != null)
            {
                _context.PropertyTypes.Remove(propertyType);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PropertyTypes));
        }

        // Trạng thái
        public IActionResult PropertyStatus()
        {
            var statuses = _context.PropertyStatuses
                .Select(ps => new
                {
                    Id = ps.StatusId,
                    Name = ps.Name,
                    Slug = ps.Name.ToLower().Replace(" ", "-"),
                    Color = GetStatusColor(ps.Name),
                    IsActive = true
                })
                .ToList();

            return View(statuses);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePropertyStatus(PropertyStatus propertyStatus)
        {
            if (ModelState.IsValid)
            {
                _context.Add(propertyStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PropertyStatus));
            }
            return View(propertyStatus);
        }

        [HttpPost]
        public async Task<IActionResult> EditPropertyStatus(int id, PropertyStatus propertyStatus)
        {
            if (id != propertyStatus.StatusId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(propertyStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyStatusExists(propertyStatus.StatusId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(PropertyStatus));
            }
            return View(propertyStatus);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePropertyStatus(int id)
        {
            var propertyStatus = await _context.PropertyStatuses.FindAsync(id);
            if (propertyStatus != null)
            {
                _context.PropertyStatuses.Remove(propertyStatus);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PropertyStatus));
        }

        // Tiện ích
        public IActionResult Amenities()
        {
            var amenities = _context.Amenities
                .Select(a => new
                {
                    Id = a.AmenityId,
                    Name = a.Name,
                    Slug = a.Name.ToLower().Replace(" ", "-"),
                    Icon = "fa-check", // Có thể thêm IconClass vào Amenity model
                    Order = a.AmenityId,
                    IsActive = true
                })
                .OrderBy(a => a.Order)
                .ToList();

            return View(amenities);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAmenity(Amenity amenity)
        {
            if (ModelState.IsValid)
            {
                _context.Add(amenity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Amenities));
            }
            return View(amenity);
        }

        [HttpPost]
        public async Task<IActionResult> EditAmenity(int id, Amenity amenity)
        {
            if (id != amenity.AmenityId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(amenity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AmenityExists(amenity.AmenityId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Amenities));
            }
            return View(amenity);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity != null)
            {
                _context.Amenities.Remove(amenity);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Amenities));
        }

        private string GetStatusColor(string statusName)
        {
            return statusName.ToLower() switch
            {
                var s when s.Contains("đang bán") || s.Contains("active") => "success",
                var s when s.Contains("cho thuê") || s.Contains("rent") => "info",
                var s when s.Contains("đã bán") || s.Contains("đã cho thuê") || s.Contains("sold") => "secondary",
                var s when s.Contains("chờ duyệt") || s.Contains("pending") => "warning",
                _ => "primary"
            };
        }

        private bool PropertyTypeExists(int id)
        {
            return _context.PropertyTypes.Any(e => e.PropertyTypeId == id);
        }

        private bool PropertyStatusExists(int id)
        {
            return _context.PropertyStatuses.Any(e => e.StatusId == id);
        }

        private bool AmenityExists(int id)
        {
            return _context.Amenities.Any(e => e.AmenityId == id);
        }
    }
}