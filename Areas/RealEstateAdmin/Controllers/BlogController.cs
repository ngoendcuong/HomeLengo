using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeLengo.Models;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class BlogController : BaseController
    {
        private readonly HomeLengoContext _context;

        public BlogController(HomeLengoContext context)
        {
            _context = context;
        }

        // Danh mục bài viết
        public IActionResult Categories()
        {
            var categories = _context.BlogCategories
                .Include(c => c.Blogs)
                .Select(c => new
                {
                    Id = c.CategoryId,
                    Name = c.Name,
                    Slug = c.Slug ?? c.Name.ToLower().Replace(" ", "-"),
                    PostCount = c.Blogs != null ? c.Blogs.Count : 0,
                    Order = c.CategoryId,
                    IsActive = true
                })
                .OrderBy(c => c.Order)
                .ToList();

            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(BlogCategory category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.UtcNow;
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Categories));
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, BlogCategory category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogCategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Categories));
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.BlogCategories.FindAsync(id);
            if (category != null)
            {
                _context.BlogCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        // Danh sách bài viết
        public IActionResult Index()
        {
            var posts = _context.Blogs
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Select(b => new
                {
                    Id = b.BlogId,
                    Title = b.Title,
                    Category = b.Category != null ? b.Category.Name : "Chưa phân loại",
                    Author = b.Author != null ? (b.Author.FullName ?? b.Author.Username) : "Admin",
                    Views = b.ViewCount ?? 0,
                    Status = b.IsPublished == true ? "Công khai" : "Nháp",
                    CreatedDate = b.CreatedAt.HasValue 
                        ? b.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : "",
                    Image = b.Thumbnail ?? "https://via.placeholder.com/100x80"
                })
                .OrderByDescending(b => b.CreatedDate)
                .ToList();

            return View(posts);
        }

        // Tạo bài viết mới
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name");
            ViewBag.Authors = new SelectList(_context.Users.Select(u => new { 
                UserId = u.UserId, 
                Name = u.FullName ?? u.Username 
            }), "UserId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Blog blog)
        {
            if (ModelState.IsValid)
            {
                blog.CreatedAt = DateTime.UtcNow;
                blog.ModifiedAt = DateTime.UtcNow;
                if (blog.IsPublished == true)
                {
                    blog.PublishedAt = DateTime.UtcNow;
                }
                _context.Add(blog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
            ViewBag.Authors = new SelectList(_context.Users.Select(u => new { 
                UserId = u.UserId, 
                Name = u.FullName ?? u.Username 
            }), "UserId", "Name", blog.AuthorId);
            return View(blog);
        }

        // Sửa bài viết
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            ViewBag.Categories = _context.BlogCategories.ToList();
            ViewBag.Authors = _context.Users.ToList();
            ViewBag.PostId = id;
            
            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Blog blog)
        {
            if (id != blog.BlogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
            ViewBag.Authors = new SelectList(_context.Users.Select(u => new { 
                UserId = u.UserId, 
                Name = u.FullName ?? u.Username 
            }), "UserId", "Name", blog.AuthorId);
            return View(blog);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog != null)
            {
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BlogCategoryExists(int id)
        {
            return _context.BlogCategories.Any(e => e.CategoryId == id);
        }

        private bool BlogExists(int id)
        {
            return _context.Blogs.Any(e => e.BlogId == id);
        }
    }
}