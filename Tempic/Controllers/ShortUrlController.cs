using Microsoft.AspNetCore.Mvc;
using Tempic.Repository;

namespace Tempic.Controllers
{
    [ApiController]
    [Route("tempic")]
    public class ShortUrlController : ControllerBase
    {
        private readonly IShortenedUrlRepository _shortenedUrlRepository;
        private readonly ILogger<ShortUrlController> _logger;

        public ShortUrlController(IShortenedUrlRepository shortenedUrlRepository, ILogger<ShortUrlController> logger)
        {
            _shortenedUrlRepository = shortenedUrlRepository;
            _logger = logger;
        }

        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectToImage(string shortCode)
        {
            var shortenedUrl = await _shortenedUrlRepository.GetShortenedUrlByShortCodeAsync(shortCode);

            if (shortenedUrl == null)
            {
                _logger.LogWarning("Shortened URL not found for code: {ShortCode}", shortCode);
                return NotFound();
            }
            if (shortenedUrl.ExpirationDateUtc.HasValue && shortenedUrl.ExpirationDateUtc.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Shortened URL for code {ShortCode} has expired", shortCode);
                return NotFound();
            }

            var originalImageUrl = Url.Action(
                action: "GetImage", 
                controller: "Image", 
                values: new { uniqueLinkId = shortenedUrl.ImageUniqueLinkId }, 
                protocol: Request.Scheme
                );

            if (string.IsNullOrEmpty(originalImageUrl))
            {
                _logger.LogError("Failed to generate URL for image with unique link ID: {UniqueLinkId}", shortenedUrl.ImageUniqueLinkId);
                return NotFound();
            }

            _logger.LogInformation("Redirecting to original image URL: {OriginalImageUrl}", originalImageUrl);
            return Redirect(originalImageUrl);
        }
    }
}
