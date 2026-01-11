// Areas/Admin/Controllers/PackagesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class PackagesController : Controller
    {
        private readonly HomeLengoContext _context;

        public PackagesController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Packages có thể được lưu trong Settings hoặc tạo model Package riêng
            // Hiện tại sử dụng dữ liệu từ Settings hoặc hardcode với khả năng mở rộng
            var packages = new List<dynamic>
            {
                new { Id = 1, Name = "Gói FREE", Price = 0, Duration = 30, MaxPosts = 5, Features = new[] { "5 tin đăng", "Hiển thị 30 ngày", "Không nổi bật" }, Type = "Free" },
                new { Id = 2, Name = "Gói VIP", Price = 500000, Duration = 60, MaxPosts = 20, Features = new[] { "20 tin đăng", "Hiển thị 60 ngày", "Tin nổi bật", "Hỗ trợ ưu tiên" }, Type = "VIP" },
                new { Id = 3, Name = "Gói PREMIUM", Price = 1000000, Duration = 90, MaxPosts = 50, Features = new[] { "50 tin đăng", "Hiển thị 90 ngày", "Ưu tiên hàng đầu", "Hỗ trợ 24/7", "Badge đặc biệt" }, Type = "Premium" }
            };

            // Lấy transactions từ database
            var transactions = _context.Transactions
                .Include(t => t.User)
                .Where(t => t.TransactionType == "package" || t.TransactionType == "subscription")
                .Select(t => new
                {
                    Id = t.TransactionId,
                    Agent = t.User != null ? (t.User.FullName ?? t.User.Username) : "N/A",
                    Package = GetPackageName(t.Amount), // Dựa vào amount để xác định package
                    Price = t.Amount,
                    PurchaseDate = t.CreatedAt.HasValue 
                        ? t.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : "",
                    ExpiryDate = CalculateExpiryDate(t.CreatedAt, t.Amount), // Tính toán dựa trên amount
                    Status = GetTransactionStatus(t.Status, t.CreatedAt, t.Amount)
                })
                .OrderByDescending(t => t.PurchaseDate)
                .ToList();

            ViewBag.Packages = packages;
            ViewBag.Transactions = transactions;

            return View();
        }

        private string GetPackageName(decimal amount)
        {
            return amount switch
            {
                0 => "FREE",
                >= 1000000 => "PREMIUM",
                >= 500000 => "VIP",
                _ => "CUSTOM"
            };
        }

        private string CalculateExpiryDate(DateTime? purchaseDate, decimal amount)
        {
            if (!purchaseDate.HasValue) return "";
            
            int duration = amount switch
            {
                0 => 30,
                >= 1000000 => 90,
                >= 500000 => 60,
                _ => 30
            };

            return purchaseDate.Value.AddDays(duration).ToString("dd/MM/yyyy");
        }

        private string GetTransactionStatus(string? status, DateTime? createdDate, decimal amount)
        {
            if (status == "completed" && createdDate.HasValue)
            {
                int duration = amount switch
                {
                    0 => 30,
                    >= 1000000 => 90,
                    >= 500000 => 60,
                    _ => 30
                };

                var expiryDate = createdDate.Value.AddDays(duration);
                return expiryDate >= DateTime.Now ? "Active" : "Expired";
            }
            
            return status ?? "Pending";
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTransactionStatus(int id, string status)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                transaction.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}