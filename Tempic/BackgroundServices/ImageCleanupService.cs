using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tempic.Data;
using Tempic.Services;
using Tempic.Settings;

namespace Tempic.BackgroundServices
{
    public class ImageCleanupService : BackgroundService
    {
        private readonly ILogger<ImageCleanupService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _cleanupInterval;

        public ImageCleanupService(
            ILogger<ImageCleanupService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<CleanupSettings> cleanupSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _cleanupInterval = TimeSpan.FromMinutes(cleanupSettings.Value.CleanupIntervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image cleanup service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Image cleanup service is running.");
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var imageUploadService = scope.ServiceProvider.GetRequiredService<IImageUploadService>();

                        var expiredImages = await dbContext.ImageMetadatas
                            .Where(i => i.ExpirationDateUtc < DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        if (expiredImages.Count == 0)
                        {
                            _logger.LogInformation("No expired images found.");
                        }
                        else
                        {
                            _logger.LogInformation($"Found {expiredImages.Count} expired images to delete.");
                            foreach (var image in expiredImages)
                            {
                                try
                                {
                                    await imageUploadService.DeleteImageAsync(image.MinioObjectName);
                                    dbContext.ImageMetadatas.Remove(image);
                                    _logger.LogInformation($"Deleted expired image with ID: {image.UniqueLinkId}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error deleting image with ID: {image.UniqueLinkId}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during image cleanup");
                }
                _logger.LogInformation("Image cleanup service completed a run.");
                await Task.Delay(_cleanupInterval, stoppingToken);      
            }
        }
    }
}
