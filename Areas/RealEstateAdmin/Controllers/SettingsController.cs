// Areas/RealEstateAdmin/Controllers/SettingsController.cs
using Microsoft.AspNetCore.Mvc;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class SettingsController : BaseController
    {
        // Trang tổng hợp cấu hình
        public IActionResult Index()
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return View();
        }

        // Cấu hình chung
        public IActionResult General()
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ViewBag.WebsiteName = "HomeLengo - Bất động sản uy tín";
            ViewBag.Slogan = "Nơi kết nối mọi giao dịch";
            ViewBag.Email = "contact@homelengo.vn";
            ViewBag.Phone = "1900 xxxx";
            ViewBag.Address = "123 Nguyễn Huệ, Q.1, TP.HCM";

            return View();
        }

        // Logo & Hình ảnh
        public IActionResult Media()
        {
            return View();
        }

        // Email & SMTP
        public IActionResult Email()
        {
            ViewBag.SmtpHost = "smtp.gmail.com";
            ViewBag.SmtpPort = "587";
            ViewBag.SmtpUser = "your-email@gmail.com";
            ViewBag.SenderName = "HomeLengo Support";

            return View();
        }

        // Banner & Slider
        public IActionResult Banners()
        {
            var banners = new List<dynamic>
            {
                new { Id = 1, Title = "Banner trang chủ 1", Image = "https://via.placeholder.com/1200x400", Order = 1, IsActive = true },
                new { Id = 2, Title = "Banner trang chủ 2", Image = "https://via.placeholder.com/1200x400", Order = 2, IsActive = true },
                new { Id = 3, Title = "Banner sidebar", Image = "https://via.placeholder.com/300x600", Order = 3, IsActive = false }
            };

            return View(banners);
        }

        // Thông báo hệ thống
        public IActionResult Notifications()
        {
            var notifications = new List<dynamic>
            {
                new { Id = 1, Title = "Bảo trì hệ thống", Message = "Hệ thống sẽ bảo trì từ 2h-4h sáng ngày 20/01", Type = "warning", IsActive = true, CreatedDate = "15/01/2025" },
                new { Id = 2, Title = "Tính năng mới", Message = "Đã cập nhật tính năng tìm kiếm nâng cao", Type = "info", IsActive = false, CreatedDate = "10/01/2025" }
            };

            return View(notifications);
        }
    }
}