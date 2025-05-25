using System.Security.Cryptography;
using Tempic.Interfaces;
using Tempic.Models;
using Tempic.Repository;

namespace Tempic.Services
{
    public class ShortCodeGeneratorService : IShortCodeGeneratorService
    {
        private const string ShortCodeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int ShortCodeLength = 6;

        private readonly IShortenedUrlRepository _shortenedUrlRepository;
        private readonly IImageMetadataRepository _imageMetadataRepository;
        private readonly ILogger<ShortCodeGeneratorService> _logger;

        public ShortCodeGeneratorService(IShortenedUrlRepository shortenedUrlRepository, IImageMetadataRepository imageMetadataRepository, ILogger<ShortCodeGeneratorService> logger)
        {
            _shortenedUrlRepository = shortenedUrlRepository;
            _imageMetadataRepository = imageMetadataRepository;
            _logger = logger;
        }

        public async Task<List<string>> BuildShortLinkToImage(List<Guid> imagesUniqueLinkId)
        {
            List<string> shortLinks = new List<string>();

            foreach (var imageUniqueLinkId in imagesUniqueLinkId)
            {
                try
                {
                    var imageMetadata = await _imageMetadataRepository.GetImageMetadataByUniqueLinkIdAsync(imageUniqueLinkId);
                    if (imageMetadata == null)
                    {
                        _logger.LogError($"Image metadata not found for UniqueLinkId: {imageUniqueLinkId}");
                        throw new KeyNotFoundException($"Image with UniqueLinkId {imageUniqueLinkId} not found.");
                    }

                    _logger.LogInformation($"Generating short code for image with UniqueLinkId: {imageUniqueLinkId}");
                    var shortCode = await GenerateUniqueShortCodeAsync();

                    var shortenedUrl = new ShortenedUrl
                    {
                        ShortCode = shortCode,
                        ImageUniqueLinkId = imageUniqueLinkId,
                        CreationDateUtc = DateTime.UtcNow,
                        ExpirationDateUtc = imageMetadata.ExpirationDateUtc
                    };

                    await _shortenedUrlRepository.InsertShortenedUrlAsync(shortenedUrl);

                    shortLinks.Add(shortCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating short link for image with UniqueLinkId: {imageUniqueLinkId}");
                    throw;
                }
            }
            return shortLinks;
        }

        public async Task<string> GenerateUniqueShortCodeAsync()
        {
            string shortCode;
            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                shortCode = GenerateRandomShortCode(ShortCodeLength);
                // Check if the generated short code already exists
                var existingUrl = _shortenedUrlRepository.GetShortenedUrlByShortCodeAsync(shortCode).Result;
                if (existingUrl == null)
                {
                    _logger.LogInformation($"Generated unique short code: {shortCode} on attempt {attempt + 1}");
                    return shortCode;
                }
                _logger.LogWarning($"Short code {shortCode} already exists. Attempt {attempt + 1} of {maxAttempts}.");
            }

            throw new InvalidOperationException("Unable to generate a unique short code after multiple attempts.");
        }

        private string GenerateRandomShortCode(int length)
        {
            char[] shortCode = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                for (int i = 0; i < length; i++)
                {
                    shortCode[i] = ShortCodeCharacters[randomBytes[i] % ShortCodeCharacters.Length];
                }
            }
            return new string(shortCode);
        }
    }
}
