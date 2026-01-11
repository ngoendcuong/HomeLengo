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
        var qLower = q.ToLowerInvariant();

        var query = _context.Properties.AsNoTracking();

        // Chỉ lấy tin đang hoạt động
        query = query.Where(p => p.StatusId == 1 || p.StatusId == 2);

        // Nhận diện thuê / bán theo DB của bạn
        bool wantsRent = qLower.Contains("thuê") || qLower.Contains("cho thuê");
        bool wantsSale = qLower.Contains("bán") || qLower.Contains("mua");

        if (wantsRent && !wantsSale)
            query = query.Where(p => p.StatusId == 2); // Cho thuê
        else if (wantsSale && !wantsRent)
            query = query.Where(p => p.StatusId == 1); // Rao bán

        // 🔥 LỌC GIÁ "DƯỚI ..."
        var maxPrice = TryParseMaxPriceVnd(qLower);
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }


        // Chỉ lấy tin còn hoạt động: Rao bán hoặc Cho thuê (không lấy đã bán/đã thuê)
        query = query.Where(p => p.StatusId == 1 || p.StatusId == 2);

        if (wantsRent && !wantsSale) query = query.Where(p => p.StatusId == 2);
        else if (wantsSale && !wantsRent) query = query.Where(p => p.StatusId == 1);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var tokens = qLower
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length >= 2)
                .Take(6)
                .ToList();

            string[] stop = { "hôm", "nay", "mấy", "giờ", "là", "gì", "bao", "nhiêu", "sql", "ef", "core", "web", "website" };
            tokens = tokens.Where(t => !stop.Contains(t)).ToList();

            if (tokens.Count > 0)
            {
                foreach (var t in tokens)
                {
                    var pattern = $"%{t}%";
                    query = query.Where(p =>
                        EF.Functions.Like(p.Title, pattern) ||
                        (p.Address != null && EF.Functions.Like(p.Address, pattern)) ||
                        (p.Description != null && EF.Functions.Like(p.Description, pattern))
                    );
                }
            }
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
                p.Area,
                p.StatusId
            })
            .ToListAsync();

        if (items.Count == 0)
        {
            var fallbackQuery = _context.Properties.AsNoTracking();

            if (wantsRent && !wantsSale) fallbackQuery = fallbackQuery.Where(p => p.StatusId == 2);
            else if (wantsSale && !wantsRent) fallbackQuery = fallbackQuery.Where(p => p.StatusId == 1);

            items = await fallbackQuery
                .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(p => p.PropertyId)
                .Take(10)
                .Select(p => new
                {
                    p.PropertyId,
                    p.Title,
                    p.Price,
                    p.Address,
                    p.Area,
                    p.StatusId
                })
                .ToListAsync();
        }

        return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = false });
    }
    private static decimal? TryParseMaxPriceVnd(string qLower)
    {
        qLower = qLower.Replace(",", ".");

        bool hasUnder = qLower.Contains("dưới") || qLower.Contains("duoi") || qLower.Contains("<=");
        if (!hasUnder) return null;

        var match = System.Text.RegularExpressions.Regex.Match(qLower, @"(\d+(\.\d+)?)");
        if (!match.Success) return null;

        var numberStr = match.Groups[1].Value;
        if (!decimal.TryParse(
                numberStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var n))
            return null;

        if (qLower.Contains("tỷ") || qLower.Contains("ty") || qLower.Contains("tỉ"))
            return n * 1_000_000_000m;

        if (qLower.Contains("triệu") || qLower.Contains("trieu"))
            return n * 1_000_000m;

        return n;
    }

}
