using System.Text;
using Newtonsoft.Json;

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
        // Bước 1: Lấy dữ liệu từ DB dựa trên câu hỏi
        string dbContext = await _productService.GetRelevantProductsAsText(userQuestion);

        // Bước 2: Chuẩn bị Prompt (Kịch bản)
        string prompt = $@"
            Bạn là nhân viên tư vấn bán hàng chuyên nghiệp.
            Dưới đây là thông tin sản phẩm trong kho (Định dạng JSON):
            {dbContext}

            Câu hỏi của khách: ""{userQuestion}""

            Yêu cầu:
            1. Trả lời dựa trên dữ liệu JSON cung cấp.
            2. Nếu sản phẩm hết hàng (Stock = 0), hãy báo khách.
            3. Văn phong thân thiện, không nhắc đến kỹ thuật như 'JSON' hay 'Database'.
            4. Trả lời bằng Tiếng Việt.
        ";

        // Bước 3: Cấu hình gọi API Gemini
        string apiKey = _config["Gemini:ApiKey"];
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        // Cấu trúc Body theo chuẩn Google API
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        // Bước 4: Gửi Request
        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(jsonResponse);

            try
            {
                // Lấy nội dung text trả về
                return result.candidates[0].content.parts[0].text;
            }
            catch { return "Gemini trả về nhưng không đọc được nội dung."; }
        }
        // ... đoạn code if (response.IsSuccessStatusCode) ở trên giữ nguyên ...

        else
        {
            // ĐỌC LỖI THỰC SỰ TỪ GOOGLE
            var errorJson = await response.Content.ReadAsStringAsync();

            // Trả về lỗi chi tiết để debug (sau này chạy ngon thì xóa đi)
            return $"Lỗi kết nối API: {response.StatusCode}. Chi tiết: {errorJson}";
        }
        //return "Hệ thống AI đang bảo trì, vui lòng thử lại sau.";
    }
}