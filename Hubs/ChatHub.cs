using Microsoft.AspNetCore.SignalR;
using HomeLengo.Services;

namespace HomeLengo.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IGeminiService _geminiService;
        private readonly IProductService _productService;

        public ChatHub(IGeminiService geminiService, IProductService productService)
        {
            _geminiService = geminiService;
            _productService = productService;
        }

        public async Task SendMessageToBot(string userMessage)
        {
            var raw = (userMessage ?? "").Trim();
            var q = raw.ToLowerInvariant();

            try
            {
                // ======================
                // 1) RULE: ngày / giờ
                // ======================
                if ((q.Contains("hôm nay") && (q.Contains("ngày") || q.Contains("ngay")))
                    || q == "hôm nay ngày mấy" || q == "hôm nay ngày bao nhiêu")
                {
                    var now = DateTime.Now;
                    await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo", $"Hôm nay là ngày {now:dd/MM/yyyy}.");
                    return;
                }

                if (q.Contains("mấy giờ") || q.Contains("may gio") || q.Contains("bây giờ") || q.Contains("bay gio"))
                {
                    var now = DateTime.Now;
                    await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo", $"Bây giờ là {now:HH:mm}.");
                    return;
                }

                // ======================
                // 2) INTENT: COUNT (đếm) - TRẢ THẲNG DB
                // ======================
                // DB bạn: StatusId = 1 là BÁN, StatusId = 2 là THUÊ
                if ((q.Contains("bao nhiêu") || q.Contains("mấy") || q.Contains("số lượng"))
                    && (q.Contains("đang bán") || q.Contains("rao bán") || q.Contains("bán")))
                {
                    var countSale = await _productService.CountByStatusAsync(1);
                    await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                        $"Hiện tại HomeLengo đang có {countSale} bất động sản đang bán ạ.");
                    return;
                }

                if ((q.Contains("bao nhiêu") || q.Contains("mấy") || q.Contains("số lượng"))
                    && (q.Contains("cho thuê") || q.Contains("đang thuê") || q.Contains("thuê")))
                {
                    var countRent = await _productService.CountByStatusAsync(2);
                    await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                        $"Hiện tại HomeLengo đang có {countRent} bất động sản cho thuê ạ.");
                    return;
                }

                // ======================
                // 3) INTENT: LIST TYPES (các loại BĐS) - TRẢ THẲNG DB
                // ======================
                if (q.Contains("loại") && (q.Contains("bất động sản") || q.Contains("bđs") || q.Contains("loại hình")))
                {
                    var types = await _productService.GetTypeCountsAsync();

                    if (types == null || types.Count == 0)
                    {
                        await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                            "Hiện tại hệ thống chưa có dữ liệu loại hình bất động sản.");
                        return;
                    }

                    var lines = string.Join("\n", types.Select(x => $"- {x.Name}: {x.Count}"));
                    await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                        $"HomeLengo hiện có {types.Count} loại hình:\n{lines}");
                    return;
                }

                // ======================
                // 4) Còn lại: hỏi tư vấn / lọc / mô tả -> GỌI GEMINI
                // ======================
                var botAnswer = await _geminiService.GetAnswer(raw);
                await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo", botAnswer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ChatHub ERROR ===");
                Console.WriteLine(ex);

#if DEBUG
                await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                    $"(DEBUG) {ex.GetType().Name}: {ex.Message}");
#else
                await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo",
                    "Hệ thống đang bận hoặc lỗi kết nối. Bạn thử lại sau nhé.");
#endif
            }
        }
    }
}
