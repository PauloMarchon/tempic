using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Minio.Exceptions;
using Tempic.DTOs;
using Tempic.Exceptions;
using Tempic.Interfaces;
using Tempic.Models;

namespace Tempic.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly IImageMetadataRepository _imageMetadataRepository;
        private readonly IMinioService _minioService;
        private readonly string _bucketName;
        private readonly IValidator<UploadImageRequest> _validator;
        private readonly ILogger<ImageUploadService> _logger;
        
        public ImageUploadService(IImageMetadataRepository imageMetadataRepository, IMinioService minioService, IValidator<UploadImageRequest> validator, ILogger<ImageUploadService> logger, IConfiguration configuration)
        {
            _imageMetadataRepository = imageMetadataRepository;
            _minioService = minioService;
            _bucketName = configuration.GetValue<string>("MinioSettings:ImagesBucketName");
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<Guid>> UploadImageAsync(List<UploadImageRequest> requests)
        {
            _logger.LogInformation("Starting image upload process...");
            var uniqueLinkIdResults = new List<Guid>();

            _logger.LogInformation("Validating requests...");
            foreach (var request in requests)
            {
                var validationResult = await _validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        _logger.LogError("Validation error: {Error}", error.ErrorMessage);
                    }
                    throw new ValidationException(validationResult.Errors);
                }
            }

            _logger.LogInformation("Requests validated successfully. Proceeding with upload...");
            foreach (var request in requests)
            {
                var memoryStream = new MemoryStream();

                try
                {
                    var uniqueLinkId = Guid.NewGuid();
                    var uploadDateUtc = DateTime.UtcNow;
                    var expirationDate = uploadDateUtc.AddMinutes(request.DurationMinutes);
                    var extension = Path.GetExtension(request.File.FileName);
                    var objectName = $"{uniqueLinkId}{extension}";

                    var imageMetadata = new ImageMetadata
                    {
                        UniqueLinkId = uniqueLinkId,
                        OriginalFileName = request.File.FileName,
                        MinioBucketName = _bucketName,
                        MinioObjectName = objectName,
                        ExpirationDateUtc = expirationDate,
                        UploadDateUtc = uploadDateUtc
                    };

                    _logger.LogInformation("Inserting image metadata into database...");
                    await _imageMetadataRepository.InsertImageMetadataAsync(imageMetadata);

                    using var stream = request.File.OpenReadStream();
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

                    _logger.LogInformation("Saving changes to database...");
                    await _imageMetadataRepository.SaveChangesAsync();

                    _logger.LogInformation("Image upload process completed successfully. UniqueLinkId: {UniqueLinkId}", uniqueLinkId);
                    uniqueLinkIdResults.Add(uniqueLinkId);
                }
                catch (MinioException ex)
                {
                    _logger.LogError("Minio error: {Message}", ex.Message);
                    throw new MinioException($"Minio error: {ex.Message}");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving image metadata to database");
                    throw new Exception("Error saving image metadata to database", ex);
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
            return uniqueLinkIdResults;
        }

        public async Task<Stream> GetImageStreamAsync(ImageMetadata imageMetadata)
        {
            _logger.LogInformation("Validating imageMetadata..");
            if (imageMetadata == null)
                throw new ImageNotFoundOrExpiredException($"Image not found/is null.");

            if (imageMetadata.ExpirationDateUtc < DateTime.UtcNow)
                throw new ImageNotFoundOrExpiredException($"Image has expired.");

            var bucketName = imageMetadata.MinioBucketName;
            var objectName = imageMetadata.MinioObjectName;

            if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(objectName))
                throw new InvalidOperationException($"Invalid bucket name or object name for UniqueLinkId: {imageMetadata.UniqueLinkId}");

            _logger.LogInformation("Retrieving image from MinIO...");
            return await _minioService.GetFileAsync(bucketName, objectName);
        }

        public async Task DeleteImageAsync(Guid uniqueLinkId)
        {
            var imageMetadata = await _imageMetadataRepository.GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);
            
            if (imageMetadata == null)
                throw new ImageNotFoundOrExpiredException($"Image with UniqueLinkId {uniqueLinkId} not found.");
            
            var bucketName = imageMetadata.MinioBucketName;
            var objectName = imageMetadata.MinioObjectName;

            try
            {
                await _minioService.DeleteFileAsync(bucketName, objectName);
                
                await _imageMetadataRepository.DeleteImageMetadataAsync(uniqueLinkId);

                await _imageMetadataRepository.SaveChangesAsync();
            }
            catch (MinioException ex)
            {
                _logger.LogError("Minio error: {Message}", ex.Message);
                throw new MinioException($"Minio error: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting image metadata from database");
                throw new Exception("Error deleting image metadata from database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the image: {Message}", ex.Message);
                throw new Exception($"An error occurred while deleting the image: {ex.Message}");
            }          
        }
    }
}
