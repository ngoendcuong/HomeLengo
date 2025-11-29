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

        public async Task<IViewComponentResult> InvokeAsync(int? propertyTypeId = null)
        {
            var query = _context.Properties
                                .Include(m => m.PropertyType)
                                .Include(m => m.Status)
                                .Include(m => m.City)
                                .Include(m => m.District)
                                .Include(m => m.Neighborhood)
                                .Include(m => m.PropertyPhotos)
                                .Include(m => m.PropertyFeatures)
                                .Include(p => p.Agent)
                                    .ThenInclude(a => a.User)
                                .AsQueryable();

            // Nếu có propertyTypeId thì lọc, không thì hiển thị tất cả
            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
            {
                query = query.Where(m => m.PropertyTypeId == propertyTypeId.Value);
            }

            var items = query.OrderByDescending(m => m.IsFeatured == true)
                            .ThenByDescending(m => m.PropertyId)
                            .ToList();

            return await Task.FromResult<IViewComponentResult>(View(items));
        }
    }
}