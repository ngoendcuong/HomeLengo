using HomeLengo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.ViewComponents
{
    public class PropertyViewComponent : ViewComponent
    {
        private readonly HomeLengoContext _context;

        public PropertyViewComponent(HomeLengoContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = _context.Properties
                                .Include(m => m.PropertyType)
                                .Include(m => m.Status)
                                .Include(m => m.City)
                                .Include(m => m.District)
                                .Include(m => m.Neighborhood)
                                .Include(m => m.PropertyPhotos)
                                .Include(m => m.PropertyFeatures)
                                .Include(p => p.Agent)
                                    .ThenInclude(a => a.User)
                                .Where(m => m.PropertyTypeId == 1)
                                .OrderByDescending(m => m.IsFeatured == true)
                                ;
                                

            return await Task.FromResult<IViewComponentResult>(
                View(items.OrderByDescending(m => m.PropertyId).ToList())
            );
        }
    }
}
