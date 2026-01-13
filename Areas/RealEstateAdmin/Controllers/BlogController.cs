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
        private readonly IWebHostEnvironment _env;

        public BlogController(HomeLengoContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===== ViewModels (nên tách ra file riêng sau) =====
        public class BlogCategoryRowVM
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Slug { get; set; } = "";
            public int PostCount { get; set; }
            public int Order { get; set; }
            public bool IsActive { get; set; }
        }

        public class BlogRowVM
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Author { get; set; } = "";
            public int Views { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string CreatedDateText { get; set; } = "";
            public string Image { get; set; } = "";
        }

        // ===== Helpers =====
        private IActionResult RedirectToAdminHome()
            => RedirectToAction("Index", "Home", new { area = "" });

        private IActionResult RedirectToBlogIndex()
            => RedirectToAction("Index", "Blog", new { area = "RealEstateAdmin" });

        private IActionResult RedirectToBlogCategories()
            => RedirectToAction("Categories", "Blog", new { area = "RealEstateAdmin" });

        private bool EnsureAdmin() => IsAdmin();

        // =========================
        // Danh mục bài viết
        // GET: /RealEstateAdmin/Blog/Categories
        // =========================
        public IActionResult Categories()
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var categories = _context.BlogCategories
                .AsNoTracking()
                .Include(c => c.Blogs)
                .Select(c => new BlogCategoryRowVM
                {
                    Id = c.CategoryId,
                    Name = c.Name ?? "",
                    Slug = c.Slug ?? (c.Name ?? "").ToLower().Replace(" ", "-"),
                    PostCount = c.Blogs != null ? c.Blogs.Count : 0,
                    Order = c.CategoryId,
                    IsActive = true
                })
                .OrderBy(c => c.Order)
                .ToList();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(BlogCategory category)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.UtcNow;
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo danh mục bài viết!";
                return RedirectToBlogCategories();
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
            return RedirectToBlogCategories();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, BlogCategory category)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            if (id != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật danh mục!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogCategoryExists(category.CategoryId)) return NotFound();
                    throw;
                }

                return RedirectToBlogCategories();
            }

            TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ!";
            return RedirectToBlogCategories();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var category = await _context.BlogCategories.FindAsync(id);
            if (category != null)
            {
                _context.BlogCategories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa danh mục!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục để xóa!";
            }

            return RedirectToBlogCategories();
        }

        // =========================
        // Danh sách bài viết
        // GET: /RealEstateAdmin/Blog
        // =========================
        public IActionResult Index(string? searchString, int? categoryId, string? status)
        {
            if (!EnsureAdmin()) return RedirectToAdminHome();

            var query = _context.Blogs
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Author)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(b =>
                    (b.Title != null && b.Title.Contains(s)) ||
                    (b.Content != null && b.Content.Contains(s)) ||
                    (b.Author != null && (
                        (b.Author.FullName != null && b.Author.FullName.Contains(s)) ||
                        (b.Author.Username != null && b.Author.Username.Contains(s))
                    ))
                );
            }

            // Filter category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            // Filter status
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "published")
                    query = query.Where(b => b.IsPublished == true);
                else if (status == "draft")
                    query = query.Where(b => b.IsPublished != true);
            }

            // Project -> ViewModel (cứng)
            var posts = query
                .OrderByDescending(b => b.CreatedAt) // sort đúng theo DateTime, không sort theo string
                .Select(b => new BlogRowVM
                {
                    Id = b.BlogId,
                    Title = b.Title ?? "",
                    Category = b.Category != null ? (b.Category.Name ?? "Chưa phân loại") : "Chưa phân loại",
                    Author = b.Author != null ? (b.Author.FullName ?? b.Author.Username ?? "Admin") : "Admin",
                    Views = b.ViewCount ?? 0,
                    Status = b.IsPublished == true ? "Công khai" : "Nháp",
                    CreatedAt = b.CreatedAt ?? DateTime.MinValue,
                    CreatedDateText = (b.CreatedAt.HasValue ? b.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : ""),
                    Image = b.Thumbnail ?? "/assets/images/default-blog.jpg"
                })
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.Categories = _context.BlogCategories
                .AsNoTracking()
                .Select(c => new { c.CategoryId, c.Name })
                .ToList();

            return View(posts);
        }

        // =========================
        // Create bài viết
        // =========================
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewBag.Categories = new SelectList(
                _context.BlogCategories,
                "CategoryId",
                "Name"
            );

            ViewBag.Authors = new SelectList(
                _context.Users.Select(u => new
                {
                    UserId = u.UserId,
                    Name = u.FullName ?? u.Username
                }),
                "UserId",
                "Name"
            );

            return View(new Blog());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Blog blog, IFormFile? thumbnailFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    _context.BlogCategories,
                    "CategoryId",
                    "Name",
                    blog.CategoryId
                );

                ViewBag.Authors = new SelectList(
                    _context.Users.Select(u => new
                    {
                        UserId = u.UserId,
                        Name = u.FullName ?? u.Username
                    }),
                    "UserId",
                    "Name",
                    blog.AuthorId
                );

                return View(blog);
            }

            blog.CreatedAt = DateTime.UtcNow;
            blog.ModifiedAt = DateTime.UtcNow;

            // Upload thumbnail nếu có
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                blog.Thumbnail = await SaveBlogThumbnailAsync(thumbnailFile);
            }
            else
            {
                blog.Thumbnail ??= "/assets/images/default-blog.jpg";
            }

            if (blog.IsPublished == true)
                blog.PublishedAt ??= DateTime.UtcNow;

            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm bài viết thành công!";

            // ✅ HARD-CODE AREA
            return RedirectToAction(
                "Index",
                "Blog",
                new { area = "RealEstateAdmin" }
            );
        }

        // =========================
        // Edit bài viết
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.BlogId == id);
            if (blog == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
            ViewBag.Authors = new SelectList(_context.Users.Select(u => new {
                UserId = u.UserId,
                Name = u.FullName ?? u.Username
            }), "UserId", "Name", blog.AuthorId);

            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Blog blog, IFormFile? thumbnailFile, string? oldThumbnail)
        {
            if (id != blog.BlogId) return NotFound();

            var existing = await _context.Blogs.FirstOrDefaultAsync(b => b.BlogId == id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.BlogCategories, "CategoryId", "Name", blog.CategoryId);
                ViewBag.Authors = new SelectList(_context.Users.Select(u => new {
                    UserId = u.UserId,
                    Name = u.FullName ?? u.Username
                }), "UserId", "Name", blog.AuthorId);
                return View(blog);
            }

            // ✅ map các field được sửa
            existing.Title = blog.Title;
            existing.Slug = blog.Slug;
            existing.Content = blog.Content;
            existing.Tags = blog.Tags;
            existing.CategoryId = blog.CategoryId;
            existing.AuthorId = blog.AuthorId;

            existing.ModifiedAt = DateTime.UtcNow;

            var wasPublished = existing.IsPublished == true;
            var nowPublished = blog.IsPublished == true;
            existing.IsPublished = blog.IsPublished;

            if (!wasPublished && nowPublished)
                existing.PublishedAt ??= DateTime.UtcNow;

            // ✅ upload thumbnail mới nếu có
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                var newPath = await SaveBlogThumbnailAsync(thumbnailFile);
                existing.Thumbnail = newPath;

                // xoá ảnh cũ nếu thuộc uploads/blog
                if (!string.IsNullOrWhiteSpace(oldThumbnail) && oldThumbnail.StartsWith("/uploads/blog/"))
                {
                    var physical = Path.Combine(_env.WebRootPath, oldThumbnail.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
            }
            else
            {
                // không upload => giữ ảnh cũ
                if (!string.IsNullOrWhiteSpace(oldThumbnail))
                    existing.Thumbnail = oldThumbnail;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!EnsureAdmin())
                return RedirectToAction("Index", "Home", new { area = "" });

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết để xóa!";
                return RedirectToAction(
                    "Index",
                    "Blog",
                    new { area = "RealEstateAdmin" }
                );
            }

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa bài viết!";
            return RedirectToAction(
                "Index",
                "Blog",
                new { area = "RealEstateAdmin" }
            );
        }

        private bool BlogCategoryExists(int id) => _context.BlogCategories.Any(e => e.CategoryId == id);
        private bool BlogExists(int id) => _context.Blogs.Any(e => e.BlogId == id);

        private async Task<string> SaveBlogThumbnailAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Chỉ cho phép JPG/JPEG/PNG/WEBP.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "blog");
            Directory.CreateDirectory(folder);

            var fileName = $"blog-{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(folder, fileName);

            using var stream = new FileStream(savePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/blog/" + fileName; // ✅ DB lưu dạng này
        }

    }
}
