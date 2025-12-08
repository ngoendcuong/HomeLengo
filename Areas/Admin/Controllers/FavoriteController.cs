using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FavoriteController : Controller
    {
        private readonly HomeLengoContext _context;

        public FavoriteController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Favorite
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Property)
                    .ThenInclude(p => p.PropertyPhotos)
                .Include(f => f.Property)
                    .ThenInclude(p => p.Status)
                .Include(f => f.Property)
                    .ThenInclude(p => p.PropertyType)
                .Include(f => f.Property)
                    .ThenInclude(p => p.City)
                .Include(f => f.Property)
                    .ThenInclude(p => p.Agent)
                        .ThenInclude(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(f => f.Property.Title.Contains(searchString) ||
                                         f.Property.Description.Contains(searchString) ||
                                         f.Property.Address.Contains(searchString));
            }

            var totalCount = await query.CountAsync();
            var favorites = await query
                .OrderByDescending(f => f.AddedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(favorites);
        }

        // POST: Admin/Favorite/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.FavoriteId == id && f.UserId == userId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

