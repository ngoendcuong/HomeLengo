using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class ReviewsController : BaseController
    {
        private readonly HomeLengoContext _context;

        public ReviewsController(HomeLengoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Property)
                    .ThenInclude(p => p.Agent)
                        .ThenInclude(a => a.User)
                .Select(r => new
                {
                    Id = r.ReviewId,
                    User = r.User != null ? (r.User.FullName ?? r.User.Username) : "N/A",
                    Property = r.Property != null ? r.Property.Title : "N/A",
                    Agent = r.Property != null && r.Property.Agent != null && r.Property.Agent.User != null
                        ? (r.Property.Agent.User.FullName ?? r.Property.Agent.User.Username)
                        : "N/A",
                    Rating = (int)r.Rating,
                    Comment = r.Body ?? "",
                    Status = r.IsApproved == true ? "Đã duyệt" : "Chờ duyệt",
                    CreatedDate = r.CreatedAt.HasValue
                        ? r.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm")
                        : ""
                })
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(reviews);
        }
    }
}