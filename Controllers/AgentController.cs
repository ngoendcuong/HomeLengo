using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Controllers
{
    public class AgentController : Controller
    {
        private readonly HomeLengoContext _context;

        public AgentController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Agent/Profile/5
        public async Task<IActionResult> Profile(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy agent với các thông tin liên quan
            var agent = await _context.Agents
                .Include(a => a.User)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.PropertyPhotos)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.PropertyType)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.Status)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.City)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.District)
                .FirstOrDefaultAsync(a => a.AgentId == id);

            if (agent == null || agent.User == null)
            {
                return NotFound();
            }

            // Lấy tất cả properties của agent
            var properties = agent.Properties
                .Where(p => p.StatusId != 3) // Loại trừ đã bán
                .OrderByDescending(p => p.CreatedAt.HasValue ? p.CreatedAt.Value : DateTime.MinValue)
                .ToList();

            // Lấy tất cả reviews cho các properties của agent này
            var propertyIds = properties.Select(p => p.PropertyId).ToList();
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Property)
                .Where(r => propertyIds.Contains(r.PropertyId) && r.IsApproved == true)
                .OrderByDescending(r => r.CreatedAt.HasValue ? r.CreatedAt.Value : DateTime.MinValue)
                .ToListAsync();

            // Tính toán thống kê
            var totalProperties = properties.Count;
            var totalViews = properties.Sum(p => p.Views ?? 0);
            var averageRating = reviews.Any() 
                ? reviews.Average(r => (double)r.Rating) 
                : 0.0;
            var totalReviews = reviews.Count;

            // Phân loại reviews theo rating
            var ratingDistribution = new Dictionary<int, int>
            {
                { 5, reviews.Count(r => r.Rating == 5) },
                { 4, reviews.Count(r => r.Rating == 4) },
                { 3, reviews.Count(r => r.Rating == 3) },
                { 2, reviews.Count(r => r.Rating == 2) },
                { 1, reviews.Count(r => r.Rating == 1) }
            };

            ViewBag.TotalProperties = totalProperties;
            ViewBag.TotalViews = totalViews;
            ViewBag.AverageRating = averageRating;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.RatingDistribution = ratingDistribution;
            ViewBag.Reviews = reviews;
            ViewBag.Properties = properties;

            return View(agent);
        }
    }
}

