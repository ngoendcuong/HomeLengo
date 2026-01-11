// Areas/RealEstateAdmin/Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class UsersController : BaseController
    {
        private readonly HomeLengoContext _context;

        public UsersController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var users = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    Id = u.UserId,
                    Name = u.FullName ?? u.Username,
                    Email = u.Email,
                    Phone = u.Phone ?? "",
                    Role = u.UserRoles.FirstOrDefault() != null 
                        ? u.UserRoles.First().Role.RoleName 
                        : "User",
                    Status = u.IsActive == true ? "Active" : "Locked",
                    Avatar = u.Avatar ?? "https://via.placeholder.com/50",
                    JoinDate = u.CreatedAt.HasValue 
                        ? u.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : "",
                    LastLogin = "" // Có thể thêm bảng LoginHistory nếu cần
                })
                .ToList();

            return View(users);
        }

        // API để lấy thông tin user cho modal
        [HttpGet]
        public async Task<JsonResult> GetUserDetail(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Agents)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return Json(new { error = "User not found" });
            }

            var totalProperties = user.Agents != null 
                ? user.Agents.Sum(a => a.Properties != null ? a.Properties.Count : 0)
                : 0;

            var result = new
            {
                Id = user.UserId,
                Name = user.FullName ?? user.Username,
                Email = user.Email,
                Phone = user.Phone ?? "",
                Role = user.UserRoles.FirstOrDefault() != null 
                    ? user.UserRoles.First().Role.RoleName 
                    : "User",
                Status = user.IsActive == true ? "Active" : "Locked",
                Avatar = user.Avatar ?? "https://via.placeholder.com/150",
                JoinDate = user.CreatedAt.HasValue 
                    ? user.CreatedAt.Value.ToString("dd/MM/yyyy") 
                    : "",
                LastLogin = "", // Có thể thêm bảng LoginHistory nếu cần
                Address = "", // Có thể thêm vào User model nếu cần
                TotalProperties = totalProperties,
                TotalViews = 0, // Có thể tính từ PropertyViews
                Bio = user.Agents.FirstOrDefault()?.Bio ?? ""
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                user.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}