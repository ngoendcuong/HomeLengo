// Areas/RealEstateAdmin/Controllers/PropertiesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class PropertiesController : BaseController
    {
        private readonly HomeLengoContext _context;

        public PropertiesController(HomeLengoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? propertyTypeId, int? statusId)
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // ✅ Tìm trạng thái "Chờ duyệt" (Pending) 1 lần
            var pendingStatus = await _context.PropertyStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Name != null &&
                    (
                        s.Name.ToLower().Contains("chờ duyệt") ||
                        s.Name.ToLower().Contains("pending") ||
                        s.Name.ToLower().Contains("chờ")
                    )
                );

            var pendingStatusId = pendingStatus?.StatusId ?? 0;
            ViewBag.PendingStatusId = pendingStatusId;

            // Query properties
            var query = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.PropertyPhotos)
                .AsQueryable();

            // Tìm kiếm theo tiêu đề / mô tả
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(p =>
                    (p.Title != null && p.Title.Contains(searchString)) ||
                    (p.Description != null && p.Description.Contains(searchString))
                );
            }

            // Lọc theo loại BĐS
            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
            {
                query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            // Lọc theo trạng thái
            if (statusId.HasValue && statusId.Value > 0)
            {
                query = query.Where(p => p.StatusId == statusId.Value);
            }
            else
            {
                // ✅ Mặc định ẩn tin "Chờ duyệt" nếu không filter statusId
                if (pendingStatusId > 0)
                {
                    query = query.Where(p => p.StatusId != pendingStatusId);
                }
            }

            // ✅ Lấy dữ liệu async
            var list = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Map sang PropertyViewModel
            var properties = list.Select(p =>
            {
                var primaryPhoto = p.PropertyPhotos?
                    .FirstOrDefault(pp => pp.IsPrimary == true);

                return new PropertyViewModel
                {
                    Id = p.PropertyId,
                    Title = p.Title ?? "N/A",
                    Type = p.PropertyType?.Name ?? "N/A",
                    Price = p.Price,
                    Currency = p.Currency ?? "VNĐ",
                    Status = p.Status?.Name ?? "N/A",
                    IsVip = p.IsFeatured ?? false,
                    Image = primaryPhoto?.FilePath ?? "/assets/images/banner/banner-property-12.jpg"
                };
            }).ToList();

            // Dropdown filters
            var propertyTypes = await _context.PropertyTypes.AsNoTracking().ToListAsync();
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "PropertyTypeId", "Name", propertyTypeId);

            var allStatuses = await _context.PropertyStatuses.AsNoTracking().ToListAsync();
            ViewBag.PropertyStatuses = new SelectList(allStatuses, "StatusId", "Name", statusId);

            ViewBag.SearchString = searchString;

            return View(properties);
        }

        public IActionResult Create()
        {
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name");
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name");
            ViewBag.Agents = new SelectList(
                _context.Agents
                    .Include(a => a.User)
                    .Select(a => new
                    {
                        AgentId = a.AgentId,
                        Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A"
                    }),
                "AgentId", "Name"
            );
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
            ViewBag.Agents = new SelectList(
                _context.Agents
                    .Include(a => a.User)
                    .Select(a => new
                    {
                        AgentId = a.AgentId,
                        Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A"
                    }),
                "AgentId", "Name", property.AgentId
            );
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);

            return View(property);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null) return NotFound();

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Agents = new SelectList(
                _context.Agents
                    .Include(a => a.User)
                    .Select(a => new
                    {
                        AgentId = a.AgentId,
                        Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A"
                    }),
                "AgentId", "Name", property.AgentId
            );
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.PropertyId = id;

            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Property property)
        {
            if (id != property.PropertyId) return NotFound();

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
                    if (!PropertyExists(property.PropertyId)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.PropertyStatuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Agents = new SelectList(
                _context.Agents
                    .Include(a => a.User)
                    .Select(a => new
                    {
                        AgentId = a.AgentId,
                        Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A"
                    }),
                "AgentId", "Name", property.AgentId
            );
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);

            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> GetPropertyDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.Neighborhood)
                .Include(p => p.Agent).ThenInclude(a => a.User)
                .Include(p => p.PropertyPhotos)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null) return NotFound();

            var primary = property.PropertyPhotos?.FirstOrDefault(pp => pp.IsPrimary == true);

            var propertyDetails = new
            {
                Id = property.PropertyId,
                Title = property.Title,
                Description = property.Description ?? "Không có mô tả",
                Type = property.PropertyType?.Name ?? "N/A",
                Price = property.Price,
                Currency = property.Currency ?? "VNĐ",
                Status = property.Status?.Name ?? "N/A",
                Area = property.Area,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                LotSize = property.LotSize,
                Address = property.Address ?? "N/A",
                City = property.City?.Name ?? "N/A",
                District = property.District?.Name ?? "N/A",
                Neighborhood = property.Neighborhood?.Name ?? "N/A",
                AgentName = property.Agent?.User != null
                    ? (property.Agent.User.FullName ?? property.Agent.User.Username)
                    : "N/A",
                IsVip = property.IsFeatured ?? false,
                Views = property.Views ?? 0,
                Image = primary?.FilePath ?? "/assets/images/banner/banner-property-12.jpg",
                CreatedAt = property.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
            };

            return Json(propertyDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == id);
            if (property == null) return NotFound();

            // Tìm status "Chờ duyệt"
            var pendingStatus = await _context.PropertyStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Name != null &&
                    (s.Name.ToLower().Contains("chờ duyệt") || s.Name.ToLower().Contains("pending") || s.Name.ToLower().Contains("chờ"))
                );

            if (pendingStatus != null && property.StatusId == pendingStatus.StatusId)
            {
                // ✅ Ưu tiên chuyển sang "Đang bán/Rao bán" -> nếu không có thì "Cho thuê"
                var activeStatus = await _context.PropertyStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.Name != null &&
                        (s.Name.ToLower().Contains("đang bán") || s.Name.ToLower().Contains("rao bán") || s.Name.ToLower().Contains("for sale"))
                    );

                if (activeStatus == null)
                {
                    activeStatus = await _context.PropertyStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s =>
                            s.Name != null &&
                            (s.Name.ToLower().Contains("cho thuê") || s.Name.ToLower().Contains("for rent"))
                        );
                }

                if (activeStatus != null)
                {
                    property.StatusId = activeStatus.StatusId;
                    property.IsFeatured = true;
                    property.ModifiedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã duyệt bất động sản thành công!";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bất động sản!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}
