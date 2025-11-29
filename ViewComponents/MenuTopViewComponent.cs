using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeLengo.ViewComponents
{
    public class MenuTopViewComponent : ViewComponent
    {
        private readonly HomeLengoContext _context;

        public MenuTopViewComponent(HomeLengoContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = _context.Menus
                                .Where(m => (bool)m.IsActive)
                                .OrderBy(m => m.SortOrder)
                                .ToList();
            return await Task.FromResult(View(items));
        }
    }
}
