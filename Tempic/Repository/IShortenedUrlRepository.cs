namespace Tempic.Repository
{
    public interface IShortenedUrlRepository
    {
        Task<string> GenerateUniqueShortCodeAsync();
    }
}
