using Minio.Exceptions;

namespace Tempic.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly IMinioService _minioService;
        private readonly string _bucketName;
        private readonly ILogger<ImageUploadService> _logger;
        
        public ImageUploadService(IMinioService minioService, ILogger<ImageUploadService> logger, IConfiguration configuration)
        {
            _minioService = minioService;
            _bucketName = configuration.GetValue<string>("MinioSettings:ImagesBucketName");
            _logger = logger;
        }
 
        public async Task UploadImageAsync(IFormFile image, string objectName)
        {
            _logger.LogInformation("Starting image upload process...");
            var memoryStream = new MemoryStream();

            try
            {
                using var stream = image.OpenReadStream();
                await stream.CopyToAsync(memoryStream);

                if (memoryStream.Length == 0)
                {
                    _logger.LogError("File stream is empty after copying.");
                    throw new InvalidOperationException("File stream is empty after copying.");
                }

                memoryStream.Position = 0;

                byte[] fileBytes = memoryStream.ToArray();
                //using var byteStream = new MemoryStream(fileBytes);

                _logger.LogInformation("Uploading file to MinIO...");
                await _minioService.UploadFileAsync(_bucketName, objectName, fileBytes);
                _logger.LogInformation("File uploaded to MinIO successfully.");
            }
            catch (MinioException ex)
            {
                _logger.LogError("Minio error: {Message}", ex.Message);
                throw new MinioException($"Minio error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while uploading the image: {Message}", ex.Message);
                throw new Exception($"An error occurred while uploading the image: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation("Disposing memory stream...");
                memoryStream.Dispose();
            }
        }

        public async Task<Stream> GetImageStreamAsync(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                throw new InvalidOperationException($"Invalid object name: {objectName}");

            _logger.LogInformation("Retrieving image from MinIO...");
            return await _minioService.GetFileAsync(_bucketName, objectName);
        }

        public async Task DeleteImageAsync(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                throw new InvalidOperationException($"Invalid object name: {objectName}");

            try
            {
                _logger.LogInformation("Deleting image from MinIO...");
                await _minioService.DeleteFileAsync(_bucketName, objectName);
            }
            catch (MinioException ex)
            {
                _logger.LogError("Minio error: {Message}", ex.Message);
                throw new MinioException($"Minio error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the image: {Message}", ex.Message);
                throw new Exception($"An error occurred while deleting the image: {ex.Message}");
            }          
        }
    }
}
