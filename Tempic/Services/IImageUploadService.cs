using Tempic.DTOs;
using Tempic.Models;

namespace Tempic.Services
{
    public interface IImageUploadService
    {
        Task<List<Guid>> UploadImageAsync(List<UploadImageRequest> requests);
        Task GetImageStreamAsync(Guid uniqueLinkId, Stream outputStream);
        Task DeleteImageAsync(Guid uniqueLinkId);
    }
}
