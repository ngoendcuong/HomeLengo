using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.ViewComponents
{
    public class MenuTopViewComponent : ViewComponent
    {
        private readonly HomeLengoContext _context;

        public MenuTopViewComponent(HomeLengoContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await _context.Menus
                                .Where(m => (bool)m.IsActive)
                                .OrderBy(m => m.SortOrder)
                                .ToListAsync();

            // Lấy danh sách PropertyTypes để map với menu items
            var propertyTypes = await _context.PropertyTypes.ToListAsync();

            // Dictionary để map tên menu với tên PropertyType (hỗ trợ nhiều biến thể)
            // Dựa trên dữ liệu thực tế trong database
            // Lưu ý: StringComparer.OrdinalIgnoreCase sẽ coi "Căn Hộ" và "Căn hộ" là giống nhau
            // nên chỉ cần thêm một biến thể chính, các biến thể khác sẽ được xử lý tự động
            var menuToPropertyTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Căn Hộ", "Căn Hộ" },
                { "can ho", "Căn Hộ" },
                { "Nhà Phố", "Nhà Phố" },
                { "nha pho", "Nhà Phố" },
                { "Biệt Thự", "Biệt Thự" },
                { "biet thu", "Biệt Thự" },
                { "Văn Phòng", "Văn Phòng" },
                { "van phong", "Văn Phòng" },
                { "Phòng Trọ", "Phòng Trọ" },
                { "phong tro", "Phòng Trọ" },
                { "Chung Cư Mini", "Chung Cư Mini" },
                { "chung cu mini", "Chung Cư Mini" },
                { "Chung Cư", "Chung Cư Mini" },
            };

            // Tự động cập nhật URL cho menu items dựa trên PropertyType
            foreach (var item in items)
            {
                // Nếu menu item là "Danh sách" (parent), set URL = /Property
                // Kiểm tra cả "Danh Sách" và "Danh sách" để tương thích
                if (item.ParentId == null && (item.Title.Equals("Danh sách", StringComparison.OrdinalIgnoreCase) || 
                    item.Title.Equals("Danh Sách", StringComparison.OrdinalIgnoreCase) ||
                    item.Title.Contains("Danh sách", StringComparison.OrdinalIgnoreCase)))
                {
                    item.Url = "/Property";
                }
                // Nếu menu item là "Bảng Điều Khiển" (parent), set URL = /Admin/Home
                else if (item.ParentId == null && (item.Title.Equals("Bảng Điều Khiển", StringComparison.OrdinalIgnoreCase) || 
                    item.Title.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) ||
                    item.Title.Contains("Bảng Điều Khiển", StringComparison.OrdinalIgnoreCase) ||
                    item.Title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase)))
                {
                    item.Url = "/Admin/Home";
                }
                // Nếu là menu con (có ParentId), tìm PropertyType tương ứng và set URL
                else if (item.ParentId != null)
                {
                    // Kiểm tra xem parent có phải là "Danh sách" không
                    var parentMenu = items.FirstOrDefault(m => m.MenuId == item.ParentId);
                    bool isDanhSachChild = parentMenu != null && 
                        (parentMenu.Title.Equals("Danh sách", StringComparison.OrdinalIgnoreCase) || 
                         parentMenu.Title.Equals("Danh Sách", StringComparison.OrdinalIgnoreCase) ||
                         parentMenu.Title.Contains("Danh sách", StringComparison.OrdinalIgnoreCase));

                    if (isDanhSachChild)
                    {
                        PropertyType? matchingPropertyType = null;

                        // Thử tìm bằng dictionary mapping trước
                        if (menuToPropertyTypeMap.TryGetValue(item.Title, out var mappedName))
                        {
                            matchingPropertyType = propertyTypes.FirstOrDefault(pt => 
                                pt.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
                        }

                        // Nếu không tìm thấy, thử tìm trực tiếp
                        if (matchingPropertyType == null)
                        {
                            matchingPropertyType = propertyTypes.FirstOrDefault(pt => 
                                pt.Name.Equals(item.Title, StringComparison.OrdinalIgnoreCase) ||
                                item.Title.Contains(pt.Name, StringComparison.OrdinalIgnoreCase) ||
                                pt.Name.Contains(item.Title, StringComparison.OrdinalIgnoreCase));
                        }

                        // Nếu vẫn không tìm thấy, thử tìm bằng cách loại bỏ dấu và so sánh
                        if (matchingPropertyType == null)
                        {
                            var itemTitleNormalized = RemoveVietnameseDiacritics(item.Title);
                            matchingPropertyType = propertyTypes.FirstOrDefault(pt =>
                            {
                                var ptNameNormalized = RemoveVietnameseDiacritics(pt.Name);
                                return ptNameNormalized.Equals(itemTitleNormalized, StringComparison.OrdinalIgnoreCase) ||
                                       itemTitleNormalized.Contains(ptNameNormalized, StringComparison.OrdinalIgnoreCase) ||
                                       ptNameNormalized.Contains(itemTitleNormalized, StringComparison.OrdinalIgnoreCase);
                            });
                        }

                        if (matchingPropertyType != null)
                        {
                            // Luôn override URL để đảm bảo dùng format mới
                            item.Url = $"/Property?propertyTypeId={matchingPropertyType.PropertyTypeId}";
                        }
                    }
                    // Xử lý menu con của "Bảng Điều Khiển" (Dashboard)
                    else
                    {
                        var dashboardParent = items.FirstOrDefault(m => m.MenuId == item.ParentId);
                        bool isDashboardChild = dashboardParent != null && 
                            (dashboardParent.Title.Equals("Bảng Điều Khiển", StringComparison.OrdinalIgnoreCase) || 
                             dashboardParent.Title.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) ||
                             dashboardParent.Title.Contains("Bảng Điều Khiển", StringComparison.OrdinalIgnoreCase) ||
                             dashboardParent.Title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase));

                        // Nếu URL hiện tại bắt đầu bằng /dashboard/, cũng coi là menu con của Dashboard
                        bool hasDashboardUrl = !string.IsNullOrEmpty(item.Url) && 
                            (item.Url.StartsWith("/dashboard/", StringComparison.OrdinalIgnoreCase) ||
                             item.Url.StartsWith("dashboard/", StringComparison.OrdinalIgnoreCase));

                        if (isDashboardChild || hasDashboardUrl)
                        {
                            // Map các menu con của Dashboard với URL tương ứng
                            var dashboardMenuMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "Bất Động Sản Của Tôi", "/Admin/Property/MyProperties" },
                                { "My Properties", "/Admin/Property/MyProperties" },
                                { "bds-cua-toi", "/Admin/Property/MyProperties" },
                                { "Tin Nhắn", "/Admin/Message" },
                                { "Message", "/Admin/Message" },
                                { "Messages", "/Admin/Message" },
                                { "tin-nhan", "/Admin/Message" },
                                { "Yêu Thích", "/Admin/Favorite" },
                                { "My Favorites", "/Admin/Favorite" },
                                { "Favorites", "/Admin/Favorite" },
                                { "yeu-thich", "/Admin/Favorite" },
                                { "Đánh Giá Gần Đây", "/Admin/Review" },
                                { "Recent Reviews", "/Admin/Review" },
                                { "Reviews", "/Admin/Review" },
                                { "danh-gia-gan-day", "/Admin/Review" },
                                { "Hồ Sơ Của Tôi", "/Admin/User/Profile" },
                                { "My Profile", "/Admin/User/Profile" },
                                { "Profile", "/Admin/User/Profile" },
                                { "ho-so-cua-toi", "/Admin/User/Profile" },
                                { "Thêm Bất Động Sản Mới", "/Admin/Property/Create" },
                                { "Add New Property", "/Admin/Property/Create" },
                                { "Add Property", "/Admin/Property/Create" },
                                { "them-bds-moi", "/Admin/Property/Create" }
                            };

                            // Thử tìm theo Title trước
                            bool mapped = false;
                            if (dashboardMenuMap.TryGetValue(item.Title, out var dashboardUrl))
                            {
                                item.Url = dashboardUrl;
                                mapped = true;
                                System.Diagnostics.Debug.WriteLine($"Dashboard menu mapped by title: '{item.Title}' -> '{dashboardUrl}'");
                            }
                            // Nếu không tìm thấy theo Title, thử tìm theo URL slug
                            if (!mapped && hasDashboardUrl && !string.IsNullOrEmpty(item.Url))
                            {
                                var urlSlug = item.Url.Replace("/dashboard/", "").Replace("dashboard/", "").Trim('/');
                                if (dashboardMenuMap.TryGetValue(urlSlug, out var urlFromSlug))
                                {
                                    item.Url = urlFromSlug;
                                    mapped = true;
                                    System.Diagnostics.Debug.WriteLine($"Dashboard menu mapped by slug: '{urlSlug}' -> '{urlFromSlug}'");
                                }
                            }
                            
                            // Fallback: Map trực tiếp dựa trên slug pattern
                            if (!mapped && hasDashboardUrl && !string.IsNullOrEmpty(item.Url))
                            {
                                var urlSlug = item.Url.Replace("/dashboard/", "").Replace("dashboard/", "").Trim('/').ToLower();
                                
                                // Map các slug phổ biến
                                if (urlSlug.Contains("bds-cua-toi") || urlSlug.Contains("my-properties") || urlSlug.Contains("property"))
                                {
                                    item.Url = "/Admin/Property/MyProperties";
                                    mapped = true;
                                }
                                else if (urlSlug.Contains("tin-nhan") || urlSlug.Contains("message"))
                                {
                                    item.Url = "/Admin/Message";
                                    mapped = true;
                                }
                                else if (urlSlug.Contains("yeu-thich") || urlSlug.Contains("favorite"))
                                {
                                    item.Url = "/Admin/Favorite";
                                    mapped = true;
                                }
                                else if (urlSlug.Contains("danh-gia") || urlSlug.Contains("review"))
                                {
                                    item.Url = "/Admin/Review";
                                    mapped = true;
                                }
                                else if (urlSlug.Contains("ho-so") || urlSlug.Contains("profile"))
                                {
                                    item.Url = "/Admin/User/Profile";
                                    mapped = true;
                                }
                                else if (urlSlug.Contains("them-bds") || urlSlug.Contains("add-property"))
                                {
                                    item.Url = "/Admin/Property/Create";
                                    mapped = true;
                                }
                                
                                if (mapped)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Dashboard menu mapped by pattern: '{urlSlug}' -> '{item.Url}'");
                                }
                            }
                            
                            if (!mapped)
                            {
                                System.Diagnostics.Debug.WriteLine($"Dashboard menu NOT mapped: Title='{item.Title}', Parent='{dashboardParent?.Title}', URL='{item.Url}'");
                            }
                        }
                    }
                }
            }

            // Debug: Log menu items để kiểm tra
            System.Diagnostics.Debug.WriteLine($"=== Menu Debug ===");
            System.Diagnostics.Debug.WriteLine($"Menu items count: {items.Count}");
            System.Diagnostics.Debug.WriteLine($"PropertyTypes count: {propertyTypes.Count}");
            foreach (var pt in propertyTypes)
            {
                System.Diagnostics.Debug.WriteLine($"PropertyType: {pt.PropertyTypeId} - {pt.Name}");
            }
            foreach (var item in items)
            {
                System.Diagnostics.Debug.WriteLine($"Menu: ID={item.MenuId}, Title='{item.Title}', URL='{item.Url}', ParentId={item.ParentId}");
            }
            System.Diagnostics.Debug.WriteLine($"=== End Menu Debug ===");

            return View(items);
        }

        private string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var result = new System.Text.StringBuilder();
            
            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }
            
            return result.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
