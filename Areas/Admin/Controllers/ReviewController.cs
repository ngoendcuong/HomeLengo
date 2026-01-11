using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;
using HomeLengo.Services;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewController : Controller
    {
        private readonly HomeLengoContext _context;
        private readonly ServicePackageService _packageService;

        public ReviewController(HomeLengoContext context, ServicePackageService packageService)
        {
            _context = context;
            _packageService = packageService;
        }

        // GET: Admin/Review
        public async Task<IActionResult> Index(string searchString, bool? isApproved, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Kiểm tra user có gói dịch vụ không (để hiển thị reviews của properties nếu có)
            var hasPackage = await _packageService.HasActivePackageAsync(userId);
            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Property)
                .AsQueryable();

            // Filter: nếu có gói và là agent, hiển thị reviews của properties thuộc agent
            // Nếu không có gói hoặc không phải agent, chỉ hiển thị reviews của chính user
            if (hasPackage && agentId.HasValue)
            {
                query = query.Where(r => r.Property.AgentId == agentId);
            }
            else
            {
                // Hiển thị reviews của chính user (chức năng cơ bản)
                query = query.Where(r => r.UserId == userId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r => r.Property.Title.Contains(searchString) ||
                                         (r.Title != null && r.Title.Contains(searchString)) ||
                                         (r.User != null && r.User.Username.Contains(searchString)));
            }

            if (isApproved.HasValue)
            {
                query = query.Where(r => r.IsApproved == isApproved.Value);
            }

            var totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.IsApproved = isApproved;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(reviews);
        }

        // GET: Admin/Review/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Property)
                .FirstOrDefaultAsync(m => m.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (agentId.HasValue)
            {
                if (review.Property.AgentId != agentId)
                {
                    return NotFound();
                }
            }
            else
            {
                if (review.UserId != userId)
                {
                    return NotFound();
                }
            }

            return View(review);
        }

        // POST: Admin/Review/ToggleApproved/5
        [HttpPost]
        public async Task<IActionResult> ToggleApproved(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var review = await _context.Reviews
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review != null)
            {
                // Kiểm tra quyền
                bool canUpdate = false;
                if (agentId.HasValue)
                {
                    canUpdate = review.Property.AgentId == agentId;
                }
                else
                {
                    canUpdate = review.UserId == userId;
                }

                if (canUpdate)
                {
                    review.IsApproved = !review.IsApproved;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var review = await _context.Reviews
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review != null)
            {
                // Kiểm tra quyền
                bool canDelete = false;
                if (agentId.HasValue)
                {
                    canDelete = review.Property.AgentId == agentId;
                }
                else
                {
                    canDelete = review.UserId == userId;
                }

                if (canDelete)
                {
                    _context.Reviews.Remove(review);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}














