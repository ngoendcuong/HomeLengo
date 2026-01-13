using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class FAQController : BaseController
    {
        private readonly HomeLengoContext _context;

        public FAQController(HomeLengoContext context)
        {
            _context = context;
        }

        // Helper: luôn quay về đúng area RealEstateAdmin
        private IActionResult RedirectToFaqIndex()
            => RedirectToAction("Index", "FAQ", new { area = "RealEstateAdmin" });

        // GET: RealEstateAdmin/FAQ
        public async Task<IActionResult> Index()
        {
            var faqs = await _context.Faqs
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.FaqId)
                .ToListAsync();

            return View(faqs);
        }

        // GET: RealEstateAdmin/FAQ/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RealEstateAdmin/FAQ/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faq faq)
        {
            if (ModelState.IsValid)
            {
                faq.CreatedAt = DateTime.UtcNow;
                faq.UpdatedAt = DateTime.UtcNow;

                _context.Add(faq);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo câu hỏi thường gặp thành công!";
                return RedirectToFaqIndex();
            }

            // Nếu bạn dùng trang Create riêng thì giữ return View(faq) là hợp lý
            return View(faq);
        }

        // GET: RealEstateAdmin/FAQ/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        // POST: RealEstateAdmin/FAQ/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Faq faq)
        {
            if (id != faq.FaqId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Đảm bảo không đụng CreatedAt (nếu form không post field này)
                    var existing = await _context.Faqs.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.FaqId == id);

                    if (existing == null) return NotFound();

                    faq.CreatedAt = existing.CreatedAt; // giữ nguyên
                    faq.UpdatedAt = DateTime.UtcNow;

                    _context.Update(faq);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật câu hỏi thường gặp thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FAQExists(faq.FaqId)) return NotFound();
                    throw;
                }

                return RedirectToFaqIndex();
            }

            return View(faq);
        }

        // GET: RealEstateAdmin/FAQ/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var faq = await _context.Faqs.FirstOrDefaultAsync(m => m.FaqId == id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        // POST: RealEstateAdmin/FAQ/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq != null)
            {
                _context.Faqs.Remove(faq);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa câu hỏi thường gặp thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy câu hỏi để xóa!";
            }

            return RedirectToFaqIndex();
        }

        // POST: RealEstateAdmin/FAQ/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null)
                return Json(new { success = false, message = "Không tìm thấy câu hỏi" });

            faq.IsActive = !faq.IsActive;
            faq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = faq.IsActive });
        }

        private bool FAQExists(int id)
        {
            return _context.Faqs.Any(e => e.FaqId == id);
        }
    }
}
