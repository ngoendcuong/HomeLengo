using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;
using HomeLengo.Services;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly HomeLengoContext _context;
        private readonly ServicePackageService _packageService;

        public HomeController(HomeLengoContext context, ServicePackageService packageService)
        {
            _context = context;
            _packageService = packageService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Lấy thông tin gói dịch vụ đang active (nếu có)
            var activePackage = await _packageService.GetActivePackageAsync(userId);
            var hasPackage = activePackage != null;
            ViewBag.ActivePackage = activePackage;
            ViewBag.HasPackage = hasPackage;

            // Lấy AgentId của user (nếu có)
            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            var pendingStatus = _context.PropertyStatuses.FirstOrDefault(s => s.Name.ToLower().Contains("pending"));
            var pendingStatusId = pendingStatus?.StatusId ?? 0;

            // Filter properties theo AgentId (chỉ hiển thị nếu có gói dịch vụ)
            var propertiesQuery = _context.Properties.AsQueryable();
            if (hasPackage && agentId.HasValue)
            {
                propertiesQuery = propertiesQuery.Where(p => p.AgentId == agentId.Value);
            }
            else
            {
                // Nếu không có gói dịch vụ hoặc không phải agent, không có properties
                propertiesQuery = propertiesQuery.Where(p => false);
            }

            // Filter bookings: của agent hoặc của user
            var bookingsQuery = _context.Bookings.AsQueryable();
            if (agentId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.AgentId == agentId || b.UserId == userId);
            }
            else
            {
                bookingsQuery = bookingsQuery.Where(b => b.UserId == userId);
            }

            // Filter inquiries: của properties thuộc agent
            var inquiriesQuery = _context.Inquiries.AsQueryable();
            if (agentId.HasValue)
            {
                inquiriesQuery = inquiriesQuery.Where(i => i.Property.AgentId == agentId);
            }
            else
            {
                inquiriesQuery = inquiriesQuery.Where(i => i.UserId == userId);
            }

            // Filter reviews: của properties thuộc agent (nếu có gói) hoặc reviews của user
            var reviewsQuery = _context.Reviews.AsQueryable();
            if (hasPackage && agentId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Property.AgentId == agentId);
            }
            else
            {
                // Nếu không có gói, chỉ hiển thị reviews của chính user
                reviewsQuery = reviewsQuery.Where(r => r.UserId == userId);
            }

            // Filter messages: từ hoặc đến user
            var messagesQuery = _context.Messages.Where(m => m.FromUserId == userId || m.ToUserId == userId);

            // Filter blogs: của user
            var blogsQuery = _context.Blogs.Where(b => b.AuthorId == userId);

            // Execute queries trước
            var recentPropertiesList = propertiesQuery
                .Include(p => p.PropertyPhotos)
                .Include(p => p.Status)
                .Include(p => p.PropertyType)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            var recentMessagesList = messagesQuery
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToList();

            var recentReviewsList = reviewsQuery
                .Include(r => r.User)
                .Include(r => r.Property)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();

            var stats = new
            {
                TotalProperties = propertiesQuery.Count(),
                PendingProperties = pendingStatusId > 0 ? propertiesQuery.Count(p => p.StatusId == pendingStatusId) : 0,
                TotalUsers = 1, // Chỉ hiển thị user hiện tại
                TotalBookings = bookingsQuery.Count(),
                PendingBookings = bookingsQuery.Count(b => b.Status == "pending"),
                TotalInquiries = inquiriesQuery.Count(),
                NewInquiries = inquiriesQuery.Count(i => i.Status == "new"),
                TotalBlogs = blogsQuery.Count(),
                PublishedBlogs = blogsQuery.Count(b => b.IsPublished == true),
                TotalReviews = reviewsQuery.Count(),
                PendingReviews = reviewsQuery.Count(r => r.IsApproved == false),
                RecentProperties = recentPropertiesList,
                RecentMessages = recentMessagesList,
                RecentReviews = recentReviewsList
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}
