using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Controllers
{
    public class PagesController : Controller
    {
        private readonly HomeLengoContext _context;

        public PagesController(HomeLengoContext context)
        {
            _context = context;
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult OurServices()
        {
            return View();
        }

        public async Task<IActionResult> Pricing()
        {
            var plans = await _context.ServicePlans
                .Include(p => p.ServicePlanFeatures)
                .OrderBy(p => p.PlanId)
                .ToListAsync();

            return View(plans); 
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        public async Task<IActionResult> FAQs()
        {
            var faqs = await _context.Faqs
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();

            // Nhóm theo Category
            var groupedFaqs = faqs.GroupBy(f => f.Category);

            return View(groupedFaqs);
        }


        public IActionResult PrivacyPolicy()
        {
            return View();
        }
    }
}
