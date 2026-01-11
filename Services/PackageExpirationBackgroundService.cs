using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeLengo.Services
{
    public class PackageExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PackageExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Kiểm tra mỗi 1 giờ

        public PackageExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PackageExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PackageExpirationBackgroundService đã bắt đầu");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var expirationService = scope.ServiceProvider.GetRequiredService<PackageExpirationService>();
                        await expirationService.ProcessExpiredPackagesAsync();
                    }

                    _logger.LogInformation("Đã hoàn thành kiểm tra gói dịch vụ hết hạn. Sẽ kiểm tra lại sau {interval}", _checkInterval);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi chạy PackageExpirationBackgroundService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("PackageExpirationBackgroundService đã dừng");
        }
    }
}

