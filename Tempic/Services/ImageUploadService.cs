using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Minio.Exceptions;
using Tempic.Data;
using Tempic.DTOs;
using Tempic.Exceptions;
using Tempic.Models;

namespace Tempic.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMinioService _minioService;
        private readonly ShortCodeGenerator _shortCodeGenerator;
        private readonly string _bucketName;
        private readonly IValidator<UploadImageRequest> _validator;
        private readonly ILogger<ImageUploadService> _logger;
        
        public ImageUploadService(IUnitOfWork unitOfWork, IMinioService minioService, ShortCodeGenerator shortCodeGenerator, IValidator<UploadImageRequest> validator, ILogger<ImageUploadService> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _minioService = minioService;
            _shortCodeGenerator = shortCodeGenerator;
            _bucketName = configuration.GetValue<string>("MinioSettings:ImagesBucketName");
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<string>> UploadImageAsync(List<UploadImageRequest> requests)
        {
            _logger.LogInformation("Starting image upload process...");
            var uniqueLinkIdResults = new List<string>();

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
                    await _unitOfWork.BeginTransactionAsync();

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
                    await _unitOfWork.ImageMetadataRepository.InsertImageMetadataAsync(imageMetadata);
                    //await _unitOfWork.SaveChangesAsync();

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

                    _logger.LogInformation("Generating unique short code for the image...");
                    string shortCode = _shortCodeGenerator.GenerateUniqueShortCode();

                    var shortenedUrl = new ShortenedUrl
                    {
                        ShortCode = shortCode,
                        ImageUniqueLinkId = imageMetadata.UniqueLinkId,
                        CreationDateUtc = DateTime.UtcNow,
                        ExpirationDateUtc = imageMetadata.ExpirationDateUtc
                    };

                    _logger.LogInformation("Inserting shortened URL into database with ShortCode: {ShortCode}", shortCode);
                    await _unitOfWork.ShortenedUrlRepository.InsertShortenedUrlAsync(shortenedUrl);
                  
                    await _unitOfWork.CommitAsync();

                    uniqueLinkIdResults.Add(shortCode);
                }
                catch (MinioException ex)
                {
                    _logger.LogError("Minio error: {Message}", ex.Message);
                    await _unitOfWork.RollbackAsync();
                    throw new MinioException($"Minio error: {ex.Message}");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving image metadata to database");
                    await _unitOfWork.RollbackAsync();
                    throw new Exception("Error saving image metadata to database", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError("An error occurred while uploading the image: {Message}", ex.Message);
                    await _unitOfWork.RollbackAsync();
                    throw new Exception($"An error occurred while uploading the image: {ex.Message}");
                }
                finally
                {
                    _logger.LogInformation("Disposing memory stream...");
                    memoryStream.Dispose();
                }
            }
            
            _logger.LogInformation("Image upload process completed successfully. Returning unique link IDs.");
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
            var imageMetadata = await _unitOfWork.ImageMetadataRepository.GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);
            
            if (imageMetadata == null)
                throw new ImageNotFoundOrExpiredException($"Image with UniqueLinkId {uniqueLinkId} not found.");
            
            var bucketName = imageMetadata.MinioBucketName;
            var objectName = imageMetadata.MinioObjectName;

            try
            {
                await _minioService.DeleteFileAsync(bucketName, objectName);
                
                await _unitOfWork.ImageMetadataRepository.DeleteImageMetadataAsync(imageMetadata.UniqueLinkId);

                await _unitOfWork.SaveChangesAsync();
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
