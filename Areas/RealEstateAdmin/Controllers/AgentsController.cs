// Areas/Admin/Controllers/AgentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class AgentsController : Controller
    {
        private readonly HomeLengoContext _context;

        public AgentsController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var agents = _context.Agents
                .Include(a => a.User)
                .Include(a => a.Properties)
                .Select(a => new
                {
                    Id = a.AgentId,
                    Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A",
                    Phone = a.User != null ? a.User.Phone ?? "" : "",
                    Email = a.User != null ? a.User.Email : "",
                    TotalProperties = a.Properties != null ? a.Properties.Count : 0,
                    TotalViews = a.Properties != null 
                        ? a.Properties.Sum(p => p.Views ?? 0) 
                        : 0,
                    Rating = 0.0, // Có thể tính từ Reviews nếu có
                    Reviews = 0, // Có thể tính từ Reviews nếu có
                    Status = a.User != null && a.User.IsActive == true ? "Active" : "Inactive",
                    Avatar = a.User != null ? (a.User.Avatar ?? "https://via.placeholder.com/80") : "https://via.placeholder.com/80"
                })
                .ToList();

            return View(agents);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var agent = await _context.Agents
                .Include(a => a.User)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.PropertyVisits)
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a => a.AgentId == id);

            if (agent == null)
            {
                return NotFound();
            }

            var totalProperties = agent.Properties?.Count ?? 0;
            var totalViews = agent.Properties?.Sum(p => p.Views ?? 0) ?? 0;
            var totalLeads = agent.Bookings?.Count ?? 0;

            var result = new
            {
                Id = agent.AgentId,
                Name = agent.User != null ? (agent.User.FullName ?? agent.User.Username) : "N/A",
                Phone = agent.User != null ? agent.User.Phone ?? "" : "",
                Email = agent.User != null ? agent.User.Email : "",
                Address = "", // Có thể thêm vào Agent model nếu cần
                TotalProperties = totalProperties,
                TotalViews = totalViews,
                TotalLeads = totalLeads,
                Rating = 0.0, // Có thể tính từ Reviews nếu có
                Reviews = 0, // Có thể tính từ Reviews nếu có
                Status = agent.User != null && agent.User.IsActive == true ? "Active" : "Inactive",
                Avatar = agent.User != null ? (agent.User.Avatar ?? "https://via.placeholder.com/150") : "https://via.placeholder.com/150",
                Bio = agent.Bio ?? ""
            };

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent != null)
            {
                _context.Agents.Remove(agent);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}