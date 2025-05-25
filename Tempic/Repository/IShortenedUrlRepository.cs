using Tempic.Models;

namespace Tempic.Repository
{
    public interface IShortenedUrlRepository
    {
        Task<ShortenedUrl> GetShortenedUrlByShortCodeAsync(string shortCode);
        Task InsertShortenedUrlAsync(ShortenedUrl shortenedUrl);
        Task DeleteShortenedUrlAsync(Guid imageUniqueLinkId);
        Task SaveChangesAsync();
    }
}
