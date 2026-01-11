using HomeLengo.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeLengo.Controllers
{
    public class PackageExpirationController : Controller
    {
        private readonly PackageExpirationService _expirationService;
        private readonly ILogger<PackageExpirationController> _logger;

        public PackageExpirationController(
            PackageExpirationService expirationService,
            ILogger<PackageExpirationController> logger)
        {
            _expirationService = expirationService;
            _logger = logger;
        }

        /// <summary>
        /// API để chạy thủ công kiểm tra gói hết hạn (dành cho admin hoặc testing)
        /// </summary>
        [HttpPost]
        [Route("api/PackageExpiration/CheckExpired")]
        public async Task<IActionResult> CheckExpired()
        {
            try
            {
                await _expirationService.ProcessExpiredPackagesAsync();
                return Json(new { success = true, message = "Đã kiểm tra và xử lý các gói hết hạn thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra gói hết hạn");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Kiểm tra và xử lý gói hết hạn cho user hiện tại
        /// </summary>
        [HttpPost]
        [Route("api/PackageExpiration/CheckMyPackage")]
        public async Task<IActionResult> CheckMyPackage()
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                await _expirationService.ProcessExpiredPackageForUserAsync(userId);
                return Json(new { success = true, message = "Đã kiểm tra gói dịch vụ của bạn" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra gói của user");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}

