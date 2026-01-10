using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace HomeLengo.Areas.Admin.Controllers
{
 
    [Area("Admin")]
    public class PropertyController : Controller
    {
        
        private readonly HomeLengoContext _context;
        private readonly IWebHostEnvironment _environment;

        public PropertyController(HomeLengoContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            ViewBag.Districts = new SelectList(new List<District>(), "DistrictId", "Name");
            ViewBag.Neighborhoods = new SelectList(new List<Neighborhood>(), "NeighborhoodId", "Name");
            ViewBag.AgentId = agent.AgentId; // Tự động gán agent
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            return View();
        }

        // POST: Admin/Property/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Property property, [FromForm] int[] selectedAmenities, [FromForm] int[] selectedFeatures, 
            [FromForm] IFormFile[] photos, [FromForm] string[] videoUrls, [FromForm] bool? IsFeatured, [FromForm] int? primaryPhotoIndex)
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

            // Remove validation errors for navigation properties
            ModelState.Remove("PropertyType");
            ModelState.Remove("Status");
            ModelState.Remove("Agent");
            ModelState.Remove("City");
            ModelState.Remove("District");
            ModelState.Remove("Neighborhood");

            // Validate PropertyTypeId and StatusId are provided
            if (property == null || property.PropertyTypeId <= 0)
            {
                ModelState.AddModelError("PropertyTypeId", "The PropertyType field is required.");
            }
            if (property == null || property.StatusId <= 0)
            {
                ModelState.AddModelError("StatusId", "The Status field is required.");
            }
            
            // Validate Title is provided
            if (property == null || string.IsNullOrWhiteSpace(property.Title))
            {
                ModelState.AddModelError("Title", "The Title field is required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    property.AgentId = agent.AgentId; // Tự động gán agent
                    property.CreatedAt = DateTime.UtcNow;
                    property.IsFeatured = IsFeatured ?? false;
                    property.Views = property.Views ?? 0;
                    _context.Add(property);
                    await _context.SaveChangesAsync();

                    // Upload and add photos
                    // Debug: Check if photos are received
                    var photosCount = photos != null ? photos.Length : 0;
                    
                    if (photos != null && photos.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "images", "property");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        int sortOrder = 1;
                        // Use primaryPhotoIndex if provided, otherwise default to first photo (index 0)
                        int primaryIndex = primaryPhotoIndex ?? 0;
                        if (primaryIndex < 0 || primaryIndex >= photos.Length)
                        {
                            primaryIndex = 0; // Ensure valid index
                        }

                        for (int i = 0; i < photos.Length; i++)
                        {
                            var photo = photos[i];
                            if (photo != null && photo.Length > 0)
                            {
                                var fileName = Path.GetFileNameWithoutExtension(photo.FileName);
                                var extension = Path.GetExtension(photo.FileName);
                                var sanitizedFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                                var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}{extension}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await photo.CopyToAsync(fileStream);
                                }

                                var propertyPhoto = new PropertyPhoto
                                {
                                    PropertyId = property.PropertyId,
                                    FilePath = $"assets/images/property/{uniqueFileName}",
                                    AltText = property.Title,
                                    IsPrimary = (i == primaryIndex), // Set primary based on index
                                    SortOrder = sortOrder++,
                                    UploadedAt = DateTime.UtcNow
                                };

                                _context.PropertyPhotos.Add(propertyPhoto);
                            }
                        }
                        
                        // Save photos immediately to ensure they are persisted
                        await _context.SaveChangesAsync();
                    }

                    // Add videos - handle both array and single values
                    var videoUrlList = new List<string>();
                    if (videoUrls != null && videoUrls.Length > 0)
                    {
                        videoUrlList.AddRange(videoUrls.Where(v => !string.IsNullOrWhiteSpace(v)));
                    }
                    // Also check Request.Form for videoUrls (in case model binding didn't work)
                    if (Request.Form.ContainsKey("videoUrls"))
                    {
                        foreach (var videoUrl in Request.Form["videoUrls"])
                        {
                            if (!string.IsNullOrWhiteSpace(videoUrl) && !videoUrlList.Contains(videoUrl))
                            {
                                videoUrlList.Add(videoUrl);
                            }
                        }
                    }
                    
                    if (videoUrlList.Count > 0)
                    {
                        int videoSortOrder = 1;
                        foreach (var videoUrl in videoUrlList)
                        {
                            string videoType = "direct";
                            if (videoUrl.Contains("youtube.com") || videoUrl.Contains("youtu.be"))
                            {
                                videoType = "youtube";
                            }
                            else if (videoUrl.Contains("vimeo.com"))
                            {
                                videoType = "vimeo";
                            }

                            var propertyVideo = new PropertyVideo
                            {
                                PropertyId = property.PropertyId,
                                VideoUrl = videoUrl.Trim(),
                                VideoType = videoType,
                                SortOrder = videoSortOrder++,
                                IsPrimary = videoSortOrder == 2, // First video is primary
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.PropertyVideos.Add(propertyVideo);
                        }
                    }

                    // Add floor plans - read from form with proper indexing
                    var floorPlanNames = new List<string>();
                    var floorPlanAreas = new List<string>();
                    var floorPlanBedrooms = new List<string>();
                    var floorPlanBathrooms = new List<string>();
                    var floorPlanDescriptions = new List<string>();
                    
                    // Read floor plan data by iterating through form keys
                    var floorPlanIndices = new HashSet<int>();
                    foreach (var key in Request.Form.Keys)
                    {
                        if (key != null && key.StartsWith("floorPlans[") && key.Contains("].FloorName"))
                        {
                            var match = Regex.Match(key, @"floorPlans\[(\d+)\]\.FloorName");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
                            {
                                floorPlanIndices.Add(index);
                            }
                        }
                    }
                    
                    // Collect floor plan data in order
                    var sortedIndices = floorPlanIndices.OrderBy(i => i).ToList();
                    foreach (var i in sortedIndices)
                    {
                        var floorNameKey = $"floorPlans[{i}].FloorName";
                        var areaKey = $"floorPlans[{i}].Area";
                        var bedroomsKey = $"floorPlans[{i}].Bedrooms";
                        var bathroomsKey = $"floorPlans[{i}].Bathrooms";
                        var descriptionKey = $"floorPlans[{i}].Description";
                        
                        if (Request.Form.ContainsKey(floorNameKey))
                        {
                            floorPlanNames.Add(Request.Form[floorNameKey].ToString());
                            floorPlanAreas.Add(Request.Form.ContainsKey(areaKey) ? Request.Form[areaKey].ToString() : "");
                            floorPlanBedrooms.Add(Request.Form.ContainsKey(bedroomsKey) ? Request.Form[bedroomsKey].ToString() : "");
                            floorPlanBathrooms.Add(Request.Form.ContainsKey(bathroomsKey) ? Request.Form[bathroomsKey].ToString() : "");
                            floorPlanDescriptions.Add(Request.Form.ContainsKey(descriptionKey) ? Request.Form[descriptionKey].ToString() : "");
                        }
                    }

                    if (floorPlanNames.Count > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "images", "property", "floorplans");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        int floorPlanSortOrder = 1;
                        for (int i = 0; i < floorPlanNames.Count; i++)
                        {
                            var floorName = floorPlanNames[i];
                            if (!string.IsNullOrWhiteSpace(floorName))
                            {
                                string imagePath = "";
                                
                                // Handle floor plan image upload
                                IFormFile floorPlanImage = null;
                                
                                // Try with dot notation first (from AJAX)
                                var floorPlanImageKey = $"floorPlanImages.{i}";
                                floorPlanImage = Request.Form.Files.FirstOrDefault(f => f.Name == floorPlanImageKey);
                                
                                // Try with bracket notation (fallback)
                                if (floorPlanImage == null)
                                {
                                    var floorPlanImageKey2 = $"floorPlanImages[{i}]";
                                    floorPlanImage = Request.Form.Files.FirstOrDefault(f => f.Name == floorPlanImageKey2);
                                }
                                
                                // Try searching by name pattern (last resort)
                                if (floorPlanImage == null)
                                {
                                    floorPlanImage = Request.Form.Files.FirstOrDefault(f => 
                                        f.Name != null && 
                                        f.Name.Contains("floorPlanImages") && 
                                        f.Name.Contains(i.ToString()));
                                }
                                
                                if (floorPlanImage != null && floorPlanImage.Length > 0)
                                {
                                    var fileName = Path.GetFileNameWithoutExtension(floorPlanImage.FileName);
                                    var extension = Path.GetExtension(floorPlanImage.FileName);
                                    var sanitizedFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                                    var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}{extension}";
                                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await floorPlanImage.CopyToAsync(fileStream);
                                    }

                                    imagePath = $"assets/images/property/floorplans/{uniqueFileName}";
                                }

                                decimal? area = null;
                                if (i < floorPlanAreas.Count && !string.IsNullOrWhiteSpace(floorPlanAreas[i]) && decimal.TryParse(floorPlanAreas[i], out decimal areaValue))
                                {
                                    area = areaValue;
                                }

                                int? bedrooms = null;
                                if (i < floorPlanBedrooms.Count && !string.IsNullOrWhiteSpace(floorPlanBedrooms[i]) && int.TryParse(floorPlanBedrooms[i], out int bedroomsValue))
                                {
                                    bedrooms = bedroomsValue;
                                }

                                int? bathrooms = null;
                                if (i < floorPlanBathrooms.Count && !string.IsNullOrWhiteSpace(floorPlanBathrooms[i]) && int.TryParse(floorPlanBathrooms[i], out int bathroomsValue))
                                {
                                    bathrooms = bathroomsValue;
                                }

                                var floorPlan = new PropertyFloorPlan
                                {
                                    PropertyId = property.PropertyId,
                                    FloorName = floorName,
                                    ImagePath = !string.IsNullOrEmpty(imagePath) ? imagePath : "assets/images/home/house-18.jpg", // Default image if none provided
                                    Area = area,
                                    Bedrooms = bedrooms,
                                    Bathrooms = bathrooms,
                                    Description = i < floorPlanDescriptions.Count ? floorPlanDescriptions[i] : "",
                                    SortOrder = floorPlanSortOrder++,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.PropertyFloorPlans.Add(floorPlan);
                            }
                        }
                    }

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
                    
                    // Always redirect on success (form submit, not AJAX)
                    return RedirectToAction(nameof(MyProperties));
                }
                catch (Exception ex)
                {
                    // Log the full exception for debugging
                    var errorMessage = $"An error occurred while saving: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" Inner: {ex.InnerException.Message}";
                    }
                    ModelState.AddModelError("", errorMessage);
                }
            }

            // Reload view with errors
            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", property.PropertyTypeId);
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", property.StatusId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", property.CityId);
            ViewBag.Districts = new SelectList(_context.Districts.Where(d => d.CityId == property.CityId), "DistrictId", "Name", property.DistrictId);
            ViewBag.Neighborhoods = new SelectList(_context.Neighborhoods.Where(n => n.DistrictId == property.DistrictId), "NeighborhoodId", "Name", property.NeighborhoodId);
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
                .Include(p => p.PropertyPhotos.OrderBy(pp => pp.SortOrder ?? 0))
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
        public async Task<IActionResult> Edit(int id, Property property, int[] selectedAmenities, int[] selectedFeatures, bool? IsFeatured, int? primaryPhotoId, string photoOrder)
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

            // Remove validation errors for navigation properties since we only bind IDs
            ModelState.Remove("PropertyType");
            ModelState.Remove("Status");

            // Validate PropertyTypeId and StatusId are provided
            if (property.PropertyTypeId <= 0)
            {
                ModelState.AddModelError("PropertyTypeId", "The PropertyType field is required.");
            }
            if (property.StatusId <= 0)
            {
                ModelState.AddModelError("StatusId", "The Status field is required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update existing property instead of using _context.Update() to avoid overwriting fields
                    existingProperty.Title = property.Title;
                    existingProperty.Description = property.Description;
                    existingProperty.Price = property.Price;
                    existingProperty.Currency = property.Currency;
                    existingProperty.Area = property.Area;
                    existingProperty.LotSize = property.LotSize;
                    existingProperty.Bedrooms = property.Bedrooms;
                    existingProperty.Bathrooms = property.Bathrooms;
                    existingProperty.Views = property.Views;
                    existingProperty.PropertyTypeId = property.PropertyTypeId;
                    existingProperty.StatusId = property.StatusId;
                    existingProperty.CityId = property.CityId;
                    existingProperty.DistrictId = property.DistrictId;
                    existingProperty.NeighborhoodId = property.NeighborhoodId;
                    existingProperty.Address = property.Address;
                    existingProperty.Latitude = property.Latitude;
                    existingProperty.Longitude = property.Longitude;
                    existingProperty.IsFeatured = IsFeatured ?? false;
                    existingProperty.ModifiedAt = DateTime.UtcNow;

                    // Update amenities
                    var existingAmenities = _context.PropertyAmenities.Where(pa => pa.PropertyId == id).ToList();
                    _context.PropertyAmenities.RemoveRange(existingAmenities);
                    if (selectedAmenities != null)
                    {
                        foreach (var amenityId in selectedAmenities)
                        {
                            _context.PropertyAmenities.Add(new PropertyAmenity
                            {
                                PropertyId = id,
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
                                PropertyId = id,
                                FeatureId = featureId
                            });
                        }
                    }

                    // Update photos: Set primary photo and update sort order
                    if (primaryPhotoId.HasValue)
                    {
                        // Set all photos to non-primary first
                        var allPhotos = await _context.PropertyPhotos
                            .Where(p => p.PropertyId == id)
                            .ToListAsync();
                        
                        foreach (var photo in allPhotos)
                        {
                            photo.IsPrimary = (photo.PhotoId == primaryPhotoId.Value);
                        }
                    }

                    // Update photo sort order if provided
                    if (!string.IsNullOrEmpty(photoOrder))
                    {
                        var photoIds = photoOrder.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s.Trim(), out int pid) ? pid : (int?)null)
                            .Where(pid => pid.HasValue)
                            .Select(pid => pid.Value)
                            .ToArray();
                        
                        if (photoIds.Length > 0)
                        {
                            var photos = await _context.PropertyPhotos
                                .Where(p => p.PropertyId == id)
                                .ToListAsync();
                            
                            for (int i = 0; i < photoIds.Length; i++)
                            {
                                var photo = photos.FirstOrDefault(p => p.PhotoId == photoIds[i]);
                                if (photo != null)
                                {
                                    photo.SortOrder = i + 1;
                                }
                            }
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
                catch (Exception ex)
                {
                    // Log error and reload property with navigation properties
                    ModelState.AddModelError("", $"An error occurred while saving: {ex.Message}");
                    return await ReloadEditViewWithErrors(id, agent.AgentId, property, selectedAmenities, selectedFeatures, IsFeatured);
                }
                return RedirectToAction(nameof(MyProperties));
            }

            // Reload property with all navigation properties when ModelState is invalid
            return await ReloadEditViewWithErrors(id, agent.AgentId, property, selectedAmenities, selectedFeatures, IsFeatured);
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

        // GET: Admin/Property/MyProperties
        [HttpGet]
        public async Task<IActionResult> MyProperties(string searchString, int? statusId, int page = 1, int pageSize = 10)
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
                ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", statusId);
                ViewBag.SearchString = searchString;
                ViewBag.StatusId = statusId;
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
                                         (p.Description != null && p.Description.Contains(searchString)) ||
                                         (p.Address != null && p.Address.Contains(searchString)));
            }

            if (statusId.HasValue && statusId.Value > 0)
            {
                query = query.Where(p => p.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync();
            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Ensure PropertyPhotos are loaded (in case of lazy loading issues)
            // Load all PropertyPhotos for all properties in one query for better performance
            if (properties.Any())
            {
                var propertyIds = properties.Select(p => p.PropertyId).ToList();
                var allPhotos = await _context.PropertyPhotos
                    .Where(pp => propertyIds.Contains(pp.PropertyId))
                    .OrderByDescending(pp => pp.IsPrimary == true)
                    .ThenBy(pp => pp.SortOrder ?? 0)
                    .ThenBy(pp => pp.PhotoId)
                    .ToListAsync();
                
                // Group photos by PropertyId and assign to each property
                var photosByProperty = allPhotos.GroupBy(p => p.PropertyId).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var prop in properties)
                {
                    if (photosByProperty.ContainsKey(prop.PropertyId))
                    {
                        prop.PropertyPhotos = photosByProperty[prop.PropertyId];
                    }
                    else
                    {
                        prop.PropertyPhotos = new List<PropertyPhoto>();
                    }
                }
            }

            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", statusId);
            ViewBag.SearchString = searchString;
            ViewBag.StatusId = statusId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(properties);
        }

        // POST: Admin/Property/MarkAsSold
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return Json(new { success = false, message = "Agent not found" });
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == id && p.AgentId == agent.AgentId);

            if (property == null)
            {
                return Json(new { success = false, message = "Property not found" });
            }

            // Tìm status "Sold" hoặc "Đã bán"
            var soldStatus = await _context.PropertyStatuses
                .FirstOrDefaultAsync(s => s.Name.ToLower().Contains("sold") || 
                                          s.Name.ToLower().Contains("đã bán"));

            if (soldStatus == null)
            {
                // Nếu không tìm thấy, tìm status có tên chứa "bán" (không có "đã")
                soldStatus = await _context.PropertyStatuses
                    .FirstOrDefaultAsync(s => s.Name.ToLower().Contains("bán") && 
                                             !s.Name.ToLower().Contains("rao"));
            }

            if (soldStatus == null)
            {
                // Nếu vẫn không tìm thấy, trả về lỗi
                return Json(new { success = false, message = "Sold status not found in database. Please contact administrator." });
            }

            property.StatusId = soldStatus.StatusId;
            property.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Property marked as sold" });
        }

        // POST: Admin/Property/UploadPhoto
        [HttpPost]
        public async Task<IActionResult> UploadPhoto(int propertyId, IFormFile file)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return Json(new { success = false, message = "Agent not found" });
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.AgentId == agent.AgentId);

            if (property == null)
            {
                return Json(new { success = false, message = "Property not found" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "images", "property");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var sanitizedFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Get current max sort order
                var maxSortOrder = await _context.PropertyPhotos
                    .Where(p => p.PropertyId == propertyId)
                    .MaxAsync(p => (int?)p.SortOrder) ?? 0;

                var photo = new PropertyPhoto
                {
                    PropertyId = propertyId,
                    FilePath = $"assets/images/property/{uniqueFileName}",
                    AltText = property.Title,
                    IsPrimary = false,
                    SortOrder = maxSortOrder + 1,
                    UploadedAt = DateTime.UtcNow
                };

                _context.PropertyPhotos.Add(photo);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    photoId = photo.PhotoId,
                    filePath = photo.FilePath,
                    message = "Photo uploaded successfully" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error uploading photo: {ex.Message}" });
            }
        }

        // POST: Admin/Property/DeletePhoto
        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return Json(new { success = false, message = "Agent not found" });
            }

            var photo = await _context.PropertyPhotos
                .Include(p => p.Property)
                .FirstOrDefaultAsync(p => p.PhotoId == photoId && p.Property.AgentId == agent.AgentId);

            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found" });
            }

            try
            {
                // Delete physical file
                if (!string.IsNullOrEmpty(photo.FilePath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, photo.FilePath.TrimStart('~', '/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.PropertyPhotos.Remove(photo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Photo deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting photo: {ex.Message}" });
            }
        }

        // POST: Admin/Property/SetPrimaryPhoto
        [HttpPost]
        public async Task<IActionResult> SetPrimaryPhoto(int photoId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var agent = _context.Agents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null)
            {
                return Json(new { success = false, message = "Agent not found" });
            }

            var photo = await _context.PropertyPhotos
                .Include(p => p.Property)
                .FirstOrDefaultAsync(p => p.PhotoId == photoId && p.Property.AgentId == agent.AgentId);

            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found" });
            }

            try
            {
                // Set all photos of this property to non-primary
                var allPhotos = await _context.PropertyPhotos
                    .Where(p => p.PropertyId == photo.PropertyId)
                    .ToListAsync();

                foreach (var p in allPhotos)
                {
                    p.IsPrimary = false;
                }

                // Set this photo as primary
                photo.IsPrimary = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Primary photo set successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error setting primary photo: {ex.Message}" });
            }
        }

        private async Task<IActionResult> ReloadEditViewWithErrors(int id, int agentId, Property property, int[] selectedAmenities, int[] selectedFeatures, bool? IsFeatured)
        {
            var propertyWithNav = await _context.Properties
                .Where(p => p.PropertyId == id && p.AgentId == agentId)
                .Include(p => p.PropertyAmenities)
                .Include(p => p.PropertyFeatures)
                .Include(p => p.PropertyPhotos.OrderBy(pp => pp.SortOrder ?? 0))
                .FirstOrDefaultAsync();

            if (propertyWithNav == null)
            {
                return NotFound();
            }

            // Update property object with form values but keep navigation properties
            propertyWithNav.Title = property.Title ?? propertyWithNav.Title;
            propertyWithNav.Description = property.Description ?? propertyWithNav.Description;
            propertyWithNav.Price = property.Price;
            propertyWithNav.Currency = property.Currency ?? propertyWithNav.Currency;
            propertyWithNav.Area = property.Area;
            propertyWithNav.LotSize = property.LotSize;
            propertyWithNav.Bedrooms = property.Bedrooms;
            propertyWithNav.Bathrooms = property.Bathrooms;
            propertyWithNav.Views = property.Views;
            // Only update PropertyTypeId and StatusId if form has valid values (> 0)
            // Otherwise keep existing values from database to preserve selected options
            if (property.PropertyTypeId > 0)
                propertyWithNav.PropertyTypeId = property.PropertyTypeId;
            if (property.StatusId > 0)
                propertyWithNav.StatusId = property.StatusId;
            if (property.CityId.HasValue && property.CityId.Value > 0)
                propertyWithNav.CityId = property.CityId;
            else if (property.CityId.HasValue && property.CityId.Value == 0)
                propertyWithNav.CityId = null;
            if (property.DistrictId.HasValue && property.DistrictId.Value > 0)
                propertyWithNav.DistrictId = property.DistrictId;
            else if (property.DistrictId.HasValue && property.DistrictId.Value == 0)
                propertyWithNav.DistrictId = null;
            if (property.NeighborhoodId.HasValue && property.NeighborhoodId.Value > 0)
                propertyWithNav.NeighborhoodId = property.NeighborhoodId;
            else if (property.NeighborhoodId.HasValue && property.NeighborhoodId.Value == 0)
                propertyWithNav.NeighborhoodId = null;
            propertyWithNav.Address = property.Address ?? propertyWithNav.Address;
            propertyWithNav.Latitude = property.Latitude;
            propertyWithNav.Longitude = property.Longitude;
            propertyWithNav.IsFeatured = IsFeatured ?? false;

            ViewBag.PropertyTypes = new SelectList(_context.PropertyTypes, "PropertyTypeId", "Name", propertyWithNav.PropertyTypeId);
            ViewBag.Statuses = new SelectList(_context.PropertyStatuses, "StatusId", "Name", propertyWithNav.StatusId);
            ViewBag.Cities = new SelectList(_context.Cities, "CityId", "Name", propertyWithNav.CityId);
            ViewBag.Districts = new SelectList(_context.Districts.Where(d => d.CityId == propertyWithNav.CityId), "DistrictId", "Name", propertyWithNav.DistrictId);
            ViewBag.Neighborhoods = new SelectList(_context.Neighborhoods.Where(n => n.DistrictId == propertyWithNav.DistrictId), "NeighborhoodId", "Name", propertyWithNav.NeighborhoodId);
            ViewBag.Agents = new SelectList(_context.Agents.Include(a => a.User), "AgentId", "User.FullName", propertyWithNav.AgentId);
            ViewBag.Amenities = _context.Amenities.ToList();
            ViewBag.Features = _context.Features.ToList();
            ViewBag.SelectedAmenities = selectedAmenities ?? propertyWithNav.PropertyAmenities.Select(pa => pa.AmenityId).ToArray();
            ViewBag.SelectedFeatures = selectedFeatures ?? propertyWithNav.PropertyFeatures.Select(pf => pf.FeatureId).ToArray();
            return View(propertyWithNav);
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}
