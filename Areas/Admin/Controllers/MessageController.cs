using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MessageController : Controller
    {
        private readonly HomeLengoContext _context;

        public MessageController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Message
        public async Task<IActionResult> Index(string searchString, bool? isRead, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Messages
                .Where(m => m.FromUserId == userId || m.ToUserId == userId)
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .Include(m => m.Property)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => (m.Subject != null && m.Subject.Contains(searchString)) ||
                                         (m.Body != null && m.Body.Contains(searchString)) ||
                                         (m.FromUser != null && m.FromUser.Username.Contains(searchString)) ||
                                         (m.ToUser != null && m.ToUser.Username.Contains(searchString)));
            }

            if (isRead.HasValue)
            {
                query = query.Where(m => m.IsRead == isRead.Value);
            }

            var totalCount = await query.CountAsync();
            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.IsRead = isRead;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(messages);
        }

        // GET: Admin/Message/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .Include(m => m.Property)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (message.FromUserId != userId && message.ToUserId != userId)
            {
                return NotFound();
            }

            // Đánh dấu đã đọc nếu user là người nhận
            if (message.ToUserId == userId && message.IsRead == false)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // POST: Admin/Message/MarkAsRead/5
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == id && m.ToUserId == userId);

            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Message/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == id && (m.FromUserId == userId || m.ToUserId == userId));

            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
