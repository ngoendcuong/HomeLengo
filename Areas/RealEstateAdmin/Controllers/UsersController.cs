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

        public IActionResult Index(string searchString, string role, string status)
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.Contains(searchString)) ||
                    (u.Username != null && u.Username.Contains(searchString)) ||
                    (u.Email != null && u.Email.Contains(searchString)) ||
                    (u.Phone != null && u.Phone.Contains(searchString)));
            }

            // Filter theo role
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == role));
            }

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    query = query.Where(u => u.IsActive == true);
                }
                else if (status == "Locked")
                {
                    query = query.Where(u => u.IsActive != true);
                }
            }

            var users = query
                .ToList()
                .Select(u => new
                {
                    Id = u.UserId,
                    Name = u.FullName ?? u.Username,
                    Email = u.Email,
                    Phone = u.Phone ?? "",
                    Role = u.UserRoles.FirstOrDefault() != null 
                        ? u.UserRoles.First().Role.RoleName 
                        : "User",
                    RoleId = u.UserRoles.FirstOrDefault() != null 
                        ? u.UserRoles.First().RoleId 
                        : 0,
                    Status = u.IsActive == true ? "Active" : "Locked",
                    Avatar = string.IsNullOrEmpty(u.Avatar) 
                        ? "/assets/images/avatar/avatarMacDinh.jpg"
                        : (u.Avatar.StartsWith("http://") || u.Avatar.StartsWith("https://") || u.Avatar.StartsWith("/"))
                            ? u.Avatar
                            : "/assets/images/avatar/" + u.Avatar,
                    JoinDate = u.CreatedAt.HasValue 
                        ? u.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : "",
                    LastLogin = "" // Có thể thêm bảng LoginHistory nếu cần
                })
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.Roles = _context.Roles.Select(r => r.RoleName).Distinct().ToList();
            ViewBag.AllRoles = _context.Roles.Select(r => new { r.RoleId, r.RoleName }).ToList();

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
                    .ThenInclude(a => a.Properties)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return Json(new { error = "User not found" });
            }

            // Tính tổng số tin đăng từ tất cả Agents của user
            var totalProperties = 0;
            var totalViews = 0;
            
            if (user.Agents != null && user.Agents.Any())
            {
                foreach (var agent in user.Agents)
                {
                    if (agent.Properties != null)
                    {
                        totalProperties += agent.Properties.Count;
                        totalViews += agent.Properties.Sum(p => p.Views ?? 0);
                    }
                }
            }

            // Xử lý avatar path
            string avatarPath;
            if (string.IsNullOrEmpty(user.Avatar))
            {
                avatarPath = "/assets/images/avatar/avatarMacDinh.jpg";
            }
            else if (user.Avatar.StartsWith("http://") || user.Avatar.StartsWith("https://"))
            {
                avatarPath = user.Avatar;
            }
            else if (user.Avatar.StartsWith("/"))
            {
                avatarPath = user.Avatar;
            }
            else
            {
                avatarPath = "/assets/images/avatar/" + user.Avatar;
            }

            var firstUserRole = user.UserRoles.FirstOrDefault();
            var result = new
            {
                Id = user.UserId,
                Username = user.Username,
                Name = user.FullName ?? user.Username,
                Email = user.Email,
                Phone = user.Phone ?? "",
                Role = firstUserRole != null 
                    ? firstUserRole.Role.RoleName 
                    : "User",
                RoleId = firstUserRole != null ? firstUserRole.RoleId : 0,
                Status = user.IsActive == true ? "Active" : "Locked",
                Avatar = avatarPath,
                JoinDate = user.CreatedAt.HasValue 
                    ? user.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                    : "",
                CreatedAt = user.CreatedAt,
                ModifiedAt = user.ModifiedAt,
                LastLogin = "", // Có thể thêm bảng LoginHistory nếu cần
                Address = "", // Có thể thêm vào User model nếu cần
                TotalProperties = totalProperties,
                TotalViews = totalViews,
                Bio = user.Agents.FirstOrDefault()?.Bio ?? ""
            };

            return Json(result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            // Kiểm tra nếu user là Admin (RoleId = 1) thì không cho khóa
            var isAdmin = user.UserRoles.Any(ur => ur.RoleId == 1);
            if (isAdmin)
            {
                return Json(new { success = false, message = "Không thể khóa tài khoản Admin" });
            }

            user.IsActive = !user.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = user.IsActive == true ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản" });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.Agents)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return Json(new { success = false, message = "Người dùng không tồn tại" });
                }

                // Kiểm tra nếu user là Admin (RoleId = 1) thì không cho xóa
                var isAdmin = user.UserRoles.Any(ur => ur.RoleId == 1);
                if (isAdmin)
                {
                    return Json(new { success = false, message = "Không thể xóa tài khoản Admin" });
                }

                // Sử dụng raw SQL để xóa tất cả các bản ghi liên quan
                // Điều này tránh được các vấn đề về foreign key constraints
                var userIdParam = new Microsoft.Data.SqlClient.SqlParameter("@UserId", id);
                
                // Xóa các bản ghi liên quan bằng raw SQL
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM PropertyVisits WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE t
                    FROM Transactions t
                    INNER JOIN Bookings b ON b.BookingId = t.BookingId
                    WHERE b.UserId = @UserId
                ", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Bookings WHERE UserId = @UserId",
                    userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Reviews WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Favorites WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Inquiries WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Messages WHERE FromUserId = @UserId OR ToUserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Notifications WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SearchHistory WHERE UserId = @UserId", userIdParam);
                //await _context.Database.ExecuteSqlRawAsync(
                //    "DELETE FROM ServiceRegisters WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserServicePackages WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM BlogComments WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Blogs WHERE AuthorId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AdminAudit WHERE UserId = @UserId", userIdParam);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserRoles WHERE UserId = @UserId", userIdParam);
                
                // Xóa Properties và các bản ghi liên quan của Agents
                if (user.Agents != null && user.Agents.Any())
                {
                    foreach (var agent in user.Agents)
                    {
                        var agentIdParam = new Microsoft.Data.SqlClient.SqlParameter("@AgentId", agent.AgentId);
                        
                        // Lấy danh sách PropertyId
                        var propertyIds = await _context.Properties
                            .Where(p => p.AgentId == agent.AgentId)
                            .Select(p => p.PropertyId)
                            .ToListAsync();
                        
                        if (propertyIds.Any())
                        {
                            // Xóa các bản ghi liên quan đến Properties
                            foreach (var propertyId in propertyIds)
                            {
                                var propertyIdParam = new Microsoft.Data.SqlClient.SqlParameter("@PropertyId", propertyId);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM PropertyPhotos WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM PropertyAmenities WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM PropertyFeatures WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM PropertyVisits WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM Favorites WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM Inquiries WHERE PropertyId = @PropertyId", propertyIdParam);
                                await _context.Database.ExecuteSqlRawAsync(
                                    "DELETE FROM Reviews WHERE PropertyId = @PropertyId", propertyIdParam);
                            }
                            
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM Properties WHERE AgentId = @AgentId", agentIdParam);
                        }
                        
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM Agents WHERE AgentId = @AgentId", agentIdParam);
                    }
                }
                
                // Cuối cùng xóa User
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Users WHERE UserId = @UserId", userIdParam);

                return Json(new { success = true, message = "Đã xóa tài khoản thành công" });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết với inner exception
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;
                }
                return Json(new { success = false, message = "Không thể xóa tài khoản! Lỗi: " + errorMessage });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new { r.RoleId, r.RoleName })
                .ToListAsync();
            return Json(roles);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, int roleId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            // Kiểm tra role có tồn tại không
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                return Json(new { success = false, message = "Vai trò không tồn tại" });
            }

            // Xóa tất cả UserRoles cũ
            var oldUserRoles = user.UserRoles.ToList();
            foreach (var oldUserRole in oldUserRoles)
            {
                _context.UserRoles.Remove(oldUserRole);
            }

            // Tạo UserRole mới
            var newUserRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(newUserRole);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật vai trò thành công", roleName = role.RoleName });
        }
    }
}