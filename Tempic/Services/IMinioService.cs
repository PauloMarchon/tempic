namespace Tempic.Services
{
    public interface IMinioService
    {
        Task StartAsync();
        Task UploadFileAsync(string bucketName, string objectName, Stream fileStream);
        Task UploadFileAsync(string bucketName, string objectName, byte[] fileBytes);
        Task<Stream> GetFileAsync(string bucketName, string objectName);
        Task DeleteFileAsync(string bucketName, string objectName);
    }
}
