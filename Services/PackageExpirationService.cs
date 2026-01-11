using HomeLengo.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Services
{
    public class PackageExpirationService
    {
        private readonly HomeLengoContext _context;
        private readonly ILogger<PackageExpirationService> _logger;

        public PackageExpirationService(HomeLengoContext context, ILogger<PackageExpirationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra và xử lý các gói dịch vụ đã hết hạn
        /// </summary>
        public async Task ProcessExpiredPackagesAsync()
        {
            try
            {
                var now = DateTime.Now;
                
                // Tìm các gói đã hết hạn nhưng vẫn đang active
                var expiredPackages = await _context.UserServicePackages
                    .Include(usp => usp.User)
                    .Where(usp => usp.IsActive == true 
                        && usp.EndDate.HasValue 
                        && usp.EndDate.Value <= now)
                    .ToListAsync();

                _logger.LogInformation($"Tìm thấy {expiredPackages.Count} gói dịch vụ đã hết hạn");

                foreach (var package in expiredPackages)
                {
                    await ProcessExpiredPackageAsync(package);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đã xử lý xong {expiredPackages.Count} gói dịch vụ hết hạn");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý gói dịch vụ hết hạn");
                throw;
            }
        }

        /// <summary>
        /// Xử lý một gói dịch vụ đã hết hạn
        /// </summary>
        private async Task ProcessExpiredPackageAsync(UserServicePackage package)
        {
            try
            {
                var userId = package.UserId;

                // 1. Vô hiệu hóa gói
                package.IsActive = false;
                _logger.LogInformation($"Đã vô hiệu hóa gói dịch vụ ID: {package.Id} cho UserId: {userId}");

                // 2. Đổi RoleId về 3 (User thường)
                // Xóa tất cả UserRole cũ của user này để tránh duplicate
                var existingUserRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                if (existingUserRoles.Any())
                {
                    _context.UserRoles.RemoveRange(existingUserRoles);
                }

                // Tạo UserRole mới với RoleId = 3 (User thường)
                var newUserRole = new UserRole
                {
                    UserId = userId,
                    RoleId = 3, // User thường
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(newUserRole);
                _logger.LogInformation($"Đã đổi RoleId về 3 (User) cho UserId: {userId}");

                // 3. Xóa tất cả các bài đăng (Properties) của user này
                // Lấy AgentId của user (nếu có)
                var agent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (agent != null)
                {
                    var properties = await _context.Properties
                        .Where(p => p.AgentId == agent.AgentId)
                        .ToListAsync();

                    if (properties.Any())
                    {
                        // Xóa các bản ghi liên quan trước
                        foreach (var property in properties)
                        {
                            // Xóa PropertyPhotos
                            var photos = await _context.PropertyPhotos
                                .Where(pp => pp.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.PropertyPhotos.RemoveRange(photos);

                            // Xóa PropertyFeatures
                            var features = await _context.PropertyFeatures
                                .Where(pf => pf.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.PropertyFeatures.RemoveRange(features);

                            // Xóa PropertyAmenities
                            var amenities = await _context.PropertyAmenities
                                .Where(pa => pa.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.PropertyAmenities.RemoveRange(amenities);

                            // Xóa Reviews liên quan
                            var reviews = await _context.Reviews
                                .Where(r => r.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.Reviews.RemoveRange(reviews);

                            // Xóa Favorites liên quan
                            var favorites = await _context.Favorites
                                .Where(f => f.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.Favorites.RemoveRange(favorites);

                            // Xóa Inquiries liên quan
                            var inquiries = await _context.Inquiries
                                .Where(i => i.PropertyId == property.PropertyId)
                                .ToListAsync();
                            _context.Inquiries.RemoveRange(inquiries);
                        }

                        // Xóa Properties
                        _context.Properties.RemoveRange(properties);
                        _logger.LogInformation($"Đã xóa {properties.Count} bài đăng của UserId: {userId} (AgentId: {agent.AgentId})");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đã xử lý xong gói dịch vụ hết hạn cho UserId: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý gói dịch vụ hết hạn ID: {package.Id}");
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra và xử lý gói hết hạn cho một user cụ thể (có thể gọi từ controller)
        /// </summary>
        public async Task ProcessExpiredPackageForUserAsync(int userId)
        {
            var expiredPackages = await _context.UserServicePackages
                .Include(usp => usp.User)
                .Where(usp => usp.UserId == userId
                    && usp.IsActive == true
                    && usp.EndDate.HasValue
                    && usp.EndDate.Value <= DateTime.Now)
                .ToListAsync();

            foreach (var package in expiredPackages)
            {
                await ProcessExpiredPackageAsync(package);
            }

            await _context.SaveChangesAsync();
        }
    }
}

