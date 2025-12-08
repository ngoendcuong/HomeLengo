using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HomeLengo.Controllers
{
    public class BlogController : Controller
    {
        private readonly HomeLengoContext _context;

        public BlogController(HomeLengoContext context)
        {
            _context = context;
        }
        public IActionResult Grid(int page = 1)
        {
            int pageSize = 6;

            var query = _context.Blogs
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Where(b => b.IsPublished == true)
                .OrderByDescending(b => b.PublishedAt);

            var totalItems = query.Count();
            var blogs = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(blogs);
        }
        public IActionResult Detail(int id)
        {
            var post = _context.Blogs
                .Include(b => b.Category)
                .FirstOrDefault(x => x.BlogId == id);

            if (post == null)
                return NotFound();

            // Danh mục
            ViewBag.Categories = _context.BlogCategories
                .Include(c => c.Blogs)
                .ToList();

            // danh mục nổi bật
            ViewBag.Featured = _context.Blogs
                .OrderByDescending(x => x.PublishedAt)
                .Take(3)
                .ToList();

            // thẻ
            ViewBag.Tags = new List<string>
            {
                "Tài sản", "Văn phòng", "Tài chính",
                "Hợp pháp","Chợ", "Đầu tư", "Cải tạo"
            };

            // BÀI VIẾT LIÊN QUAN (optional)
            ViewBag.Related = _context.Blogs
                    .Where(x => x.CategoryId == post.CategoryId && x.BlogId != post.BlogId)
                    .OrderByDescending(x => x.PublishedAt)
                    .Take(3)
                    .ToList();
            return View(post);
        }
    }
}