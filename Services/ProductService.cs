using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using HomeLengo.Models; 

public interface IProductService
{
    Task<string> GetRelevantProductsAsText(string userQuery);
}

public class ProductService : IProductService
{
    private readonly HomeLengoContext _context;

    public ProductService(HomeLengoContext context)
    {
        _context = context;
    }

    public async Task<string> GetRelevantProductsAsText(string userQuery)
    {
        // 1. Tách từ khóa đơn giản từ câu hỏi của user (Ví dụ: "giá iphone")
        // Ở đây làm đơn giản, thực tế có thể dùng Full-Text Search
        var keywords = userQuery.ToLower().Split(' ');

        // 2. Query DB: Lấy sản phẩm có tên hoặc mô tả chứa từ khóa
        var query = _context.Properties.AsQueryable();

        // Logic lọc đơn giản: Lấy 10 sản phẩm mới nhất (hoặc logic tìm kiếm của bạn)
        // Lưu ý: Gemini 1.5 Flash có thể đọc hàng nghìn dòng, nhưng lọc trước vẫn tốt hơn.
        var products = await query
            .Take(20)
            .Select(p => new { p.Title, p.Price, p.Address, p.Description }) // Chỉ lấy trường cần thiết
            .ToListAsync();

        if (!products.Any()) return "Không tìm thấy dữ liệu sản phẩm nào.";

        // 3. Serialize thành JSON gọn nhẹ
        return JsonConvert.SerializeObject(products);
    }
}