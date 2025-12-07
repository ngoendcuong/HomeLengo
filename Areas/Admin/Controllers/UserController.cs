using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly HomeLengoContext _context;

        public UserController(HomeLengoContext context)
        {
            _context = context;
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

            ViewBag.Roles = _context.Roles.ToList();
            return View(user);
        }

        // POST: Admin/User/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User user, int[] selectedRoles)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (userId != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Không cho phép thay đổi password từ đây
                    var existingUser = await _context.Users.FindAsync(userId);
                    if (existingUser != null)
                    {
                        existingUser.FullName = user.FullName;
                        existingUser.Email = user.Email;
                        existingUser.Phone = user.Phone;
                        existingUser.Avatar = user.Avatar;
                        existingUser.ModifiedAt = DateTime.UtcNow;

                        // Cập nhật session nếu cần
                        HttpContext.Session.SetString("FullName", existingUser.FullName ?? "");
                        HttpContext.Session.SetString("Email", existingUser.Email);
                        if (!string.IsNullOrEmpty(existingUser.Avatar))
                        {
                            HttpContext.Session.SetString("Avatar", existingUser.Avatar);
                        }

                        // Update roles nếu có
                        if (selectedRoles != null)
                        {
                            var existingRoles = _context.UserRoles.Where(ur => ur.UserId == userId).ToList();
                            _context.UserRoles.RemoveRange(existingRoles);
                            foreach (var roleId in selectedRoles)
                            {
                                _context.UserRoles.Add(new UserRole
                                {
                                    UserId = userId,
                                    RoleId = roleId,
                                    AssignedAt = DateTime.UtcNow
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                        ViewBag.SuccessMessage = "Profile updated successfully!";
                    }
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
            }

            var updatedUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Agents)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            ViewBag.Roles = _context.Roles.ToList();
            ViewBag.SelectedRoles = updatedUser?.UserRoles.Select(ur => ur.RoleId).ToArray() ?? new int[0];
            return View(updatedUser);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
