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
        public IActionResult Index(string searchString, string status)
        {
            var query = _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.User)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i => 
                    (i.ContactName != null && i.ContactName.Contains(searchString)) ||
                    (i.ContactPhone != null && i.ContactPhone.Contains(searchString)) ||
                    (i.ContactEmail != null && i.ContactEmail.Contains(searchString)) ||
                    (i.Property != null && i.Property.Title != null && i.Property.Title.Contains(searchString)) ||
                    (i.User != null && ((i.User.FullName != null && i.User.FullName.Contains(searchString)) ||
                                       (i.User.Username != null && i.User.Username.Contains(searchString)))));
            }

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var contacts = query
                .ToList()
                .Select(i => new
                {
                    Id = i.InquiryId,
                    Name = i.ContactName ?? (i.User != null ? i.User.FullName ?? i.User.Username : "N/A"),
                    Phone = i.ContactPhone ?? (i.User != null ? i.User.Phone ?? "" : ""),
                    Email = i.ContactEmail ?? (i.User != null ? i.User.Email : ""),
                    Property = i.Property != null ? i.Property.Title : "N/A",
                    PropertyId = i.PropertyId,
                    Message = i.Message ?? "",
                    Status = i.Status ?? "new",
                    CreatedDate = i.CreatedAt.HasValue 
                        ? i.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                        : "",
                    Agent = "Chưa phân" // Có thể thêm AgentId vào Inquiry nếu cần
                })
                .OrderByDescending(i => i.CreatedDate)
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            return View(contacts);
        }

        [HttpGet]
        public async Task<JsonResult> GetInquiryDetail(int id)
        {
            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InquiryId == id);

            if (inquiry == null)
            {
                return Json(new { error = "Không tìm thấy yêu cầu" });
            }

            var result = new
            {
                Id = inquiry.InquiryId,
                Name = inquiry.ContactName ?? (inquiry.User != null ? inquiry.User.FullName ?? inquiry.User.Username : "N/A"),
                Phone = inquiry.ContactPhone ?? (inquiry.User != null ? inquiry.User.Phone ?? "" : ""),
                Email = inquiry.ContactEmail ?? (inquiry.User != null ? inquiry.User.Email : ""),
                Property = inquiry.Property != null ? inquiry.Property.Title : "N/A",
                PropertyId = inquiry.PropertyId,
                Message = inquiry.Message ?? "",
                Status = inquiry.Status ?? "new",
                CreatedDate = inquiry.CreatedAt.HasValue 
                    ? inquiry.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                    : ""
            };

            return Json(result);
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

        // Form liên hệ chung - sử dụng bảng ContactUs
        public IActionResult FormContacts(string searchString, string status)
        {
            var query = _context.ContactUs.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => 
                    (c.FullName != null && c.FullName.Contains(searchString)) ||
                    (c.Email != null && c.Email.Contains(searchString)) ||
                    (c.Phone != null && c.Phone.Contains(searchString)) ||
                    (c.Information != null && c.Information.Contains(searchString)) ||
                    (c.Message != null && c.Message.Contains(searchString)));
            }

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var formContacts = query
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            return View(formContacts);
        }

        [HttpGet]
        public async Task<JsonResult> GetContactDetail(int id)
        {
            var contact = await _context.ContactUs
                .FirstOrDefaultAsync(c => c.ContactId == id);

            if (contact == null)
            {
                return Json(new { error = "Không tìm thấy liên hệ" });
            }

            var result = new
            {
                ContactId = contact.ContactId,
                FullName = contact.FullName ?? "N/A",
                Email = contact.Email ?? "",
                Phone = contact.Phone ?? "",
                Information = contact.Information ?? "",
                Message = contact.Message ?? "",
                Status = contact.Status ?? "Chưa xử lý",
                CreatedAt = contact.CreatedAt.HasValue 
                    ? contact.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") 
                    : ""
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContactStatus(int id, string status)
        {
            var contact = await _context.ContactUs.FindAsync(id);
            if (contact != null)
            {
                contact.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(FormContacts));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var contact = await _context.ContactUs.FindAsync(id);
            if (contact != null)
            {
                _context.ContactUs.Remove(contact);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(FormContacts));
        }

        // Phản hồi khách hàng (sử dụng Reviews)
        public IActionResult Feedbacks()
        {
            var feedbacks = _context.Reviews
                .Include(r => r.Property)
                .Include(r => r.User)
                .ToList()
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