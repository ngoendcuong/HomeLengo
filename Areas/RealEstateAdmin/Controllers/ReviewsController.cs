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

        public async Task<IActionResult> Index(string searchString, string status, int? rating)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Property)
                    .ThenInclude(p => p.Agent)
                        .ThenInclude(a => a.User)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r =>
                    (r.User != null && ((r.User.FullName != null && r.User.FullName.Contains(searchString)) ||
                                       (r.User.Username != null && r.User.Username.Contains(searchString)))) ||
                    (r.Property != null && r.Property.Title != null && r.Property.Title.Contains(searchString)) ||
                    (r.Body != null && r.Body.Contains(searchString)));
            }

            // Filter theo trạng thái (approved = hiển thị, pending = đã ẩn)
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "approved")
                {
                    query = query.Where(r => r.IsApproved == true);
                }
                else if (status == "pending")
                {
                    query = query.Where(r => r.IsApproved != true);
                }
            }

            // Filter theo rating
            if (rating.HasValue && rating.Value > 0)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            // ✅ sắp xếp theo DateTime trước (đừng sort string)
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(r => r.ReviewId)
                .ToListAsync();

            var reviewsList = reviews
                .Select(r => new
                {
                    Id = r.ReviewId,
                    User = r.User != null ? (r.User.FullName ?? r.User.Username) : "N/A",
                    Property = r.Property != null ? r.Property.Title : "N/A",
                    PropertyId = r.PropertyId,
                    Agent = r.Property != null && r.Property.Agent != null && r.Property.Agent.User != null
                        ? (r.Property.Agent.User.FullName ?? r.Property.Agent.User.Username)
                        : "N/A",
                    Rating = (int)r.Rating,
                    Comment = r.Body ?? "",
                    Status = r.IsApproved == true ? "Đã duyệt" : "Đã ẩn",
                    CreatedDate = r.CreatedAt.HasValue
                        ? r.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm")
                        : ""
                })
                .ToList();

            // Thống kê
            var totalReviews = await _context.Reviews.CountAsync();
            var approvedReviews = await _context.Reviews.CountAsync(r => r.IsApproved == true);
            var hiddenReviews = await _context.Reviews.CountAsync(r => r.IsApproved != true);
            var avgRating = await _context.Reviews
                .Where(r => r.IsApproved == true)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            ViewBag.TotalReviews = totalReviews;
            ViewBag.ApprovedReviews = approvedReviews;
            ViewBag.PendingReviews = hiddenReviews; // dùng lại biến cũ cho view
            ViewBag.AvgRating = Math.Round(avgRating, 1);
            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.Rating = rating;

            return View(reviewsList);
        }

        // HIỆN: IsApproved = true
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Show(int id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá!";
                return Redirect("/RealEstateAdmin/Reviews");
            }

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hiển thị đánh giá!";
            return Redirect("/RealEstateAdmin/Reviews");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá!";
                return Redirect("/RealEstateAdmin/Reviews");
            }

            review.IsApproved = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã ẩn đánh giá!";
            return Redirect("/RealEstateAdmin/Reviews");
        }

    }
}
