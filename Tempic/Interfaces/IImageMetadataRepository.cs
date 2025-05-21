using Tempic.Models;

namespace Tempic.Interfaces
{
    public interface IImageMetadataRepository : IDisposable 
    {
        ImageMetadata GetImageMetadataByUniqueLinkId(Guid uniqueLinkId);
        void InsertImageMetadata(ImageMetadata imageMetadata);
        void DeleteImageMetadata(Guid uniqueLinkId);
        void SaveChanges();
    }
}
