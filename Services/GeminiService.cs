using System.Text;
using System.Text.Json;

public interface IGeminiService
{
    Task<string> GetAnswer(string userQuestion);
}

public class GeminiService : IGeminiService
{
    private readonly IProductService _productService;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public GeminiService(IProductService productService, IConfiguration config, HttpClient httpClient)
    {
        _productService = productService;
        _config = config;
        _httpClient = httpClient;
    }

    public async Task<string> GetAnswer(string userQuestion)
    {
        userQuestion ??= "";

        // Lấy dữ liệu liên quan từ DB (đã là JSON gọn)
        var dbContextJson = await _productService.GetRelevantProductsAsText(userQuestion);

        // Prompt đúng domain bất động sản + cho phép trả lời câu hỏi chung
        var prompt = $@"
Bạn là trợ lý ảo của website bất động sản HomeLengo.
- Nếu câu hỏi liên quan bất động sản (giá, địa chỉ, diện tích, danh sách, gợi ý...), hãy dựa vào dữ liệu JSON bên dưới.
- Nếu câu hỏi là kiến thức chung (ví dụ: hôm nay ngày mấy, bây giờ mấy giờ, thời tiết...), hãy trả lời như chat bình thường.
- Trả lời ngắn gọn, thân thiện, tiếng Việt. Không nhắc đến ""JSON"" hay ""database"".

Dữ liệu bất động sản (JSON):
{dbContextJson}

Câu hỏi người dùng: ""{userQuestion}""
";

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return "Thiếu cấu hình Gemini:ApiKey trong appsettings.";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"Lỗi Gemini: {response.StatusCode}. Chi tiết: {raw}";

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(text) ? "Mình chưa lấy được câu trả lời từ Gemini." : text!;
        }
        catch
        {
            return "Gemini trả về nhưng không đọc được nội dung.";
        }
    }
}
