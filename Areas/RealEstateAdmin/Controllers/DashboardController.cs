using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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
            // --- 1. XỬ LÝ THỜI GIAN ---
            var now = DateTime.Now;
            var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
            var startOfNextMonth = startOfCurrentMonth.AddMonths(1);
            var startOfLastMonth = startOfCurrentMonth.AddMonths(-1);

            // --- 2. TÍNH DOANH THU & TĂNG TRƯỞNG (Ô THỐNG KÊ) ---
            decimal currentMonthRevenue = CalculateRevenue(startOfCurrentMonth, startOfNextMonth);
            decimal lastMonthRevenue = CalculateRevenue(startOfLastMonth, startOfCurrentMonth);

            double growthPercent = 0;
            if (lastMonthRevenue > 0)
            {
                growthPercent = (double)((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
            }
            else if (currentMonthRevenue > 0) growthPercent = 100;

            ViewBag.MonthRevenue = currentMonthRevenue;
            ViewBag.GrowthPercent = Math.Round(growthPercent, 1);
            ViewBag.IsGrowthPositive = growthPercent >= 0;

            // --- 3. CÁC CHỈ SỐ COUNT (BOXES) ---
            ViewBag.TotalProperties = _context.Properties.Count();
            ViewBag.TotalAgents = _context.Agents.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalBlogs = _context.Blogs.Count();

            ViewBag.ActiveProperties = _context.Properties.Count(p => p.StatusId == 1);
            ViewBag.RentProperties = _context.Properties.Count(p => p.StatusId == 2);
            ViewBag.PendingProperties = _context.Properties.Count(p => p.StatusId == 5);

            // --- 4. DỮ LIỆU BIỂU ĐỒ 6 THÁNG (LINE CHART) ---
            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var start = new DateTime(monthDate.Year, monthDate.Month, 1);
                var end = start.AddMonths(1);

                revenueLabels.Add($"Tháng {monthDate.Month}/{monthDate.Year}");
                revenueData.Add(CalculateRevenue(start, end));
            }
            ViewBag.RevenueLabels = revenueLabels;
            ViewBag.RevenueData = revenueData;

            // --- 5. BIỂU ĐỒ TIN ĐĂNG THEO LOẠI (DOUGHNUT) ---
            var propertyTypeStats = _context.PropertyTypes
                .Select(t => new {
                    TypeName = t.Name,
                    Total = _context.Properties.Count(p => p.PropertyTypeId == t.PropertyTypeId)
                }).ToList();

            ViewBag.PropertyTypeLabels = propertyTypeStats.Select(x => x.TypeName).ToList();
            ViewBag.PropertyTypeData = propertyTypeStats.Select(x => x.Total).ToList();

            return View();
        }

        private decimal CalculateRevenue(DateTime start, DateTime end)
        {
            var serviceRevenue = _context.UserServicePackages
                .Include(u => u.Plan)
                .Where(u => u.CreatedAt >= start && u.CreatedAt < end)
                .Sum(u => (decimal?)u.Plan.Price) ?? 0;

            var commissionRevenue = _context.Properties
                .Where(p => (p.StatusId == 3 || p.StatusId == 4) &&
                            p.CreatedAt >= start && p.CreatedAt < end)
                .Sum(p => (decimal?)p.Price * 0.03m) ?? 0;

            return serviceRevenue + commissionRevenue;
        }
    }
}