using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InquiryController : Controller
    {
        private readonly HomeLengoContext _context;

        public InquiryController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Inquiry
        public async Task<IActionResult> Index(string searchString, string status, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var query = _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.User)
                .AsQueryable();

            // Filter: inquiries của properties thuộc agent hoặc của user
            if (agentId.HasValue)
            {
                query = query.Where(i => i.Property.AgentId == agentId);
            }
            else
            {
                query = query.Where(i => i.UserId == userId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i => i.Property.Title.Contains(searchString) ||
                                         i.ContactName.Contains(searchString) ||
                                         i.ContactEmail.Contains(searchString) ||
                                         i.ContactPhone.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var totalCount = await query.CountAsync();
            var inquiries = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.Statuses = new List<string> { "new", "contacted", "viewed", "closed" };
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(inquiries);
        }

        // GET: Admin/Inquiry/Details/5
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

            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.User)
                .FirstOrDefaultAsync(m => m.InquiryId == id);

            if (inquiry == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (agentId.HasValue)
            {
                if (inquiry.Property.AgentId != agentId)
                {
                    return NotFound();
                }
            }
            else
            {
                if (inquiry.UserId != userId)
                {
                    return NotFound();
                }
            }

            return View(inquiry);
        }

        // POST: Admin/Inquiry/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Inquiry/Delete/5
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

            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .FirstOrDefaultAsync(i => i.InquiryId == id);

            if (inquiry != null)
            {
                // Kiểm tra quyền xóa
                bool canDelete = false;
                if (agentId.HasValue)
                {
                    canDelete = inquiry.Property.AgentId == agentId;
                }
                else
                {
                    canDelete = inquiry.UserId == userId;
                }

                if (canDelete)
                {
                    _context.Inquiries.Remove(inquiry);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}














