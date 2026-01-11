// Areas/Admin/Controllers/PropertiesController.cs
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

        public IActionResult Index(string searchString, int? propertyTypeId, int? statusId)
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Tìm trạng thái "Chờ duyệt"
            var pendingStatus = _context.PropertyStatuses
                .ToList()
                .FirstOrDefault(s => s.Name.Contains("Chờ duyệt") || s.Name.Contains("Pending") || s.Name.Contains("Chờ") || s.Name.ToLower().Contains("pending"));
            
            var query = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.PropertyPhotos)
                .AsQueryable();

            // Tìm kiếm theo tiêu đề
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Title.Contains(searchString) || 
                                        (p.Description != null && p.Description.Contains(searchString)));
            }

            // Lọc theo loại bất động sản
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
                // Mặc định chỉ hiển thị những tin đã được duyệt (không phải "Chờ duyệt")
                // Chỉ khi không có filter statusId nào được chọn
                if (pendingStatus != null)
                {
                    query = query.Where(p => p.StatusId != pendingStatus.StatusId);
                }
            }

            // Materialize query và tạo PropertyViewModel
            var properties = query
                .ToList()
                .Select(p => 
                {
                    var primaryPhoto = p.PropertyPhotos?.Where(pp => pp.IsPrimary == true).FirstOrDefault();
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
                })
                .ToList();

            // Lấy danh sách PropertyTypes và Statuses cho dropdown
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", propertyTypeId);
            var allStatuses = _context.PropertyStatuses.ToList();
            ViewBag.PropertyStatuses = new SelectList(allStatuses, "StatusId", "Name", statusId);
            ViewBag.SearchString = searchString;
            
            // Lưu StatusId của "Chờ duyệt" để hiển thị nút
            // Tìm với nhiều cách khác nhau
            var foundPendingStatus = allStatuses.FirstOrDefault(s => 
                !string.IsNullOrEmpty(s.Name) && (
                    s.Name.Contains("Chờ duyệt") || 
                    s.Name.Contains("Pending") || 
                    s.Name.ToLower().Contains("pending") ||
                    s.Name.ToLower().Contains("waiting") ||
                    s.Name.ToLower().Contains("chờ duyệt")
                ));
            
            // Nếu vẫn không tìm thấy, tìm bất kỳ status nào có chứa "chờ"
            if (foundPendingStatus == null)
            {
                foundPendingStatus = allStatuses.FirstOrDefault(s => 
                    !string.IsNullOrEmpty(s.Name) && s.Name.ToLower().Contains("chờ"));
            }
            
            ViewBag.PendingStatusId = foundPendingStatus?.StatusId;

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

        [HttpGet]
        public async Task<IActionResult> GetPropertyDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.Neighborhood)
                .Include(p => p.Agent)
                    .ThenInclude(a => a.User)
                .Include(p => p.PropertyPhotos.Where(pp => pp.IsPrimary == true))
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

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
                Image = property.PropertyPhotos.Where(pp => pp.IsPrimary == true).FirstOrDefault() != null
                    ? property.PropertyPhotos.Where(pp => pp.IsPrimary == true).First().FilePath
                    : "/assets/images/banner/banner-property-12.jpg",
                CreatedAt = property.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
            };

            return Json(propertyDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PropertyId == id);
            
            if (property == null)
            {
                return NotFound();
            }

            // Kiểm tra xem property có đang ở trạng thái "Chờ duyệt" không
            var pendingStatus = await _context.PropertyStatuses
                .FirstOrDefaultAsync(s => s.Name.Contains("Chờ duyệt") || s.Name.Contains("Pending") || s.Name.Contains("Chờ"));
            
            if (pendingStatus != null && property.StatusId == pendingStatus.StatusId)
            {
                // Tìm trạng thái "Đang bán" hoặc "Cho thuê" để chuyển sang
                // Ưu tiên tìm "Đang bán" (StatusId = 1), nếu không có thì tìm "Cho thuê" (StatusId = 2)
                var activeStatus = await _context.PropertyStatuses
                    .FirstOrDefaultAsync(s => s.StatusId == 1 || s.Name.Contains("Đang bán") || s.Name.Contains("Rao bán"));
                
                if (activeStatus == null)
                {
                    activeStatus = await _context.PropertyStatuses
                        .FirstOrDefaultAsync(s => s.StatusId == 2 || s.Name.Contains("Cho thuê") || s.Name.Contains("For rent"));
                }
                
                if (activeStatus != null)
                {
                    property.StatusId = activeStatus.StatusId;
                    // Sau khi duyệt, set IsFeatured = true
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
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}