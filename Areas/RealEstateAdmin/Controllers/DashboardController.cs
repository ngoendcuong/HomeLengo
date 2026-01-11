// Areas/Admin/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class DashboardController : Controller
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
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthRevenue = _context.Transactions
                .Where(t => t.CreatedAt.HasValue && 
                           t.CreatedAt.Value.Month == currentMonth && 
                           t.CreatedAt.Value.Year == currentYear &&
                           t.Status == "completed")
                .Sum(t => (decimal?)t.Amount) ?? 0;
            
            // Bất động sản chờ duyệt (giả sử status "Chờ duyệt" có StatusId = 5)
            var pendingStatus = _context.PropertyStatuses.FirstOrDefault(s => s.Name.Contains("Chờ duyệt") || s.Name.Contains("Pending"));
            var pendingProperties = pendingStatus != null 
                ? _context.Properties.Count(p => p.StatusId == pendingStatus.StatusId)
                : 0;
            
            // Bất động sản đang bán (giả sử status "Đang bán" có StatusId = 1)
            var activeStatus = _context.PropertyStatuses.FirstOrDefault(s => s.Name.Contains("Đang bán") || s.Name.Contains("Active"));
            var activeProperties = activeStatus != null 
                ? _context.Properties.Count(p => p.StatusId == activeStatus.StatusId)
                : _context.Properties.Count();
            
            // Bất động sản cho thuê (giả sử status "Cho thuê" có StatusId = 2)
            var rentStatus = _context.PropertyStatuses.FirstOrDefault(s => s.Name.Contains("Cho thuê") || s.Name.Contains("Rent"));
            var rentProperties = rentStatus != null 
                ? _context.Properties.Count(p => p.StatusId == rentStatus.StatusId)
                : 0;

            ViewBag.TotalProperties = totalProperties;
            ViewBag.TotalAgents = totalAgents;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.MonthRevenue = monthRevenue;
            ViewBag.PendingProperties = pendingProperties;
            ViewBag.ActiveProperties = activeProperties;
            ViewBag.RentProperties = rentProperties;

            return View();
        }
    }
}