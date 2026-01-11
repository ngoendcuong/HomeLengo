using Microsoft.AspNetCore.SignalR;

namespace HomeLengo.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IGeminiService _geminiService;

        public ChatHub(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public async Task SendMessageToBot(string userMessage)
        {
            //// 1) echo user
            //await Clients.Caller.SendAsync("ReceiveMessage", "Bạn", userMessage);

            var q = (userMessage ?? "").Trim().ToLowerInvariant();

            // 2) CÂU CƠ BẢN: khỏi gọi Gemini (tiết kiệm quota)
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

            // 3) Còn lại mới gọi Gemini
            string botAnswer = await _geminiService.GetAnswer(userMessage);

            // 4) gửi bot reply
            await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo", botAnswer);
        }
    }
}