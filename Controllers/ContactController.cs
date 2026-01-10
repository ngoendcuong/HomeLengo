using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Index(ContactU model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            model.Status = "New";
            try
            {
                _context.ContactUs.Add(model);
                _context.SaveChanges();

                TempData["Success"] = "Gửi liên hệ thành công!";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return View(model);
            }
        }
    }
}
