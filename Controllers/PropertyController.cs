using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Controllers
{
    public class PropertyController : Controller
    {
        private readonly HomeLengoContext _context;

        public PropertyController(HomeLengoContext context)
        {
            _context = context;
        }

        // Route handler cho URL /listing (menu chính "Danh sách")
        [Route("listing")]
        public IActionResult IndexListing()
        {
            // Redirect đến trang danh sách tất cả
            return RedirectToAction("Index");
        }

        // Route handler cho slug-based URLs (ví dụ: /listing/can-ho)
        [Route("listing/{slug}")]
        public async Task<IActionResult> IndexBySlug(string slug)
        {
            // Map slug sang PropertyType
            var slugToPropertyTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "can-ho", "Căn Hộ" },
                { "can_ho", "Căn Hộ" },
                { "nha-pho", "Nhà Phố" },
                { "nha_pho", "Nhà Phố" },
                { "biet-thu", "Biệt Thự" },
                { "biet_thu", "Biệt Thự" },
                { "van-phong", "Văn Phòng" },
                { "van_phong", "Văn Phòng" },
                { "phong-tro", "Phòng Trọ" },
                { "phong_tro", "Phòng Trọ" },
                { "chung-cu-mini", "Chung Cư Mini" },
                { "chung_cu_mini", "Chung Cư Mini" },
                { "chung-cu", "Chung Cư Mini" },
            };

            // Tìm PropertyType từ slug
            if (slugToPropertyTypeMap.TryGetValue(slug, out var propertyTypeName))
            {
                var propertyType = await _context.PropertyTypes
                    .FirstOrDefaultAsync(pt => pt.Name.Equals(propertyTypeName, StringComparison.OrdinalIgnoreCase));
                
                if (propertyType != null)
                {
                    // Redirect đến URL với query string
                    return RedirectToAction("Index", new { propertyTypeId = propertyType.PropertyTypeId });
                }
            }

            // Nếu không tìm thấy, redirect về trang danh sách tất cả
            return RedirectToAction("Index");
        }

        [Route("Property")]
        [Route("Property/Index")]
        public async Task<IActionResult> Index(
            int? propertyTypeId,
            int? statusId,
            string? keyword,
            string? location,
            int? bedrooms,
            int? bathrooms,
            decimal? minPrice,
            decimal? maxPrice,
            decimal? minSize,
            decimal? maxSize,
            int[]? amenities,
            string? sortBy = "default",
            int page = 1,
            int pageSize = 10)
        {
            // Lấy danh sách property types để hiển thị trong filter
            var propertyTypes = await _context.PropertyTypes.OrderBy(pt => pt.Name).ToListAsync();
            ViewBag.PropertyTypes = propertyTypes;

            // Lấy danh sách amenities để hiển thị trong filter
            var allAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.Amenities = allAmenities;

            // Query properties với các includes cần thiết
            IQueryable<Property> query = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.Neighborhood)
                .Include(p => p.PropertyPhotos)
                .Include(p => p.PropertyFeatures)
                .Include(p => p.PropertyAmenities)
                    .ThenInclude(pa => pa.Amenity)
                .Include(p => p.Agent)
                    .ThenInclude(a => a.User);

            // Filter theo StatusId (Bán = 1, Cho Thuê = 2)
            // Mặc định hiển thị Cho Thuê (StatusId = 2) nếu không có statusId
            if (statusId.HasValue && (statusId.Value == 1 || statusId.Value == 2))
            {
                // Đảo ngược: statusId từ form (1=Cho Thuê, 2=Bán) sang database (1=Bán, 2=Cho Thuê)
                int dbStatusId = statusId.Value == 1 ? 2 : 1;
                query = query.Where(p => p.StatusId == dbStatusId);
            }
            else
            {
                // Mặc định hiển thị Cho Thuê (StatusId = 2 trong database)
                query = query.Where(p => p.StatusId == 2);
            }

            // Filter theo keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword) || 
                    (p.Description != null && p.Description.Contains(keyword)));
            }

            // Filter theo location
            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(p => 
                    (p.Address != null && p.Address.Contains(location)) ||
                    (p.Neighborhood != null && p.Neighborhood.Name.Contains(location)) ||
                    (p.District != null && p.District.Name.Contains(location)) ||
                    (p.City != null && p.City.Name.Contains(location)));
            }

            // Filter theo propertyTypeId
            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
            {
                query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);
                ViewBag.SelectedPropertyTypeId = propertyTypeId.Value;
                var selectedType = propertyTypes.FirstOrDefault(pt => pt.PropertyTypeId == propertyTypeId.Value);
                ViewBag.SelectedPropertyTypeName = selectedType?.Name ?? "Tất cả";
            }
            else
            {
                ViewBag.SelectedPropertyTypeId = null;
                ViewBag.SelectedPropertyTypeName = "Tất cả";
            }

            // Filter theo bedrooms
            if (bedrooms.HasValue && bedrooms.Value > 0)
            {
                query = query.Where(p => p.Bedrooms.HasValue && p.Bedrooms.Value >= bedrooms.Value);
            }

            // Filter theo bathrooms
            if (bathrooms.HasValue && bathrooms.Value > 0)
            {
                query = query.Where(p => p.Bathrooms.HasValue && p.Bathrooms.Value >= bathrooms.Value);
            }

            // Filter theo price range - chỉ áp dụng khi có giá trị từ query string
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Filter theo size range - chỉ áp dụng khi có giá trị từ query string
            if (minSize.HasValue)
            {
                query = query.Where(p => p.Area.HasValue && p.Area.Value >= minSize.Value);
            }
            if (maxSize.HasValue)
            {
                query = query.Where(p => p.Area.HasValue && p.Area.Value <= maxSize.Value);
            }

            // Filter theo amenities
            if (amenities != null && amenities.Length > 0)
            {
                // Lọc properties có tất cả các amenities được chọn
                foreach (var amenityId in amenities)
                {
                    query = query.Where(p => p.PropertyAmenities.Any(pa => pa.AmenityId == amenityId));
                }
            }

            // Sắp xếp
            switch (sortBy?.ToLower())
            {
                case "newest":
                    query = query.OrderByDescending(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MinValue)
                        .ThenByDescending(p => p.PropertyId);
                    break;
                case "oldest":
                    query = query.OrderBy(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MaxValue)
                        .ThenBy(p => p.PropertyId);
                    break;
                case "price-low":
                    query = query.OrderBy(p => p.Price).ThenByDescending(p => p.PropertyId);
                    break;
                case "price-high":
                    query = query.OrderByDescending(p => p.Price).ThenByDescending(p => p.PropertyId);
                    break;
                default: // "default"
                    query = query.OrderByDescending(p => p.IsFeatured == true)
                        .ThenByDescending(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MinValue)
                        .ThenByDescending(p => p.PropertyId);
                    break;
            }

            // Lấy tổng số properties trước khi phân trang
            var totalCount = await query.CountAsync();
            ViewBag.TotalCount = totalCount;

            // Phân trang
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;

            // Lấy properties cho trang hiện tại
            var properties = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy latest properties cho sidebar (Cho Thuê - StatusId = 2)
            var latestProperties = await _context.Properties
                .Include(p => p.PropertyPhotos)
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Where(p => p.StatusId == 2)
                .OrderByDescending(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MinValue)
                .ThenByDescending(p => p.IsFeatured == true)
                .Take(5)
                .ToListAsync();

            ViewBag.LatestProperties = latestProperties;

            // Lưu các filter values để hiển thị lại
            ViewBag.Keyword = keyword;
            ViewBag.Location = location;
            ViewBag.StatusId = statusId;
            ViewBag.Bedrooms = bedrooms;
            ViewBag.Bathrooms = bathrooms;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinSize = minSize;
            ViewBag.MaxSize = maxSize;
            ViewBag.SelectedAmenities = amenities ?? Array.Empty<int>();

            return View(properties);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Status)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.Neighborhood)
                .Include(p => p.Agent)
                    .ThenInclude(a => a.User)
                .Include(p => p.PropertyPhotos)
                .Include(p => p.PropertyFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.PropertyAmenities)
                    .ThenInclude(pa => pa.Amenity)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.PropertyVideos)
                .Include(p => p.PropertyFloorPlans)
                .FirstOrDefaultAsync(m => m.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            // Tăng số lượt xem
            if (property.Views.HasValue)
            {
                property.Views++;
            }
            else
            {
                property.Views = 1;
            }
            await _context.SaveChangesAsync();

            // Lấy latest properties để hiển thị trong sidebar (loại trừ property hiện tại)
            // Chỉ lấy properties có StatusId == 2 (Cho Thuê)
            var latestPropertiesQuery = _context.Properties
                .Include(p => p.PropertyPhotos)
                .Where(p => p.PropertyId != id.Value && p.StatusId == 2);
            
            var latestProperties = await latestPropertiesQuery
                .OrderByDescending(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MinValue)
                .ThenByDescending(p => p.IsFeatured == true)
                .ThenByDescending(p => p.PropertyId)
                .Take(5)
                .ToListAsync();

            ViewBag.LatestProperties = latestProperties;

            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int propertyId, string name, string email, byte rating, string? title, string body)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường bắt buộc.";
                return RedirectToAction("Details", new { id = propertyId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn đánh giá hợp lệ (1-5 sao).";
                return RedirectToAction("Details", new { id = propertyId });
            }

            // Kiểm tra property có tồn tại không
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            // Tìm user theo email (nếu có)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Tạo review mới
            var review = new Review
            {
                PropertyId = propertyId,
                UserId = user?.UserId,
                Rating = rating,
                Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                Body = body.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsApproved = false // Mặc định chưa được approve, admin sẽ approve sau
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn đánh giá của bạn! Đánh giá của bạn đang chờ phê duyệt và sẽ được công bố sớm.";
            return RedirectToAction("Details", new { id = propertyId });
        }
    }
}
