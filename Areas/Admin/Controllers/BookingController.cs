using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingController : Controller
    {
        private readonly HomeLengoContext _context;

        public BookingController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Booking
        public async Task<IActionResult> Index(string searchString, string status, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var query = _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.User)
                .Include(b => b.Agent)
                    .ThenInclude(a => a.User)
                .AsQueryable();

            // Filter: bookings của agent hoặc của user
            if (agentId.HasValue)
            {
                query = query.Where(b => b.AgentId == agentId || b.UserId == userId);
            }
            else
            {
                query = query.Where(b => b.UserId == userId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Property.Title.Contains(searchString) ||
                                         b.User.Username.Contains(searchString) ||
                                         b.User.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var totalCount = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.Statuses = new List<string> { "pending", "confirmed", "cancelled", "completed" };
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(bookings);
        }

        // GET: Admin/Booking/Details/5
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

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.User)
                .Include(b => b.Agent)
                    .ThenInclude(a => a.User)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (agentId.HasValue)
            {
                if (booking.AgentId != agentId && booking.UserId != userId)
                {
                    return NotFound();
                }
            }
            else
            {
                if (booking.UserId != userId)
                {
                    return NotFound();
                }
            }

            return View(booking);
        }

        // POST: Admin/Booking/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                // Kiểm tra quyền cập nhật
                bool canUpdate = false;
                if (agentId.HasValue)
                {
                    canUpdate = booking.AgentId == agentId || booking.UserId == userId;
                }
                else
                {
                    canUpdate = booking.UserId == userId;
                }

                if (canUpdate)
                {
                    booking.Status = status;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
