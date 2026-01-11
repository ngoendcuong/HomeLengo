using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

public interface IGeminiService
{
    Task<string> GetAnswer(string userQuestion);
}

public class GeminiService : IGeminiService
{
    private readonly IProductService _productService;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public GeminiService(IProductService productService, IConfiguration config, HttpClient httpClient, IMemoryCache cache)
    {
        _productService = productService;
        _config = config;
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<string> GetAnswer(string userQuestion)
    {
        userQuestion = (userQuestion ?? "").Trim();
        var cacheKey = "gemini:" + userQuestion.Trim().ToLowerInvariant();
        if (_cache.TryGetValue(cacheKey, out string cached))
            return cached;

        if (string.IsNullOrWhiteSpace(userQuestion))
            return "Bạn muốn hỏi gì về bất động sản hoặc tính năng của HomeLengo?";

        // ✅ Thời gian hiện tại (server) để trả lời đúng các câu hỏi cơ bản
        // Nếu server chạy VN thì DateTime.Now ok. Nếu deploy khác timezone, bạn nên set timezone rõ.
        var now = DateTime.Now;
        var nowText = now.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

        // Lấy dữ liệu liên quan từ DB
        // (Nếu câu hỏi không liên quan BĐS, service này có thể trả rỗng/nhẹ -> ok)
        var dbContextJson = await _productService.GetRelevantProductsAsText(userQuestion);
        dbContextJson = (dbContextJson ?? "").Trim();

        // ✅ Tách SYSTEM INSTRUCTION riêng, không nhét chung vào "prompt user"
        var systemInstruction = @"
Bạn là trợ lý ảo của website bất động sản HomeLengo. Trả lời tiếng Việt, thân thiện, ngắn gọn.

QUY TẮC CHỐNG SAI:
1) Không bịa dữ liệu. Nếu không đủ thông tin để kết luận, hãy nói rõ bạn chưa có dữ liệu và gợi ý người dùng cung cấp thêm tiêu chí.
2) Với câu hỏi về ngày/giờ: phải dùng đúng 'THỜI GIAN HIỆN TẠI' do hệ thống cung cấp.
3) Với câu hỏi về bất động sản: chỉ được dựa trên dữ liệu BĐS được cung cấp. Nếu dữ liệu trống hoặc không có mục phù hợp, nói không tìm thấy.
4) Không nhắc đến 'JSON', 'database', 'prompt', 'system'. Chỉ trả lời như một trợ lý trên website.
";

        // ✅ User message gọn: gồm thời gian + dữ liệu + câu hỏi
        // Mẹo: đánh nhãn rõ ràng giúp model bám đúng nguồn
        var userContent = $@"
THỜI GIAN HIỆN TẠI (theo hệ thống): {nowText}

DỮ LIỆU BẤT ĐỘNG SẢN (chỉ dùng khi câu hỏi liên quan BĐS):
{(string.IsNullOrWhiteSpace(dbContextJson) ? "(không có dữ liệu phù hợp cho câu hỏi này)" : dbContextJson)}

CÂU HỎI NGƯỜI DÙNG:
{userQuestion}
";

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return "Thiếu cấu hình Gemini:ApiKey trong appsettings.";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        // ✅ Thêm generationConfig để giảm bịa
        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userContent } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.9,
                topK = 40,
                maxOutputTokens = 512
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(url, content);
        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // ✅ 429 quota exceeded
            if ((int)response.StatusCode == 429)
            {
                // cố parse retryDelay
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var details = doc.RootElement.GetProperty("error").GetProperty("details");

                    string? retryDelay = null;
                    foreach (var d in details.EnumerateArray())
                    {
                        if (d.TryGetProperty("@type", out var t) &&
                            t.GetString() == "type.googleapis.com/google.rpc.RetryInfo" &&
                            d.TryGetProperty("retryDelay", out var rd))
                        {
                            retryDelay = rd.GetString(); // ví dụ "42s"
                            break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(retryDelay))
                        return $"Hiện tại hệ thống đang quá tải (vượt giới hạn Gemini). Bạn thử lại sau {retryDelay.Replace("s", "")} giây nhé.";

                    return "Hiện tại hệ thống đang quá tải (vượt giới hạn Gemini). Bạn thử lại sau ít phút nhé.";
                }
                catch
                {
                    return "Hiện tại hệ thống đang quá tải (vượt giới hạn Gemini). Bạn thử lại sau ít phút nhé.";
                }
            }

            // các lỗi khác
            return $"Lỗi Gemini: {response.StatusCode}.";
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);

            // Gemini đôi khi trả multiple parts -> gộp lại cho chắc
            var parts = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            var sb = new StringBuilder();
            foreach (var p in parts.EnumerateArray())
            {
                if (p.TryGetProperty("text", out var t))
                    sb.Append(t.GetString());
            }

            var text = sb.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return "Mình chưa lấy được câu trả lời từ trợ lý.";
            }

            // ✅ LƯU CACHE 2 PHÚT
            _cache.Set(
                "gemini:" + userQuestion.Trim().ToLowerInvariant(),
                text,
                TimeSpan.FromMinutes(2)
            );

            return text;

        }
        catch
        {
            return "Gemini trả về nhưng không đọc được nội dung.";
        }
    }
}
