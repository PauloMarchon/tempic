using Tempic.Models;

namespace Tempic.Repository
{
    public interface IImageMetadataRepository : IDisposable 
    {
        Task<ImageMetadata> GetImageMetadataByUniqueLinkIdAsync(Guid uniqueLinkId);
        Task InsertImageMetadataAsync(ImageMetadata imageMetadata);
        Task DeleteImageMetadataAsync(Guid uniqueLinkId);
    }
}
