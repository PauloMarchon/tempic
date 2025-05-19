using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Tempic.Data;
using Tempic.Exceptions;
using Tempic.Models;
using Tempic.Settings;

namespace Tempic.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _minioSettings;
        private readonly string[] _allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly ILogger<ImageUploadService> _logger;

        public ImageUploadService(
            AppDbContext dbContext,
            IMinioClient minioClient,
            MinioSettings minioSettings,
            ILogger<ImageUploadService> logger)
        {
            _dbContext = dbContext;
            _minioClient = minioClient;
            _minioSettings = minioSettings;
            _logger = logger;
        }

        public async Task<Guid> UploadImageAsync(Stream fileStream, string fileName, TimeSpan expirationDuration)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                _logger.LogError("File stream is null or empty.");
                throw new ArgumentNullException(nameof(fileStream));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("File name is null or empty.");
                throw new ArgumentNullException(nameof(fileName));
            }

            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                _logger.LogError($"File extension '{fileExtension}' is not allowed.");
                throw new ArgumentException($"File extension '{fileExtension}' is not allowed.", nameof(fileName));
            }

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var uniqueLinkId = Guid.NewGuid();
            _logger.LogInformation($"Generated unique link ID: {uniqueLinkId}");

            var uploadDateUtc = DateTime.UtcNow;
            var expirationDate = uploadDateUtc.Add(expirationDuration);
            _logger.LogInformation($"Upload date: {uploadDateUtc}, Expiration date: {expirationDate}");

            var extension = Path.GetExtension(fileName);
            var objectName = $"{uniqueLinkId}{extension}";
            _logger.LogInformation($"Generated object name: {objectName}");

            try
            {
                var putArgs = new PutObjectArgs()
                    .WithBucket(_minioSettings.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithContentType("application/octet-stream")
                    .WithObjectSize(fileStream.Length);

                await _minioClient.PutObjectAsync(putArgs).ConfigureAwait(false);
                _logger.LogInformation($"Image uploaded to MinIO: {objectName}");
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "Error uploading image to MinIO");
                throw new Exception("Error uploading image to MinIO", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading image to MinIO");
                throw new Exception("Unexpected error uploading image to MinIO", ex);
            }

            var metadata = new ImageMetadata
            {
                UniqueLinkId = uniqueLinkId,
                OriginalFileName = fileName,
                MinioBucketName = _minioSettings.BucketName,
                MinioObjectName = objectName,
                ExpirationDateUtc = expirationDate,
                UploadDateUtc = uploadDateUtc
            };
            _logger.LogInformation($"Image metadata created for link: {uniqueLinkId}");

            try
            {
                _dbContext.ImageMetadatas.Add(metadata);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation($"Image metadata saved to database for link: {uniqueLinkId}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving image metadata to database");
                throw new Exception("Error saving image metadata to database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving image metadata to database");
                throw new Exception("Unexpected error saving image metadata to database", ex);
            }

            return uniqueLinkId;
        }

        public async Task GetImageStreamAsync(Guid uniqueLinkId, Stream outputStream)
        {
            var metadata = await GetImageMetadataAsync(uniqueLinkId);

            if (metadata == null)
                throw new ImageNotFoundOrExpiredException();

            _logger.LogInformation($"Retrieving image stream for unique link ID: {uniqueLinkId}");

            try
            {
                var bucketName = metadata.MinioBucketName;
                var objectName = metadata.MinioObjectName;

                if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
                {
                    _logger.LogError($"Bucket name or object name is null or empty for unique link ID: {uniqueLinkId}");
                    throw new InvalidOperationException("Bucket name or object name is null or empty.");
                }

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(async output =>
                    {
                        _logger.LogInformation($"Writing image stream to output stream for unique link ID: {uniqueLinkId}");
                        await output.CopyToAsync(outputStream).ConfigureAwait(false);
                        _logger.LogInformation($"Image stream written to output stream for unique link ID: {uniqueLinkId}");
                    });

                await _minioClient.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
                _logger.LogInformation($"Image stream retrieved from MinIO for unique link ID: {uniqueLinkId}");
            }
            catch (ObjectNotFoundException e)
            {
                _logger.LogError(e, $"Image not found in MinIO for unique link ID: {uniqueLinkId}");
                throw new ImageNotFoundOrExpiredException($"Image not found in MinIO for unique link ID: {uniqueLinkId}", e);
            }
            catch (MinioException e)
            {
                _logger.LogError(e, $"Error retrieving image from MinIO for unique link ID: {uniqueLinkId}");
                throw new Exception($"Error retrieving image from MinIO for unique link ID: {uniqueLinkId}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unexpected error retrieving image from MinIO for unique link ID: {uniqueLinkId}");
                throw new Exception($"Unexpected error retrieving image from MinIO for unique link ID: {uniqueLinkId}", e);
            }
        }

        public async Task<ImageMetadata?> GetImageMetadataAsync(Guid uniqueLinkId)
        {
            var metadata = await _dbContext.ImageMetadatas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UniqueLinkId == uniqueLinkId)
                .ConfigureAwait(false);

            if (metadata == null)
            {
                _logger.LogWarning($"No metadata found for unique link ID: {uniqueLinkId}");
                return null;
            }

            if (metadata.ExpirationDateUtc < DateTime.UtcNow)
            {
                _logger.LogWarning($"Metadata for unique link ID: {uniqueLinkId} has expired.");
                return null;
            }

            _logger.LogInformation($"Metadata retrieved for unique link ID: {uniqueLinkId}");
            return metadata;
        }

        public async Task DeleteImageAsync(Guid uniqueLinkId)
        {
            _logger.LogInformation($"Deleting image with unique link ID: {uniqueLinkId}");

            var metadata = _dbContext.ImageMetadatas
                .FirstOrDefault(m => m.UniqueLinkId == uniqueLinkId) ?? throw new ImageNotFoundOrExpiredException($"Image with unique link ID: {uniqueLinkId} not found.");

            var bucketName = metadata.MinioBucketName;
            var objectName = metadata.MinioObjectName;

            if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            {
                _logger.LogError($"Bucket name or object name is null or empty for unique link ID: {uniqueLinkId}");
                throw new InvalidOperationException("Bucket name or object name is null or empty.");
            }
            else
            {
                try
                {
                    var removeObjectArgs = new RemoveObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName);

                    await _minioClient.RemoveObjectAsync(removeObjectArgs).ConfigureAwait(false);
                    _logger.LogInformation($"Image removed from MinIO for unique link ID: {uniqueLinkId}");
                }
                catch (MinioException ex)
                {
                    _logger.LogError(ex, $"Error removing object from MinIO for unique link ID: {uniqueLinkId}");
                    throw new Exception($"Error removing object from MinIO for unique link ID: {uniqueLinkId}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error removing object from MinIO for unique link ID: {uniqueLinkId}");
                    throw new Exception($"Unexpected error removing object from MinIO for unique link ID: {uniqueLinkId}", ex);
                }
            }

            try
            {
                _dbContext.ImageMetadatas.Remove(metadata);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation($"Image metadata deleted from database for unique link ID: {uniqueLinkId}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error deleting image metadata from database for unique link ID: {uniqueLinkId}");
                throw new Exception($"Error deleting image metadata from database for unique link ID: {uniqueLinkId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error deleting image metadata from database for unique link ID: {uniqueLinkId}");
                throw new Exception($"Unexpected error deleting image metadata from database for unique link ID: {uniqueLinkId}", ex);
            }

            _logger.LogInformation($"Image with unique link ID: {uniqueLinkId} deleted successfully.");
        }
    }
}
