using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HomeLengo.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IProductService
{
    Task<string> GetRelevantProductsAsText(string userQuery);
    Task<int> CountByStatusAsync(int statusId);
    Task<List<(string Name, int Count)>> GetTypeCountsAsync();

}

public class ProductService : IProductService
{
    private readonly HomeLengoContext _context;

    public ProductService(HomeLengoContext context)
    {
        _context = context;
    }

    public Task<int> CountByStatusAsync(int statusId)
    {
        return _context.Properties.AsNoTracking()
            .CountAsync(p => p.StatusId == statusId);
    }

    public async Task<List<(string Name, int Count)>> GetTypeCountsAsync()
    {
        // Bạn sửa lại đúng tên navigation/field của bạn:
        // Ví dụ: p.PropertyType.TypeName hoặc p.PropertyType.Name
        var data = await _context.Properties.AsNoTracking()
            .Where(p => p.StatusId == 1 || p.StatusId == 2)
            .GroupBy(p => p.PropertyType.Name) // <-- nếu bạn không có PropertyType/TypeName thì gửi model mình sửa đúng
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return data.Select(x => (x.Name ?? "Khác", x.Count)).ToList();
    }


    public async Task<string> GetRelevantProductsAsText(string userQuery)
    {
        userQuery ??= "";
        var q = userQuery.Trim();
        var qLower = q.ToLowerInvariant();

        // 1) base query: chỉ lấy tin còn hoạt động (StatusId 1/2)
        IQueryable<Property> query = _context.Properties.AsNoTracking()
            .Where(p => p.StatusId == 1 || p.StatusId == 2);

        // 2) intent thuê/bán
        bool wantsRent = qLower.Contains("thuê") || qLower.Contains("cho thuê");
        bool wantsSale = qLower.Contains("bán") || qLower.Contains("mua");

        if (wantsRent && !wantsSale) query = query.Where(p => p.StatusId == 2); // Cho thuê
        else if (wantsSale && !wantsRent) query = query.Where(p => p.StatusId == 1); // Rao bán

        // 3) giá tối đa (vd: "dưới 2 tỷ")
        var maxPrice = TryParseMaxPriceVnd(qLower);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // 4) token search
        var tokens = ExtractTokens(qLower);
        if (tokens.Count > 0)
        {
            var predicate = BuildOrLikePredicate(tokens);
            query = query.Where(predicate);
        }


        // 5) lấy ít thôi để model đọc hết
        var items = await query
            .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
            .ThenByDescending(p => p.PropertyId)
            .Take(8)
            .Select(p => new
            {
                p.PropertyId,
                p.Title,
                p.Price,
                p.Currency,
                p.Address,
                p.Area,
                p.Bedrooms,
                p.Bathrooms,
                p.StatusId
            })
            .ToListAsync();

        // 6) fallback: vẫn phải giữ status + price, chỉ nới điều kiện keyword thôi
        if (items.Count == 0)
        {
            IQueryable<Property> fallbackQuery = _context.Properties.AsNoTracking()
                .Where(p => p.StatusId == 1 || p.StatusId == 2);

            if (wantsRent && !wantsSale) fallbackQuery = fallbackQuery.Where(p => p.StatusId == 2);
            else if (wantsSale && !wantsRent) fallbackQuery = fallbackQuery.Where(p => p.StatusId == 1);

            if (maxPrice.HasValue) fallbackQuery = fallbackQuery.Where(p => p.Price <= maxPrice.Value);

            items = await fallbackQuery
                .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(p => p.PropertyId)
                .Take(8)
                .Select(p => new
                {
                    p.PropertyId,
                    p.Title,
                    p.Price,
                    p.Currency,
                    p.Address,
                    p.Area,
                    p.Bedrooms,
                    p.Bathrooms,
                    p.StatusId
                })
                .ToListAsync();
        }

        // 7) TRẢ TEXT PHẲNG (không JSON)
        if (items.Count == 0)
            return "__NO_DATA__";

        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();
        foreach (var x in items)
        {
            var priceText = x.Price.ToString("#,0", vi);
            var addr = string.IsNullOrWhiteSpace(x.Address) ? "(chưa có địa chỉ)" : x.Address;
            sb.AppendLine($"- ID={x.PropertyId} | {x.Title} | Giá={priceText} {x.Currency} | DT={x.Area}m2 | PN={x.Bedrooms} | WC={x.Bathrooms} | StatusId={x.StatusId} | {addr}");
        }

        return sb.ToString();
    }

    private static List<string> ExtractTokens(string qLower)
    {
        var tokens = qLower
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Take(8)
            .ToList();

        string[] stop =
        {
            "hôm","nay","mấy","giờ","là","gì","bao","nhiêu",
            "sql","ef","core","web","website","cho","tôi","muốn","tìm","cần"
        };

        tokens = tokens.Where(t => !stop.Contains(t)).Distinct().ToList();
        return tokens;
    }

    private static decimal? TryParseMaxPriceVnd(string qLower)
    {
        qLower = qLower.Replace(",", ".");

        bool hasUnder = qLower.Contains("dưới") || qLower.Contains("duoi") || qLower.Contains("<=");
        if (!hasUnder) return null;

        var match = Regex.Match(qLower, @"(\d+(\.\d+)?)");
        if (!match.Success) return null;

        var numberStr = match.Groups[1].Value;

        if (!decimal.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
            return null;

        if (qLower.Contains("tỷ") || qLower.Contains("ty") || qLower.Contains("tỉ"))
            return n * 1_000_000_000m;

        if (qLower.Contains("triệu") || qLower.Contains("trieu"))
            return n * 1_000_000m;

        return n;
    }

    private static Expression<Func<Property, bool>> BuildOrLikePredicate(List<string> tokens)
    {
        // p => false
        var p = Expression.Parameter(typeof(Property), "p");
        Expression body = Expression.Constant(false);

        foreach (var t in tokens)
        {
            var likePattern = Expression.Constant($"%{t}%");

            // EF.Functions.Like(p.Title, "%t%")
            Expression Like(Expression? member)
            {
                // member != null && EF.Functions.Like(member, pattern)
                var notNull = Expression.NotEqual(member!, Expression.Constant(null, typeof(string)));

                var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

                var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
                    nameof(DbFunctionsExtensions.Like),
                    new[] { typeof(DbFunctions), typeof(string), typeof(string) }
                )!;

                var likeCall = Expression.Call(likeMethod, efFunctions, member!, likePattern);

                return Expression.AndAlso(notNull, likeCall);
            }

            var titleProp = Expression.Property(p, nameof(Property.Title));
            var addressProp = Expression.Property(p, nameof(Property.Address));
            var descProp = Expression.Property(p, nameof(Property.Description));

            // Title LIKE OR Address LIKE OR Description LIKE
            Expression oneToken =
                Expression.OrElse(
                    Expression.Call(
                        typeof(DbFunctionsExtensions).GetMethod(
                            nameof(DbFunctionsExtensions.Like),
                            new[] { typeof(DbFunctions), typeof(string), typeof(string) }
                        )!,
                        Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!),
                        titleProp,
                        likePattern
                    ),
                    Expression.OrElse(
                        Like(addressProp),
                        Like(descProp)
                    )
                );

            body = Expression.OrElse(body, oneToken);
        }

        return Expression.Lambda<Func<Property, bool>>(body, p);
    }
}
