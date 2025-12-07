using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PropertyController : Controller
    {
        private readonly HomeLengoContext _context;

        public PropertyController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Property
        public async Task<IActionResult> Index(string searchString, int? statusId, int? propertyTypeId, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Lấy AgentId của user
            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                // Nếu không phải agent, không có properties
                ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", statusId);
                ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", propertyTypeId);
                ViewBag.SearchString = searchString;
                ViewBag.StatusId = statusId;
                ViewBag.PropertyTypeId = propertyTypeId;
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 0;
                ViewBag.TotalCount = 0;
                return View(new List<Property>());
            }

            var query = _context.Properties
                .Where(p => p.AgentId == agent.AgentId)
                .Include(p => p.Status)
                .Include(p => p.PropertyType)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.PropertyPhotos)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Title.Contains(searchString) || 
                                         p.Description.Contains(searchString) ||
                                         p.Address.Contains(searchString));
            }

            if (statusId.HasValue && statusId.Value > 0)
            {
                query = query.Where(p => p.StatusId == statusId.Value);
            }

            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
            {
                query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            var totalCount = await query.CountAsync();
            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", statusId);
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", propertyTypeId);
            ViewBag.SearchString = searchString;
            ViewBag.StatusId = statusId;
            ViewBag.PropertyTypeId = propertyTypeId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(properties);
        }

        // GET: Admin/Property/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return NotFound();
            }

            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Where(p => p.AgentId == agent.AgentId)
                .Include(p => p.Status)
                .Include(p => p.PropertyType)
                .Include(p => p.City)
                .Include(p => p.District)
                .Include(p => p.Neighborhood)
                .Include(p => p.Agent)
                    .ThenInclude(a => a.User)
                .Include(p => p.PropertyPhotos.OrderBy(pp => pp.SortOrder))
                .Include(p => p.PropertyVideos.OrderBy(pv => pv.SortOrder))
                .Include(p => p.PropertyFloorPlans.OrderBy(pfp => pfp.SortOrder))
                .Include(p => p.PropertyAmenities)
                    .ThenInclude(pa => pa.Amenity)
                .Include(p => p.PropertyFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(m => m.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // GET: Admin/Property/Create
        public IActionResult Create()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name");
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name");
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name");
            ViewBag.AgentId = agent.AgentId; // Tự động gán agent
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            return View();
        }

        // POST: Admin/Property/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property, int[] selectedAmenities, int[] selectedFeatures)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                property.AgentId = agent.AgentId; // Tự động gán agent
                property.CreatedAt = DateTime.UtcNow;
                _context.Add(property);
                await _context.SaveChangesAsync();

                // Add amenities
                if (selectedAmenities != null)
                {
                    foreach (var amenityId in selectedAmenities)
                    {
                        _context.PropertyAmenities.Add(new PropertyAmenity
                        {
                            PropertyId = property.PropertyId,
                            AmenityId = amenityId
                        });
                    }
                }

                // Add features
                if (selectedFeatures != null)
                {
                    foreach (var featureId in selectedFeatures)
                    {
                        _context.PropertyFeatures.Add(new PropertyFeature
                        {
                            PropertyId = property.PropertyId,
                            FeatureId = featureId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User), "AgentId", "User.FullName", property.AgentId);
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            return View(property);
        }

        // GET: Admin/Property/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return NotFound();
            }

            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Where(p => p.AgentId == agent.AgentId)
                .Include(p => p.PropertyAmenities)
                .Include(p => p.PropertyFeatures)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.Districts = new SelectList(_context.Districts.Where(d => d.CityId == property.CityId), "DistrictId", "Name", property.DistrictId);
            ViewBag.Neighborhoods = new SelectList(_context.Neighborhoods.Where(n => n.DistrictId == property.DistrictId), "NeighborhoodId", "Name", property.NeighborhoodId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User), "AgentId", "User.FullName", property.AgentId);
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            ViewBag.SelectedAmenities = property.PropertyAmenities.Select(pa => pa.AmenityId).ToArray();
            ViewBag.SelectedFeatures = property.PropertyFeatures.Select(pf => pf.FeatureId).ToArray();

            return View(property);
        }

        // POST: Admin/Property/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Property property, int[] selectedAmenities, int[] selectedFeatures)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return NotFound();
            }

            if (id != property.PropertyId)
            {
                return NotFound();
            }

            // Kiểm tra property thuộc về agent này
            var existingProperty = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == id && p.AgentId == agent.AgentId);
            if (existingProperty == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    property.AgentId = agent.AgentId; // Đảm bảo agent không bị thay đổi
                    property.ModifiedAt = DateTime.UtcNow;
                    _context.Update(property);

                    // Update amenities
                    var existingAmenities = _context.PropertyAmenities.Where(pa => pa.PropertyId == id).ToList();
                    _context.PropertyAmenities.RemoveRange(existingAmenities);
                    if (selectedAmenities != null)
                    {
                        foreach (var amenityId in selectedAmenities)
                        {
                            _context.PropertyAmenities.Add(new PropertyAmenity
                            {
                                PropertyId = property.PropertyId,
                                AmenityId = amenityId
                            });
                        }
                    }

                    // Update features
                    var existingFeatures = _context.PropertyFeatures.Where(pf => pf.PropertyId == id).ToList();
                    _context.PropertyFeatures.RemoveRange(existingFeatures);
                    if (selectedFeatures != null)
                    {
                        foreach (var featureId in selectedFeatures)
                        {
                            _context.PropertyFeatures.Add(new PropertyFeature
                            {
                                PropertyId = property.PropertyId,
                                FeatureId = featureId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.Districts = new SelectList(_context.Districts.Where(d => d.CityId == property.CityId), "DistrictId", "Name", property.DistrictId);
            ViewBag.Neighborhoods = new SelectList(_context.Neighborhoods.Where(n => n.DistrictId == property.DistrictId), "NeighborhoodId", "Name", property.NeighborhoodId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User), "AgentId", "User.FullName", property.AgentId);
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            ViewBag.SelectedAmenities = selectedAmenities ?? new int[0];
            ViewBag.SelectedFeatures = selectedFeatures ?? new int[0];
            return View(property);
        }

        // GET: Admin/Property/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return NotFound();
            }

            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Where(p => p.AgentId == agent.AgentId)
                .Include(p => p.Status)
                .Include(p => p.PropertyType)
                .Include(p => p.City)
                .FirstOrDefaultAsync(m => m.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // POST: Admin/Property/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Where(p => p.AgentId == agent.AgentId)
                .Include(p => p.PropertyAmenities)
                .Include(p => p.PropertyFeatures)
                .Include(p => p.PropertyPhotos)
                .Include(p => p.PropertyVideos)
                .Include(p => p.PropertyFloorPlans)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property != null)
            {
                _context.PropertyAmenities.RemoveRange(property.PropertyAmenities);
                _context.PropertyFeatures.RemoveRange(property.PropertyFeatures);
                _context.PropertyPhotos.RemoveRange(property.PropertyPhotos);
                _context.PropertyVideos.RemoveRange(property.PropertyVideos);
                _context.PropertyFloorPlans.RemoveRange(property.PropertyFloorPlans);
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Property/GetDistricts
        [HttpGet]
        public JsonResult GetDistricts(int cityId)
        {
            var districts = _context.Districts
                .Where(d => d.CityId == cityId)
                .Select(d => new { d.DistrictId, d.Name })
                .ToList();
            return Json(districts);
        }

        // GET: Admin/Property/GetNeighborhoods
        [HttpGet]
        public JsonResult GetNeighborhoods(int districtId)
        {
            var neighborhoods = _context.Neighborhoods
                .Where(n => n.DistrictId == districtId)
                .Select(n => new { n.NeighborhoodId, n.Name })
                .ToList();
            return Json(neighborhoods);
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}
