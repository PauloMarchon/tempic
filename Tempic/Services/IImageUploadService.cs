using Tempic.DTOs;
using Tempic.Models;

namespace Tempic.Services
{
    public interface IImageUploadService
    {
        Task<List<Guid>> UploadImageAsync(List<UploadImageRequest> requests);
        Task<Stream> GetImageStreamAsync(Guid uniqueLinkId);
        Task DeleteImageAsync(Guid uniqueLinkId);
    }
}
