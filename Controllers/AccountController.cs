using HomeLengo.Models;
using HomeLengo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeLengo.Controllers;

public class AccountController : Controller
{
    private readonly HomeLengoContext _context;
    private readonly IServiceProvider _serviceProvider;

    public AccountController(HomeLengoContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // ✅ LOGIN AJAX (dùng cho popup/modal + trang login)
    // ✅ BỎ IgnoreAntiforgeryToken để bảo mật + dùng token từ form
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Login(string usernameOrEmail, string password, string? returnUrl = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin", redirectUrl = (string?)null });

            // Tìm user theo username hoặc email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null)
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng", redirectUrl = (string?)null });

            // Kiểm tra mật khẩu (hash)
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng", redirectUrl = (string?)null });

            // Kiểm tra tài khoản active
            if (user.IsActive == false)
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa", redirectUrl = (string?)null });

            // Lưu session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Avatar", user.Avatar ?? "avatarMacDinh.jpg");

            // Role mới nhất
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.UserId)
                .OrderByDescending(ur => ur.AssignedAt)
                .FirstOrDefaultAsync();

            int? roleId = null;
            string? roleName = null;
            if (userRole != null)
            {
                roleId = userRole.RoleId;
                roleName = userRole.Role?.RoleName;

                HttpContext.Session.SetString("RoleId", roleId.Value.ToString());
                if (!string.IsNullOrWhiteSpace(roleName))
                    HttpContext.Session.SetString("RoleName", roleName);
            }

            // Lấy AgentId nếu có
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == user.UserId);
            if (agent != null)
                HttpContext.Session.SetString("AgentId", agent.AgentId.ToString());

            // Chạy kiểm tra gói hết hạn ngầm
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var expirationService = scope.ServiceProvider.GetRequiredService<PackageExpirationService>();
                    await expirationService.ProcessExpiredPackageForUserAsync(user.UserId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi kiểm tra gói hết hạn cho user {user.UserId}: {ex.Message}");
                }
            });

            // ✅ QUY TẮC REDIRECT MỚI (khắc phục bị nhảy RealEstateAdmin khi login từ popup):
            // 1) Nếu client truyền returnUrl (vd: /Payment/Checkout?planId=1) => ưu tiên redirect về đó
            // 2) Nếu không có returnUrl:
            //    - Admin/Moderator => /RealEstateAdmin/Dashboard
            //    - còn lại => null (client tự reload hoặc ở nguyên trang)
            string? redirectUrl = null;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                redirectUrl = returnUrl;
            }
            else
            {
                if (roleId == 1 || roleId == 4)
                    redirectUrl = "/RealEstateAdmin/Dashboard";
            }

            return Json(new { success = true, message = "Đăng nhập thành công", redirectUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra khi đăng nhập: " + ex.Message, redirectUrl = (string?)null });
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // ✅ REGISTER AJAX
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Register(string username, string email, string fullName, string phone, string password, string confirmPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            if (password != confirmPassword)
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });

            if (password.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });

            if (await _context.Users.AnyAsync(u => u.Username == username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Json(new { success = false, message = "Email đã được sử dụng" });

            if (await _context.Users.AnyAsync(u => u.Phone == phone))
                return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });

            var user = new User
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Phone = phone,
                PasswordHash = PasswordHasher.HashPassword(password),
                Avatar = "avatarMacDinh.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = 3,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
