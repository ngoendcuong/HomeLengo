using HomeLengo.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Services
{
    public class ServicePackageService
    {
        private readonly HomeLengoContext _context;

        public ServicePackageService(HomeLengoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra user đã mua gói dịch vụ và đang active chưa
        /// </summary>
        public async Task<bool> HasActivePackageAsync(int userId)
        {
            var activePackage = await _context.UserServicePackages
                .Include(usp => usp.Plan)
                .Where(usp => usp.UserId == userId && usp.IsActive == true)
                .OrderByDescending(usp => usp.StartDate)
                .FirstOrDefaultAsync();

            return activePackage != null;
        }

        /// <summary>
        /// Lấy gói dịch vụ đang active của user
        /// </summary>
        public async Task<UserServicePackage?> GetActivePackageAsync(int userId)
        {
            var activePackage = await _context.UserServicePackages
                .Include(usp => usp.Plan)
                .ThenInclude(p => p.ServicePlanFeatures)
                .Include(usp => usp.User)
                .Where(usp => usp.UserId == userId && usp.IsActive == true)
                .OrderByDescending(usp => usp.StartDate)
                .FirstOrDefaultAsync();

            return activePackage;
        }

        /// <summary>
        /// Kiểm tra user có thể đăng thêm tin không (dựa trên giới hạn của gói)
        /// </summary>
        public async Task<(bool CanPost, int? MaxListings, int CurrentListings, string? Message)> CanPostListingAsync(int userId, int agentId)
        {
            var activePackage = await GetActivePackageAsync(userId);
            if (activePackage == null)
            {
                return (false, null, 0, "Bạn cần mua gói dịch vụ để đăng tin. Vui lòng truy cập trang gói dịch vụ để đăng ký.");
            }

            // Đếm số tin đã đăng của agent
            var currentListings = await _context.Properties
                .Where(p => p.AgentId == agentId)
                .CountAsync();

            // Lấy giới hạn từ gói (nếu có)
            var maxListings = activePackage.Plan.MaxListings;

            // Nếu không có giới hạn (null hoặc 0), cho phép đăng không giới hạn
            if (!maxListings.HasValue || maxListings.Value == 0)
            {
                return (true, null, currentListings, null);
            }

            // Kiểm tra giới hạn
            if (currentListings >= maxListings.Value)
            {
                return (false, maxListings.Value, currentListings, 
                    $"Bạn đã đạt giới hạn {maxListings.Value} tin đăng của gói {activePackage.Plan.Name}. Vui lòng nâng cấp gói để đăng thêm tin.");
            }

            return (true, maxListings.Value, currentListings, null);
        }
    }
}

