using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogController : Controller
    {
        private readonly HomeLengoContext _context;

        public BlogController(HomeLengoContext context)
        {
            _context = context;
        }

        // GET: Admin/Blog
        public async Task<IActionResult> Index(string searchString, int? categoryId, bool? isPublished, int page = 1, int pageSize = 10)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var query = _context.Blogs
                .Where(b => b.AuthorId == userId)
                .Include(b => b.Author)
                .Include(b => b.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Title.Contains(searchString) ||
                                         b.Content.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            if (isPublished.HasValue)
            {
                query = query.Where(b => b.IsPublished == isPublished.Value);
            }

            var totalCount = await query.CountAsync();
            var blogs = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", categoryId);
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.IsPublished = isPublished;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(blogs);
        }

        // GET: Admin/Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.BlogComments)
                    .ThenInclude(bc => bc.User)
                .FirstOrDefaultAsync(m => m.BlogId == id);

            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // GET: Admin/Blog/Create
        public IActionResult Create()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name");
            ViewBag.AuthorId = userId; // Tự động gán author
            return View();
        }

        // POST: Admin/Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Blog blog)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (ModelState.IsValid)
            {
                blog.AuthorId = userId; // Tự động gán author
                blog.CreatedAt = DateTime.UtcNow;
                if (blog.IsPublished == true)
                {
                    blog.PublishedAt = DateTime.UtcNow;
                }
                _context.Add(blog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
            ViewBag.AuthorId = userId;
            return View(blog);
        }

        // GET: Admin/Blog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Where(b => b.AuthorId == userId)
                .FirstOrDefaultAsync(b => b.BlogId == id);
            if (blog == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
            ViewBag.AuthorId = userId;
            return View(blog);
        }

        // POST: Admin/Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Blog blog)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (id != blog.BlogId)
            {
                return NotFound();
            }

            // Kiểm tra blog thuộc về user này
            var existingBlog = await _context.Blogs
                .FirstOrDefaultAsync(b => b.BlogId == id && b.AuthorId == userId);
            if (existingBlog == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    blog.AuthorId = userId; // Đảm bảo author không bị thay đổi
                    blog.ModifiedAt = DateTime.UtcNow;
                    if (blog.IsPublished == true && !blog.PublishedAt.HasValue)
                    {
                        blog.PublishedAt = DateTime.UtcNow;
                    }
                    _context.Update(blog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(blog.BlogId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
            ViewBag.AuthorId = userId;
            return View(blog);
        }

        // GET: Admin/Blog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Where(b => b.AuthorId == userId)
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.BlogId == id);

            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // POST: Admin/Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var blog = await _context.Blogs
                .Where(b => b.AuthorId == userId)
                .Include(b => b.BlogComments)
                .FirstOrDefaultAsync(b => b.BlogId == id);

            if (blog != null)
            {
                _context.BlogComments.RemoveRange(blog.BlogComments);
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Blog/TogglePublish/5
        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var blog = await _context.Blogs
                .Where(b => b.AuthorId == userId)
                .FirstOrDefaultAsync(b => b.BlogId == id);
            if (blog != null)
            {
                blog.IsPublished = !blog.IsPublished;
                if (blog.IsPublished == true && !blog.PublishedAt.HasValue)
                {
                    blog.PublishedAt = DateTime.UtcNow;
                }
                blog.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BlogExists(int id)
        {
            return _context.Blogs.Any(e => e.BlogId == id);
        }
    }
}















