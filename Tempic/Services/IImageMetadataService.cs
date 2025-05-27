using Tempic.DTOs;
using Tempic.Models;

namespace Tempic.Services
{
    public interface IImageMetadataService
    {
        Task<List<string>> AddImageMetadataAsync(List<UploadImageRequest> uploadImageRequests);
        Task<ImageMetadata> GetImageMetadataAsync(Guid uniqueLinkId);
        Task<Stream> GetImageMetadataStreamByUniqueLinkIdAsync(Guid uniqueLinkId);   
        Task DeleteImageMetadataAsync(Guid uniqueLinkId);
        Task<bool> IsImageExpiredAsync(Guid uniqueLinkId);
    }
}
