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

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Login(string usernameOrEmail, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            // Tìm user theo username hoặc email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null)
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng" });
            }

            // Kiểm tra mật khẩu
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng" });
            }

            // Kiểm tra tài khoản có active không
            if (user.IsActive == false)
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa" });
            }

            // Lưu thông tin user vào session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Avatar", user.Avatar ?? "avatarMacDinh.jpg");

            // Lấy role của user (lấy role mới nhất nếu có nhiều)
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.UserId)
                .OrderByDescending(ur => ur.AssignedAt)
                .FirstOrDefaultAsync();
            
            string redirectUrl = null;
            if (userRole != null)
            {
                var roleId = userRole.RoleId;
                HttpContext.Session.SetString("RoleId", roleId.ToString());
                HttpContext.Session.SetString("RoleName", userRole.Role.RoleName);
                
                // Chỉ redirect đến RealEstateAdmin nếu là Admin (role=1) hoặc Người kiểm duyệt (role=4)
                // KHÔNG redirect nếu là Agent (role=2) - Agent sẽ vào trang Admin thông thường
                if (roleId == 1) // Admin
                {
                    redirectUrl = "/RealEstateAdmin/Dashboard";
                }
                else if (roleId == 4) // Người kiểm duyệt
                {
                    redirectUrl = "/RealEstateAdmin/Dashboard";
                }
                // RoleId = 2 (Agent) không redirect đến RealEstateAdmin, sẽ ở trang chủ hoặc Admin area
            }

            // Lấy AgentId nếu user là agent
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == user.UserId);
            if (agent != null)
            {
                HttpContext.Session.SetString("AgentId", agent.AgentId.ToString());
            }

            // Kiểm tra gói dịch vụ hết hạn khi đăng nhập (chạy ngầm, không block)
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
                    // Log lỗi nhưng không ảnh hưởng đến quá trình đăng nhập
                    Console.WriteLine($"Lỗi khi kiểm tra gói hết hạn cho user {user.UserId}: {ex.Message}");
                }
            });

            return Json(new { success = true, message = "Đăng nhập thành công", redirectUrl = redirectUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra khi đăng nhập: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Register(string username, string email, string fullName, string phone, string password, string confirmPassword)
    {
        try
        {
            // Validation cơ bản
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            if (password != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
            }

            if (password.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            // Kiểm tra username đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
            }

            // Kiểm tra email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return Json(new { success = false, message = "Email đã được sử dụng" });
            }

            // Kiểm tra số điện thoại đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Phone == phone))
            {
                return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });
            }

            // Tạo user mới
            var user = new User
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Phone = phone,
                PasswordHash = PasswordHasher.HashPassword(password),
                Avatar = "avatarMacDinh.jpg", // Avatar mặc định
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Gán role mặc định (RoleId = 3 - User thường)
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = 3, // Role mặc định là User thường
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Không tự động đăng nhập, trả về JSON để xử lý bằng JavaScript
            return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}

