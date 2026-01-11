using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Controllers
{
    public class ContactController : Controller
    {
        private readonly HomeLengoContext _context;

        public ContactController(HomeLengoContext context)
        {
            _context = context;
        }

        // 👉 GET: /Contact
        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactU());
        }

        // 👉 POST: /Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] ContactU model)
        {
            // Kiểm tra model có null không
            if (model == null)
            {
                TempData["Error"] = "Dữ liệu form không hợp lệ. Vui lòng thử lại!";
                return View(new ContactU());
            }

            // Lấy dữ liệu từ form nếu model binding không hoạt động
            var fullName = Request.Form["FullName"].ToString();
            var email = Request.Form["Email"].ToString();
            var phone = Request.Form["Phone"].ToString();
            var information = Request.Form["Information"].ToString();
            var message = Request.Form["Message"].ToString();

            // Sử dụng dữ liệu từ form nếu model null hoặc rỗng
            if (string.IsNullOrWhiteSpace(model?.FullName) && !string.IsNullOrWhiteSpace(fullName))
            {
                model = model ?? new ContactU();
                model.FullName = fullName;
                model.Email = email;
                model.Phone = phone;
                model.Information = information;
                model.Message = message;
            }

            // Kiểm tra validation
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Tên đầy đủ không được để trống");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống");
            }
            else if (!model.Email.Contains("@"))
            {
                ModelState.AddModelError("Email", "Email không hợp lệ");
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                ModelState.AddModelError("Message", "Lời nhắn không được để trống");
            }

            // Log để debug
            Console.WriteLine($"=== Contact Form Submit ===");
            Console.WriteLine($"FullName: {model.FullName}");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"Phone: {model.Phone}");
            Console.WriteLine($"Message: {model.Message?.Substring(0, Math.Min(50, model.Message?.Length ?? 0))}...");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Error in {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return View(model);
            }

            try
            {
                // Tạo đối tượng mới để lưu
                var contact = new ContactU
                {
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    Phone = !string.IsNullOrWhiteSpace(model.Phone) ? model.Phone.Trim() : null,
                    Information = !string.IsNullOrWhiteSpace(model.Information) ? model.Information.Trim() : null,
                    Message = model.Message.Trim(),
                    Status = "New",
                    CreatedAt = DateTime.Now
                };

                _context.ContactUs.Add(contact);
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"Đã lưu liên hệ thành công: ID={contact.ContactId}, Email={contact.Email}, Name={contact.FullName}, Rows affected: {result}");

                TempData["Success"] = "Gửi liên hệ thành công! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database Error: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                
                TempData["Error"] = "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng kiểm tra lại thông tin và thử lại!";
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lưu liên hệ: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}. Vui lòng thử lại!";
                return View(model ?? new ContactU());
            }
        }
    }
}
