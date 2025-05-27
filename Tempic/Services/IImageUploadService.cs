namespace Tempic.Services
{
    public interface IImageUploadService
    {
        Task UploadImageAsync(IFormFile image, string objectName);
        Task<Stream> GetImageStreamAsync(string objectName);
        Task DeleteImageAsync(string objectName);
    }
}
