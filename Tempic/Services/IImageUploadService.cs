using Tempic.Models;

namespace Tempic.Services
{
    public interface IImageUploadService
    {
        Task<Guid> UploadImageAsync(Stream fileStream, string fileName, TimeSpan expirationDuration);
        Task<ImageMetadata?> GetImageMetadataAsync(Guid uniqueLinkId);
        Task GetImageStreamAsync(Guid uniqueLinkId, Stream outputStream);
        Task DeleteImageAsync(Guid uniqueLinkId);
    }
}
