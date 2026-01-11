using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class ContactsController : BaseController
    {
        private readonly HomeLengoContext _context;

        public ContactsController(HomeLengoContext context)
        {
            _context = context;
        }

        // Yêu cầu tư vấn
        public IActionResult Index()
        {
            var contacts = _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.User)
                .Select(i => new
                {
                    Id = i.InquiryId,
                    Name = i.ContactName ?? (i.User != null ? i.User.FullName ?? i.User.Username : "N/A"),
                    Phone = i.ContactPhone ?? (i.User != null ? i.User.Phone ?? "" : ""),
                    Email = i.ContactEmail ?? (i.User != null ? i.User.Email : ""),
                    Property = i.Property != null ? i.Property.Title : "N/A",
                    Message = i.Message ?? "",
                    Status = i.Status ?? "new",
                    CreatedDate = i.CreatedAt.HasValue 
                        ? i.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                        : "",
                    Agent = "Chưa phân" // Có thể thêm AgentId vào Inquiry nếu cần
                })
                .OrderByDescending(i => i.CreatedDate)
                .ToList();

            return View(contacts);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInquiryStatus(int id, string status)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry != null)
            {
                _context.Inquiries.Remove(inquiry);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Form liên hệ chung (có thể sử dụng Inquiry hoặc tạo model Contact riêng)
        public IActionResult FormContacts()
        {
            // Nếu có model Contact riêng, sử dụng nó
            // Hiện tại sử dụng Inquiry với PropertyId = null hoặc một giá trị đặc biệt
            var formContacts = _context.Inquiries
                .Where(i => i.PropertyId == 0 || i.Property == null) // Giả sử PropertyId = 0 là form liên hệ chung
                .Select(i => new
                {
                    Id = i.InquiryId,
                    Name = i.ContactName ?? "N/A",
                    Phone = i.ContactPhone ?? "",
                    Email = i.ContactEmail ?? "",
                    Subject = "Liên hệ", // Có thể thêm Subject vào Inquiry model
                    Message = i.Message ?? "",
                    Status = i.Status ?? "new",
                    CreatedDate = i.CreatedAt.HasValue 
                        ? i.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                        : ""
                })
                .OrderByDescending(i => i.CreatedDate)
                .ToList();

            return View(formContacts);
        }

        // Phản hồi khách hàng (sử dụng Reviews)
        public IActionResult Feedbacks()
        {
            var feedbacks = _context.Reviews
                .Include(r => r.Property)
                .Include(r => r.User)
                .Select(r => new
                {
                    Id = r.ReviewId,
                    Customer = r.User != null ? (r.User.FullName ?? r.User.Username) : "N/A",
                    Property = r.Property != null ? r.Property.Title : "N/A",
                    Rating = (int)r.Rating,
                    Comment = r.Body ?? "",
                    Status = r.IsApproved == true ? "Đã duyệt" : "Chờ duyệt",
                    CreatedDate = r.CreatedAt.HasValue 
                        ? r.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : ""
                })
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            return View(feedbacks);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Feedbacks));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Feedbacks));
        }
    }
}