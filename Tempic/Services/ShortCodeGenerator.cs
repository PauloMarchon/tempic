using System.Security.Cryptography;
using Tempic.Repository;

namespace Tempic.Services
{
    public class ShortCodeGenerator
    {
        private const string ShortCodeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int ShortCodeLength = 6;

        private readonly IShortenedUrlRepository _shortenedUrlRepository;
        private readonly ILogger<ShortCodeGenerator> _logger;

        public ShortCodeGenerator(IShortenedUrlRepository shortenedUrlRepository, ILogger<ShortCodeGenerator> logger)
        {
            _shortenedUrlRepository = shortenedUrlRepository;
            _logger = logger;
        }

        public string GenerateUniqueShortCode()
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
