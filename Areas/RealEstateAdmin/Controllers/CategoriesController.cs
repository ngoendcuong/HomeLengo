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

        // Helper redirect hard-core (đúng area)
        private IActionResult RedirectToAdminHome()
            => RedirectToAction("Index", "Home", new { area = "" });

        private IActionResult RedirectToPropertyTypes()
            => RedirectToAction("PropertyTypes", "Categories", new { area = "RealEstateAdmin" });

        private IActionResult RedirectToPropertyStatus()
            => RedirectToAction("PropertyStatus", "Categories", new { area = "RealEstateAdmin" });

        private IActionResult RedirectToAmenities()
            => RedirectToAction("Amenities", "Categories", new { area = "RealEstateAdmin" });

        private bool EnsureAdmin()
        {
            return IsAdmin();
        }

        // Trang chính - Danh mục
        public IActionResult Index()
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();
            return View();
        }

        // Loại bất động sản
        public IActionResult PropertyTypes()
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePropertyType(PropertyType propertyType)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (ModelState.IsValid)
            {
                _context.Add(propertyType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm loại bất động sản!";
                return RedirectToPropertyTypes();
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ, vui lòng kiểm tra lại!";
            return RedirectToPropertyTypes();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPropertyType(int id, PropertyType propertyType)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (id != propertyType.PropertyTypeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(propertyType);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật loại bất động sản!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyTypeExists(propertyType.PropertyTypeId)) return NotFound();
                    throw;
                }

                return RedirectToPropertyTypes();
            }

            TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ!";
            return RedirectToPropertyTypes();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePropertyType(int id)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var propertyType = await _context.PropertyTypes.FindAsync(id);
            if (propertyType != null)
            {
                _context.PropertyTypes.Remove(propertyType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa loại bất động sản!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại bất động sản để xóa!";
            }

            return RedirectToPropertyTypes();
        }

        // Trạng thái
        public IActionResult PropertyStatus()
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var statuses = _context.PropertyStatuses
                .ToList()
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePropertyStatus(PropertyStatus propertyStatus)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (ModelState.IsValid)
            {
                _context.Add(propertyStatus);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm trạng thái!";
                return RedirectToPropertyStatus();
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ, vui lòng kiểm tra lại!";
            return RedirectToPropertyStatus();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPropertyStatus(int id, PropertyStatus propertyStatus)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (id != propertyStatus.StatusId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(propertyStatus);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyStatusExists(propertyStatus.StatusId)) return NotFound();
                    throw;
                }

                return RedirectToPropertyStatus();
            }

            TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ!";
            return RedirectToPropertyStatus();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePropertyStatus(int id)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var propertyStatus = await _context.PropertyStatuses.FindAsync(id);
            if (propertyStatus != null)
            {
                _context.PropertyStatuses.Remove(propertyStatus);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa trạng thái!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạng thái để xóa!";
            }

            return RedirectToPropertyStatus();
        }

        // Tiện ích
        public IActionResult Amenities()
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var amenities = _context.Amenities
                .Select(a => new
                {
                    Id = a.AmenityId,
                    Name = a.Name,
                    Slug = a.Name.ToLower().Replace(" ", "-"),
                    Icon = "fa-check",
                    Order = a.AmenityId,
                    IsActive = true
                })
                .OrderBy(a => a.Order)
                .ToList();

            return View(amenities);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAmenity(string Name)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["ErrorMessage"] = "Tên tiện ích không được để trống!";
                return RedirectToAmenities();
            }

            var amenity = new Amenity { Name = Name.Trim() };
            _context.Add(amenity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm tiện ích thành công!";
            return RedirectToAmenities();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAmenity(int id, Amenity amenity)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (id != amenity.AmenityId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(amenity);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật tiện ích!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AmenityExists(amenity.AmenityId)) return NotFound();
                    throw;
                }

                return RedirectToAmenities();
            }

            TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ!";
            return RedirectToAmenities();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity != null)
            {
                _context.Amenities.Remove(amenity);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tiện ích!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiện ích để xóa!";
            }

            return RedirectToAmenities();
        }

        private static string GetStatusColor(string statusName)
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

        private bool PropertyTypeExists(int id) => _context.PropertyTypes.Any(e => e.PropertyTypeId == id);
        private bool PropertyStatusExists(int id) => _context.PropertyStatuses.Any(e => e.StatusId == id);
        private bool AmenityExists(int id) => _context.Amenities.Any(e => e.AmenityId == id);
    }
}
