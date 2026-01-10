using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Controllers
{
    public class AgentProfileController : Controller
    {
        private readonly HomeLengoContext _context;

        public AgentProfileController(HomeLengoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create(int ServiceRegisterId)
        {
            var register = _context.ServiceRegisters
                .FirstOrDefault(x => x.Id == ServiceRegisterId);

            if (register == null)
                return NotFound();

            return View(new AgentProfile
            {
                ServiceRegisterId = ServiceRegisterId,
                DisplayName = register.FullName
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AgentProfile model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.AvatarFile != null)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/avatars"
                );
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(model.AvatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.AvatarFile.CopyTo(stream);
                }

                model.Avatar = "/uploads/avatars/" + fileName;
            }
            else
            {
                model.Avatar = "/uploads/avatars/default.png";
            }

            model.CreatedAt = DateTime.Now;
            _context.AgentProfiles.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Profile", new { id = model.Id });
        }

        public IActionResult Profile(int id)
        {
            var profile = _context.AgentProfiles
                .Include(x => x.ServiceRegister)
                .ThenInclude(x => x.Plan)
                .FirstOrDefault(x => x.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }
    }
}
