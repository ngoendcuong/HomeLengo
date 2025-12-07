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
                // Nếu là menu con (có ParentId), tìm PropertyType tương ứng và set URL
                else if (item.ParentId != null)
                {
                    // Kiểm tra xem parent có phải là "Danh sách" không
                    var parent = items.FirstOrDefault(m => m.MenuId == item.ParentId);
                    bool isDanhSachChild = parent != null && 
                        (parent.Title.Equals("Danh sách", StringComparison.OrdinalIgnoreCase) || 
                         parent.Title.Equals("Danh Sách", StringComparison.OrdinalIgnoreCase) ||
                         parent.Title.Contains("Danh sách", StringComparison.OrdinalIgnoreCase));

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
