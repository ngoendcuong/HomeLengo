using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;
using HomeLengo.Services;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly HomeLengoContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserController(HomeLengoContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string searchString, bool? isActive, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.Username.Contains(searchString) || 
                                         u.Email.Contains(searchString) ||
                                         (u.FullName != null && u.FullName.Contains(searchString)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.IsActive = isActive;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(users);
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Agents)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Roles = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewBag.SelectedRoles = user.UserRoles.Select(ur => ur.RoleId).ToArray();
            return View(user);
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user, int[] selectedRoles)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.ModifiedAt = DateTime.UtcNow;
                    _context.Update(user);

                    // Update roles
                    var existingRoles = _context.UserRoles.Where(ur => ur.UserId == id).ToList();
                    _context.UserRoles.RemoveRange(existingRoles);
                    if (selectedRoles != null)
                    {
                        foreach (var roleId in selectedRoles)
                        {
                            _context.UserRoles.Add(new UserRole
                            {
                                UserId = user.UserId,
                                RoleId = roleId,
                                AssignedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewBag.SelectedRoles = selectedRoles ?? new int[0];
            return View(user);
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
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

        // GET: Admin/User/Profile
        public async Task<IActionResult> Profile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Agents)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Get agent info if exists
            var agent = user.Agents.FirstOrDefault();
            ViewBag.Agent = agent;
            ViewBag.Roles = _context.Roles.ToList();
            return View(user);
        }

        // POST: Admin/User/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User user, IFormFile? avatarFile, IFormFile? posterFile, string? agencyName, string? licenseNumber, string? bio)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Validate required fields manually
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                TempData["ErrorMessage"] = "Họ tên không được để trống";
                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["ErrorMessage"] = "Email không được để trống";
                return RedirectToAction(nameof(Profile));
            }

            if (userId != user.UserId)
            {
                return NotFound();
            }

            try
            {
                var existingUser = await _context.Users
                    .Include(u => u.Agents)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (existingUser == null)
                {
                    return NotFound();
                }

                // Update user info
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.ModifiedAt = DateTime.UtcNow;

                // Handle avatar upload
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh định dạng: JPG, JPEG, PNG, GIF";
                    }
                    else if (avatarFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                    }
                    else
                    {
                        var avatarFileName = await SaveFile(avatarFile, "avatar");
                        if (!string.IsNullOrEmpty(avatarFileName))
                        {
                            // Delete old avatar if exists and is not default
                            if (!string.IsNullOrEmpty(existingUser.Avatar) && 
                                existingUser.Avatar != "avatarMacDinh.jpg" &&
                                !existingUser.Avatar.StartsWith("http"))
                            {
                                try
                                {
                                    var oldAvatarPath = Path.Combine(_environment.WebRootPath, "assets", "images", "avatar", existingUser.Avatar);
                                    if (System.IO.File.Exists(oldAvatarPath))
                                    {
                                        System.IO.File.Delete(oldAvatarPath);
                                    }
                                }
                                catch
                                {
                                    // Ignore deletion errors
                                }
                            }
                            
                            existingUser.Avatar = avatarFileName;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Không thể lưu file ảnh. Vui lòng thử lại.";
                        }
                    }
                }

                // Update or create agent info
                var agent = existingUser.Agents.FirstOrDefault();
                if (agent != null)
                {
                    // Update existing agent
                    if (!string.IsNullOrEmpty(agencyName))
                        agent.AgencyName = agencyName;
                    if (!string.IsNullOrEmpty(licenseNumber))
                        agent.LicenseNumber = licenseNumber;
                    if (!string.IsNullOrEmpty(bio))
                        agent.Bio = bio;

                    // Handle poster upload for agent
                    if (posterFile != null && posterFile.Length > 0)
                    {
                        // Note: Agent model doesn't have a poster field, so we'll skip this for now
                        // You can add a Poster field to Agent model if needed
                    }
                }
                else if (!string.IsNullOrEmpty(agencyName) || !string.IsNullOrEmpty(licenseNumber))
                {
                    // Create new agent if agency info is provided
                    agent = new Agent
                    {
                        UserId = userId,
                        AgencyName = agencyName,
                        LicenseNumber = licenseNumber,
                        Bio = bio,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Agents.Add(agent);
                }

                // Update session
                HttpContext.Session.SetString("FullName", existingUser.FullName ?? "");
                HttpContext.Session.SetString("Email", existingUser.Email);
                if (!string.IsNullOrEmpty(existingUser.Avatar))
                {
                    HttpContext.Session.SetString("Avatar", existingUser.Avatar);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating profile: " + ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        // POST: Admin/User/ChangePassword
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu mới và xác nhận không khớp" });
            }

            if (newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Verify old password
            if (!PasswordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            {
                return Json(new { success = false, message = "Mật khẩu cũ không đúng" });
            }

            // Update password
            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        // Helper method to save uploaded file
        private async Task<string?> SaveFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "images", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Sanitize filename
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var sanitizedFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
                return null;
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
