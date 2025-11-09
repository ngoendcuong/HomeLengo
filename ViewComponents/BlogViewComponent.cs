using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.ViewComponents
{
    public class BlogViewComponent : ViewComponent
    {
        private readonly HomeLengoContext _context;

        public BlogViewComponent(HomeLengoContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = _context.Blogs
                                .Include(m => m.Category)     
                                .Include(m => m.BlogComments)
                                .Include(m => m.Author)
                                .Where(m => m.IsPublished == true); 

            return await Task.FromResult<IViewComponentResult>(
                View(items.OrderByDescending(m => m.PublishedAt)
                          .ThenByDescending(m => m.BlogId)
                          .ToList())
            );
        }

    }
}
