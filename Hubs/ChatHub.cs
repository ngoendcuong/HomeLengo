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
            // 1. Gửi tin nhắn của User lại cho giao diện User (để hiện lên khung chat)
            await Clients.Caller.SendAsync("ReceiveMessage", "Bạn", userMessage);

            // 2. Gọi Gemini xử lý
            string botAnswer = await _geminiService.GetAnswer(userMessage);

            // 3. Gửi câu trả lời của Bot về giao diện User
            await Clients.Caller.SendAsync("ReceiveMessage", "Trợ lý ảo", botAnswer);
        }
    }
}