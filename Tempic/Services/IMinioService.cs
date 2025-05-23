namespace Tempic.Services
{
    public interface IMinioService
    {
        Task StartAsync();
        Task UploadFileAsync(string bucketName, string objectName, Stream fileStream);
        Task UploadFileAsync(string bucketName, string objectName, byte[] fileBytes);
        Task GetFileAsync(string bucketName, string objectName, Stream outputStream);
        Task DeleteFileAsync(string bucketName, string objectName);
    }
}
