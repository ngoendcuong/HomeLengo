// Areas/Admin/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class DashboardController : BaseController
    {
        private readonly HomeLengoContext _context;

        public DashboardController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Tổng số bất động sản
            var totalProperties = _context.Properties.Count();

            // Tổng số agents
            var totalAgents = _context.Agents.Count();

            // Tổng số users
            var totalUsers = _context.Users.Count();

            // Doanh thu tháng hiện tại
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var monthRevenue =
                (_context.UserServicePackages
                    .Include(u => u.Plan)
                    .Where(u =>
                        u.CreatedAt >= startOfMonth &&
                        u.CreatedAt < startOfNextMonth)
                    .Sum(u => (decimal?)u.Plan.Price) ?? 0)
                +
                (_context.Properties
                    .Where(p =>
                        (p.StatusId == 3 || p.StatusId == 4) &&
                        p.CreatedAt >= startOfMonth &&
                        p.CreatedAt < startOfNextMonth)
                    .Sum(p => (decimal?)p.Price * 0.03m) ?? 0);

            ViewBag.MonthRevenue = monthRevenue;


            // Bất động sản chờ duyệt (giả sử StatusId = 5)
            var pendingProperties = _context.Properties.Count(p => p.StatusId == 5);

            // Bất động sản đang bán (giả sử StatusId = 1)
            var activeProperties = _context.Properties.Count(p => p.StatusId == 1);

            // Bất động sản cho thuê (StatusId = 2)
            var rentProperties = _context.Properties.Count(p => p.StatusId == 2);

            ViewBag.TotalProperties = totalProperties;
            ViewBag.TotalAgents = totalAgents;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.MonthRevenue = monthRevenue;
            ViewBag.PendingProperties = pendingProperties;
            ViewBag.ActiveProperties = activeProperties;
            ViewBag.RentProperties = rentProperties;

             // Tổng số bài viết blog
    var totalBlogs = _context.Blogs.Count();

    ViewBag.TotalProperties = totalProperties;
    ViewBag.ActiveProperties = activeProperties;
    ViewBag.PendingProperties = pendingProperties;
    ViewBag.RentProperties = rentProperties;
    ViewBag.TotalAgents = totalAgents;
    ViewBag.TotalUsers = totalUsers;
    ViewBag.MonthRevenue = monthRevenue;
    ViewBag.TotalBlogs = totalBlogs; // <-- đây là dòng bạn cần

            // ===== Biểu đồ Tin đăng theo loại hình =====
            var propertyTypes = new[]
            {
    new { Id = 1, Name = "Căn hộ" },
    new { Id = 2, Name = "Nhà phố" },
    new { Id = 3, Name = "Biệt thự" },
    new { Id = 5, Name = "Văn phòng" },
    new { Id = 8, Name = "Kinh doanh" },
    new { Id = 9, Name = "Chung cư" }
};

            var propertyTypeStats = propertyTypes
                .GroupJoin(
                    _context.Properties,
                    t => t.Id,
                    p => p.PropertyTypeId,
                    (t, p) => new
                    {
                        TypeName = t.Name,
                        Total = p.Count()
                    }
                )
                .ToList();

            // Đẩy sang View
            ViewBag.PropertyTypeLabels = propertyTypeStats.Select(x => x.TypeName).ToList();
            ViewBag.PropertyTypeData = propertyTypeStats.Select(x => x.Total).ToList();

            return View();
        }

    }
}