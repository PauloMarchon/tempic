using System.IO;
using System.Security.Cryptography;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.Exceptions;
using Tempic.Settings;

namespace Tempic.Services
{
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _minioSettings;
        private readonly ILogger<MinioService> _logger;
        public MinioService(MinioClient minioClient, MinioSettings minioSettings, ILogger<MinioService> logger)
        {
            _minioClient = minioClient;
            _minioSettings = minioSettings;
            _logger = logger;
        }
        public async Task StartAsync()
        {
            // Implementation for starting the MinIO service
            // This could include checking if the bucket exists, creating it if not, etc.
            try
            {
                bool bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs()
                   .WithBucket(_minioSettings.BucketName));

                if (!bucketExists)
                {
                    _minioClient.MakeBucketAsync(new MakeBucketArgs()
                        .WithBucket(_minioSettings.BucketName)).Wait();
                    _logger.LogInformation($"Bucket {_minioSettings.BucketName} created.");
                }
                else
                {
                    _logger.LogInformation($"Bucket {_minioSettings.BucketName} already exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting MinIO service: {ex.Message}");
                throw new Exception($"Error starting MinIO service: {ex.Message}");
            }
        }
      
        public async Task UploadFileAsync(string bucketName, string objectName, Stream fileStream)
        {
            try
            {
                //Aes aesEncryption = Aes.Create();
                //aesEncryption.KeySize = 256;
                //aesEncryption.GenerateKey();
                //var ssec = new SSEC(aesEncryption.Key);

                PutObjectArgs putArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithContentType("application/octet-stream");
                //.WithServerSideEncryption(ssec);

                _logger.LogInformation($"Uploading file to MinIO: {objectName}");
                await _minioClient.PutObjectAsync(putArgs).ConfigureAwait(false);
                _logger.LogInformation($"File uploaded to MinIO: {objectName}");
            }
            catch (MinioException ex)
            {
                _logger.LogError($"Error uploading file to MinIO: {ex.Message}");
                throw new Exception($"Error uploading object to MinIO: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                throw new Exception($"Unexpected error uploading object to MinIO: {ex.Message}");
            }
        }
        public async Task UploadFileAsync(string bucketName, string objectName, byte[] fileBytes)
        {
            try
            {
                using var byteStream = new MemoryStream(fileBytes);

                var putArgs = new PutObjectArgs()
                          .WithBucket(bucketName)
                          .WithObject(objectName)
                          .WithStreamData(byteStream)
                          .WithContentType("application/octet-stream");

                await _minioClient.PutObjectAsync(putArgs).ConfigureAwait(false);
                _logger.LogInformation($"File uploaded to MinIO: {objectName}");
            }
            catch (MinioException ex)
            {
                _logger.LogError($"Error uploading file to MinIO: {ex.Message}");
                throw new Exception($"Error uploading object to MinIO: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                throw new Exception($"Unexpected error uploading object to MinIO: {ex.Message}");
            }
        }
        public async Task GetFileAsync(string bucketName, string objectName, Stream outputStream)
        {
            try
            {
                StatObjectArgs statArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                await _minioClient.StatObjectAsync(statArgs);

                GetObjectArgs getArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream((stream) =>
                        {
                            stream.CopyTo(outputStream);
                        });
                await _minioClient.GetObjectAsync(getArgs).ConfigureAwait(false);
                _logger.LogInformation($"File uploaded to MinIO: {objectName}");
            }
            catch (MinioException ex)
            {
                _logger.LogError($"Error getting file from MinIO: {ex.Message}");
                throw new Exception($"Error getting object from MinIO: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                throw new Exception($"Unexpected error getting object from MinIO: {ex.Message}");
            }
        }
        public async Task DeleteFileAsync(string bucketName, string objectName)
        {
            try
            {
                RemoveObjectArgs removeArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);

                await _minioClient.RemoveObjectAsync(removeArgs).ConfigureAwait(false);
                _logger.LogInformation($"File removed from MinIO: {objectName}");

            }
            catch (MinioException ex)
            {
                _logger.LogError($"Error deleting file from MinIO: {ex.Message}");
                throw new Exception($"Error removing object from MinIO");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                throw new Exception($"Unexpected error removing object from MinIO");
            }
        }
    }
}
