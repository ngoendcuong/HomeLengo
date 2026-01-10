using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.Win32;

namespace HomeLengo.Controllers
{
    public class ServiceRegisterController : Controller
    {
        private readonly HomeLengoContext _context;

        public ServiceRegisterController(HomeLengoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create(int? planId)
        {
            ViewBag.Plans = new SelectList(
                _context.ServicePlans.ToList(),
                "PlanId",
                "Name",
                planId  // set selected value luôn cho dropdown
            );

            var model = new ServiceRegister();

            if (planId.HasValue)
            {
                model.PlanId = planId.Value;
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ServiceRegister model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Plans = new SelectList(
                    _context.ServicePlans.ToList(),
                    "PlanId",
                    "Name",
                    model.PlanId
                );
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            _context.ServiceRegisters.Add(model);
            _context.SaveChanges();

            var plan = _context.ServicePlans
                .FirstOrDefault(p => p.PlanId == model.PlanId);

            if (plan?.IsBroker == true)
            {
                // Chuyển sang tạo hồ sơ môi giới
                return RedirectToAction(
                "Create",
                "AgentProfile",
                 new { ServiceRegisterId = model.Id }
                 );

            }

            return RedirectToAction("Checkout", "Payment",
    new { serviceRegisterId = model.Id });

        }
    }
}
