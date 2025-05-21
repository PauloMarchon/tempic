using Tempic.Models;

namespace Tempic.Interfaces
{
    public interface IImageMetadataRepository : IDisposable 
    {
        Task<ImageMetadata> GetImageMetadataByUniqueLinkIdAsync(Guid uniqueLinkId);
        Task InsertImageMetadataAsync(ImageMetadata imageMetadata);
        Task DeleteImageMetadataAsync(Guid uniqueLinkId);
        Task SaveChangesAsync();
    }
}
