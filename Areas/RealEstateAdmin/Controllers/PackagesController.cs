// Areas/Admin/Controllers/PackagesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class PackagesController : BaseController
    {
        private readonly HomeLengoContext _context;

        public PackagesController(HomeLengoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Chỉ Admin mới được truy cập
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Lấy ServicePlans từ database với features
            var packages = await _context.ServicePlans
                .Include(sp => sp.ServicePlanFeatures.OrderBy(f => f.DisplayOrder))
                .Select(sp => new
                {
                    PlanId = sp.PlanId,
                    Name = sp.Name,
                    Price = sp.Price,
                    MaxListings = sp.MaxListings ?? 0,
                    CreatedAt = sp.CreatedAt,
                    Features = sp.ServicePlanFeatures
                        .Where(f => f.IsIncluded)
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => f.FeatureText)
                        .ToList()
                })
                .ToListAsync();

            // Lấy lịch sử giao dịch từ UserServicePackages
            var transactions = await _context.UserServicePackages
                .Include(usp => usp.User)
                .Include(usp => usp.Plan)
                .OrderByDescending(usp => usp.CreatedAt)
                .Select(usp => new
                {
                    Id = usp.Id,
                    UserName = usp.User != null ? (usp.User.FullName ?? usp.User.Username) : "N/A",
                    PackageName = usp.Plan != null ? usp.Plan.Name : "N/A",
                    Price = usp.Plan != null ? usp.Plan.Price : 0,
                    StartDate = usp.StartDate.ToString("dd/MM/yyyy"),
                    EndDate = usp.EndDate.HasValue ? usp.EndDate.Value.ToString("dd/MM/yyyy") : "Không giới hạn",
                    IsActive = usp.IsActive,
                    CreatedAt = usp.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            ViewBag.Packages = packages;
            ViewBag.Transactions = transactions;

            return View();
        }

        // GET: Create ServicePlan
        [HttpGet]
        public IActionResult CreatePlan()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            return View();
        }

        // POST: Create ServicePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(ServicePlan plan)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (ModelState.IsValid)
            {
                plan.CreatedAt = DateTime.UtcNow;
                _context.ServicePlans.Add(plan);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo gói dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(plan);
        }

        // GET: Edit ServicePlan
        [HttpGet]
        public async Task<IActionResult> EditPlan(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var plan = await _context.ServicePlans
                .Include(sp => sp.ServicePlanFeatures.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(sp => sp.PlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: Edit ServicePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan([FromForm] ServicePlan plan)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPlan = await _context.ServicePlans.FindAsync(plan.PlanId);
                    if (existingPlan == null)
                    {
                        return NotFound();
                    }

                    existingPlan.Name = plan.Name;
                    existingPlan.Price = plan.Price;
                    existingPlan.MaxListings = plan.MaxListings;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật gói dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServicePlanExists(plan.PlanId))
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
            return RedirectToAction(nameof(Index));
        }

        // POST: Delete ServicePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var plan = await _context.ServicePlans
                .Include(sp => sp.ServicePlanFeatures)
                .FirstOrDefaultAsync(sp => sp.PlanId == id);

            if (plan != null)
            {
                // Xóa các features trước
                _context.ServicePlanFeatures.RemoveRange(plan.ServicePlanFeatures);
                _context.ServicePlans.Remove(plan);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa gói dịch vụ thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Get Plan Details for Edit Modal
        [HttpGet]
        public async Task<JsonResult> GetPlanDetails(int id)
        {
            var plan = await _context.ServicePlans
                .Include(sp => sp.ServicePlanFeatures.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(sp => sp.PlanId == id);

            if (plan == null)
            {
                return Json(new { error = "Không tìm thấy gói dịch vụ" });
            }

            var result = new
            {
                PlanId = plan.PlanId,
                Name = plan.Name,
                Price = plan.Price,
                MaxListings = plan.MaxListings,
                Features = plan.ServicePlanFeatures
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new
                    {
                        FeatureId = f.FeatureId,
                        FeatureText = f.FeatureText,
                        IsIncluded = f.IsIncluded,
                        DisplayOrder = f.DisplayOrder
                    })
                    .ToList()
            };

            return Json(result);
        }

        // POST: Create ServicePlanFeature
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeature(int planId, string featureText, bool isIncluded, int displayOrder)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var feature = new ServicePlanFeature
                {
                    PlanId = planId,
                    FeatureText = featureText,
                    IsIncluded = isIncluded,
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ServicePlanFeatures.Add(feature);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm tính năng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Edit ServicePlanFeature
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeature(int featureId, string featureText, bool isIncluded, int displayOrder)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            try
            {
                var feature = await _context.ServicePlanFeatures.FindAsync(featureId);
                if (feature != null)
                {
                    feature.FeatureText = featureText;
                    feature.IsIncluded = isIncluded;
                    feature.DisplayOrder = displayOrder;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Đã cập nhật tính năng thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy tính năng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Delete ServicePlanFeature
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeature(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var feature = await _context.ServicePlanFeatures.FindAsync(id);
            if (feature != null)
            {
                _context.ServicePlanFeatures.Remove(feature);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tính năng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServicePlanExists(int id)
        {
            return _context.ServicePlans.Any(e => e.PlanId == id);
        }
    }
}