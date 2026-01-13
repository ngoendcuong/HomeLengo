// Areas/Admin/Controllers/AgentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class AgentsController : BaseController
    {
        private readonly HomeLengoContext _context;

        public AgentsController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult Index(string searchString, string status, string sortBy)
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Agents
                .Include(a => a.User)
                .Include(a => a.Properties)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a => 
                    (a.User != null && (a.User.FullName != null && a.User.FullName.Contains(searchString) ||
                                       a.User.Username != null && a.User.Username.Contains(searchString) ||
                                       a.User.Email != null && a.User.Email.Contains(searchString) ||
                                       a.User.Phone != null && a.User.Phone.Contains(searchString))));
            }

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    query = query.Where(a => a.User != null && a.User.IsActive == true);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(a => a.User == null || a.User.IsActive != true);
                }
            }

            var agentsList = query.ToList();

            // Tính toán rating và reviews từ database
            var agents = agentsList.Select(a =>
            {
                var properties = a.Properties?.ToList() ?? new List<Property>();
                var propertyIds = properties.Select(p => p.PropertyId).ToList();
                
                // Tính rating từ reviews của properties của agent này
                var reviews = _context.Reviews
                    .Where(r => propertyIds.Contains(r.PropertyId))
                    .ToList();
                
                var avgRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0.0;
                var reviewCount = reviews.Count;

                // Xử lý avatar path
                string avatarPath;
                if (a.User == null || string.IsNullOrEmpty(a.User.Avatar))
                {
                    avatarPath = "/assets/images/avatar/avatarMacDinh.jpg";
                }
                else if (a.User.Avatar.StartsWith("http://") || a.User.Avatar.StartsWith("https://"))
                {
                    avatarPath = a.User.Avatar;
                }
                else if (a.User.Avatar.StartsWith("/"))
                {
                    avatarPath = a.User.Avatar;
                }
                else
                {
                    avatarPath = "/assets/images/avatar/" + a.User.Avatar.TrimStart('/');
                }

                return new
                {
                    Id = a.AgentId,
                    Name = a.User != null ? (a.User.FullName ?? a.User.Username) : "N/A",
                    Phone = a.User != null ? a.User.Phone ?? "" : "",
                    Email = a.User != null ? a.User.Email : "",
                    TotalProperties = properties.Count,
                    TotalViews = properties.Sum(p => p.Views ?? 0),
                    Rating = Math.Round(avgRating, 1),
                    Reviews = reviewCount,
                    Status = a.User != null && a.User.IsActive == true ? "Active" : "Inactive",
                    Avatar = avatarPath
                };
            }).ToList();

            // Sắp xếp
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy)
                {
                    case "properties":
                        agents = agents.OrderByDescending(a => a.TotalProperties).ToList();
                        break;
                    case "rating":
                        agents = agents.OrderByDescending(a => a.Rating).ToList();
                        break;
                    case "newest":
                        agents = agents.OrderByDescending(a => a.Id).ToList();
                        break;
                }
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.SortBy = sortBy;

            return View(agents);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var agent = await _context.Agents
                .Include(a => a.User)
                .Include(a => a.Properties)
                    .ThenInclude(p => p.Status)
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

            // Tính rating và reviews từ properties của agent
            var propertyIds = agent.Properties?.Select(p => p.PropertyId).ToList() ?? new List<int>();
            var reviews = _context.Reviews
                .Where(r => propertyIds.Contains(r.PropertyId))
                .ToList();
            
            var avgRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0.0;
            var reviewCount = reviews.Count;

            // Xử lý avatar path
            string avatarPath;
            if (agent.User == null || string.IsNullOrEmpty(agent.User.Avatar))
            {
                avatarPath = "/assets/images/avatar/avatarMacDinh.jpg";
            }
            else if (agent.User.Avatar.StartsWith("http://") || agent.User.Avatar.StartsWith("https://"))
            {
                avatarPath = agent.User.Avatar;
            }
            else if (agent.User.Avatar.StartsWith("/"))
            {
                avatarPath = agent.User.Avatar;
            }
            else
            {
                avatarPath = "/assets/images/avatar/" + agent.User.Avatar.TrimStart('/');
            }

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
                Rating = Math.Round(avgRating, 1),
                Reviews = reviewCount,
                Status = agent.User != null && agent.User.IsActive == true ? "Active" : "Inactive",
                Avatar = avatarPath,
                Bio = agent.Bio ?? "",
                Properties = agent.Properties != null && agent.Properties.Any()
                    ? agent.Properties.OrderByDescending(p => p.CreatedAt).Take(10).Select(p => new
                    {
                        Id = p.PropertyId,
                        Title = p.Title ?? "N/A",
                        Price = p.Price,
                        Currency = p.Currency ?? "VNĐ",
                        Views = p.Views ?? 0,
                        Status = p.Status != null ? p.Status.Name : "N/A"
                    }).Cast<object>().ToList()
                    : new List<object>()
            };

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Lock(int id)
        {
            // Lấy agent + user
            var agent = await _context.Agents
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AgentId == id);

            if (agent == null)
                return NotFound();

            if (agent.User != null)
            {
                agent.User.IsActive = false; // 🔒 KHÓA MÔI GIỚI
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Index",
                "Agents",
                new { area = "RealEstateAdmin" }
            );
        }
        [HttpPost]
        public async Task<IActionResult> Unlock(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = await _context.Agents
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AgentId == id);

            if (agent == null || agent.User == null)
            {
                return NotFound();
            }

            agent.User.IsActive = true; // MỞ KHÓA

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Agents", new { area = "RealEstateAdmin" });
        }
    }
}