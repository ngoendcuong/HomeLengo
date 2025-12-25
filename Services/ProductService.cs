using System.Text.Json;
using HomeLengo.Models;
using Microsoft.EntityFrameworkCore;

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
        userQuery ??= "";
        var q = userQuery.Trim();

        // Query cơ bản (chỉ lấy field cần thiết, AsNoTracking cho nhanh)
        var query = _context.Properties
            .AsNoTracking()
            .Where(p => p.StatusId == 2); // tuỳ bạn: giữ mặc định "Cho thuê" để phù hợp logic listing

        // Nếu user có nhập từ khóa thì lọc thực sự
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                p.Title.Contains(q) ||
                (p.Address != null && p.Address.Contains(q)) ||
                (p.Description != null && p.Description.Contains(q))
            );
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
            .ThenByDescending(p => p.PropertyId)
            .Take(20)
            .Select(p => new
            {
                p.PropertyId,
                p.Title,
                p.Price,
                p.Address,
                p.Area
            })
            .ToListAsync();

        if (items.Count == 0) return "[]";

        return JsonSerializer.Serialize(items, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
