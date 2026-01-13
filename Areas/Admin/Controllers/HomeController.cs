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

            // 1) Gói dịch vụ đang active
            var activePackage = await _packageService.GetActivePackageAsync(userId);
            var hasPackage = activePackage != null;

            ViewBag.ActivePackage = activePackage;
            ViewBag.HasPackage = hasPackage;

            // 2) AgentId của user
            var agent = await _context.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);
            var agentId = agent?.AgentId;

            // 3) Properties query: chỉ có khi có gói + là agent
            IQueryable<Property> propertiesQuery = _context.Properties.AsQueryable();

            if (hasPackage && agentId.HasValue)
            {
                propertiesQuery = propertiesQuery.Where(p => p.AgentId == agentId.Value);
            }
            else
            {
                propertiesQuery = propertiesQuery.Where(p => false);
            }

            // ✅ CHỈ TÍNH TIN ĐANG HOẠT ĐỘNG: StatusId 1 (Bán) hoặc 2 (Thuê)
            var activeListingsQuery = propertiesQuery.Where(p => p.StatusId == 1 || p.StatusId == 2);

            // 4) Bookings
            IQueryable<Booking> bookingsQuery = _context.Bookings.AsQueryable();
            if (agentId.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.AgentId == agentId || b.UserId == userId);
            else
                bookingsQuery = bookingsQuery.Where(b => b.UserId == userId);

            // 5) Inquiries
            IQueryable<Inquiry> inquiriesQuery = _context.Inquiries.AsQueryable();
            if (agentId.HasValue)
                inquiriesQuery = inquiriesQuery.Where(i => i.Property.AgentId == agentId);
            else
                inquiriesQuery = inquiriesQuery.Where(i => i.UserId == userId);

            // 6) Reviews
            IQueryable<Review> reviewsQuery = _context.Reviews.AsQueryable();
            if (hasPackage && agentId.HasValue)
                reviewsQuery = reviewsQuery.Where(r => r.Property.AgentId == agentId);
            else
                reviewsQuery = reviewsQuery.Where(r => r.UserId == userId);

            // 7) Messages
            var messagesQuery = _context.Messages.Where(m => m.FromUserId == userId || m.ToUserId == userId);

            // 8) Blogs
            var blogsQuery = _context.Blogs.Where(b => b.AuthorId == userId);

            // ====== COUNTS (Async) ======
            var totalPropertiesActive = await activeListingsQuery.CountAsync();     // ✅ tổng tin đang còn hiệu lực (1/2)
            var forSaleCount = await propertiesQuery.CountAsync(p => p.StatusId == 1); // ✅ rao bán
            var forRentCount = await propertiesQuery.CountAsync(p => p.StatusId == 2); // ✅ cho thuê

            // ✅ Tin còn lại = MaxListings - totalPropertiesActive
            int? remainingListings = null;
            if (activePackage?.Plan?.MaxListings.HasValue == true && activePackage.Plan.MaxListings.Value > 0)
            {
                remainingListings = activePackage.Plan.MaxListings.Value - totalPropertiesActive;
                if (remainingListings < 0) remainingListings = 0;
            }

            // ====== LISTS ======
            var recentPropertiesList = await propertiesQuery
                .Include(p => p.PropertyPhotos)
                .Include(p => p.Status)
                .Include(p => p.PropertyType)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentMessagesList = await messagesQuery
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            var recentReviewsList = await reviewsQuery
                .Include(r => r.User)
                .Include(r => r.Property)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            // ====== STATS ======
            var stats = new
            {
                TotalProperties = totalPropertiesActive,   // ✅ tổng tin đang còn hiệu lực (StatusId 1/2)

                // ✅ 2 ô mới bạn muốn:
                ForSaleCount = forSaleCount,               // ✅ Rao bán
                ForRentCount = forRentCount,               // ✅ Cho thuê

                // ✅ để hiển thị "đang dùng" & "còn lại"
                ActiveListingsCount = totalPropertiesActive,
                RemainingListings = remainingListings,

                TotalUsers = 1,
                TotalBookings = await bookingsQuery.CountAsync(),
                PendingBookings = await bookingsQuery.CountAsync(b => b.Status == "pending"),
                TotalInquiries = await inquiriesQuery.CountAsync(),
                NewInquiries = await inquiriesQuery.CountAsync(i => i.Status == "new"),
                TotalBlogs = await blogsQuery.CountAsync(),
                PublishedBlogs = await blogsQuery.CountAsync(b => b.IsPublished == true),
                TotalReviews = await reviewsQuery.CountAsync(),
                PendingReviews = await reviewsQuery.CountAsync(r => r.IsApproved == false),

                RecentProperties = recentPropertiesList,
                RecentMessages = recentMessagesList,
                RecentReviews = recentReviewsList
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}
