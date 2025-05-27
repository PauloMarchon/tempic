using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Tempic.Data;
using Tempic.DTOs;
using Tempic.Exceptions;
using Tempic.Models;

namespace Tempic.Services
{
    public class ImageMetadataService : IImageMetadataService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageUploadService _imageUploadService;
        private readonly IValidator<UploadImageRequest> _validator;
        private readonly ILogger<ImageMetadataService> _logger;
        private readonly ShortCodeGenerator _shortCodeGenerator;
        private readonly string _bucketName;  

        public ImageMetadataService(
            IUnitOfWork unitOfWork, 
            IImageUploadService imageUploadService, 
            IValidator<UploadImageRequest> validator,
            ILogger<ImageMetadataService> logger,
            IConfiguration configuration,
            ShortCodeGenerator shortCodeGenerator)
        {
            _unitOfWork = unitOfWork;
            _imageUploadService = imageUploadService;
            _validator = validator;
            _logger = logger;
            _shortCodeGenerator = shortCodeGenerator;
            _bucketName = configuration.GetValue<string>("MinioSettings:ImagesBucketName");      
        }

        public async Task<List<string>> AddImageMetadataAsync(List<UploadImageRequest> uploadImageRequests)
        {
            var uniqueLinkIdResults = new List<string>();

            _logger.LogInformation("Validating requests...");
            foreach (var request in uploadImageRequests)
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

            _logger.LogInformation("All requests validated successfully. Starting upload...");
            foreach (var imageRequest in uploadImageRequests)
            {
                try
                {
                    await _unitOfWork.BeginTransactionAsync();

                    var uniqueLinkId = Guid.NewGuid();
                    var uploadDateUtc = DateTime.UtcNow;
                    var expirationDate = uploadDateUtc.AddMinutes(imageRequest.DurationMinutes);
                    var extension = Path.GetExtension(imageRequest.File.FileName);
                    var objectName = $"{uniqueLinkId}{extension}";

                    var imageMetadata = new ImageMetadata
                    {
                        UniqueLinkId = uniqueLinkId,
                        OriginalFileName = imageRequest.File.FileName,
                        MinioBucketName = _bucketName,
                        MinioObjectName = objectName,
                        ExpirationDateUtc = expirationDate,
                        UploadDateUtc = uploadDateUtc
                    };

                    await _unitOfWork.ImageMetadataRepository.InsertImageMetadataAsync(imageMetadata);

                    await _imageUploadService.UploadImageAsync(imageRequest.File, objectName);

                    _logger.LogInformation("Image uploaded successfully. Generating shortened link...");
                    string shortCode = _shortCodeGenerator.GenerateUniqueShortCode();

                    var shortenedUrl = new ShortenedUrl
                    {
                        ShortCode = shortCode,
                        ImageUniqueLinkId = imageMetadata.UniqueLinkId,
                        CreationDateUtc = DateTime.UtcNow,
                        ExpirationDateUtc = imageMetadata.ExpirationDateUtc
                    };

                    await _unitOfWork.ShortenedUrlRepository.InsertShortenedUrlAsync(shortenedUrl);
                    _logger.LogInformation("Shortened link generated successfully: {ShortCode}", shortCode);
                    
                    // Commit the transaction
                    await _unitOfWork.CommitAsync();

                    uniqueLinkIdResults.Add(shortCode);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update error while saving image metadata.");
                    await _unitOfWork.RollbackAsync();     
                    throw new Exception("Error saving image metadata to database", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the image.");
                    await _unitOfWork.RollbackAsync();
                    throw new Exception($"An error occurred while uploading the image: {ex.Message}");
                }
            }
            return uniqueLinkIdResults;
        }

        public async Task<ImageMetadata?> GetImageMetadataAsync(Guid uniqueLinkId)
        {
            var imageMetadata = await _unitOfWork.ImageMetadataRepository
                .GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);

            return imageMetadata;
        }

        public async Task<Stream> GetImageMetadataStreamByUniqueLinkIdAsync(Guid uniqueLinkId)
        {
            var imageMetadata = await _unitOfWork.ImageMetadataRepository
                .GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);

            if (imageMetadata == null)
                throw new ImageNotFoundOrExpiredException($"Image not found.");
            
            if (imageMetadata.ExpirationDateUtc < DateTime.UtcNow)
                throw new ImageNotFoundOrExpiredException($"Image has expired.");

            var objectName = imageMetadata.MinioObjectName;

            return await _imageUploadService.GetImageStreamAsync(objectName);
        }

        public async Task DeleteImageMetadataAsync(Guid uniqueLinkId)
        {
            var imageMetadata = await _unitOfWork.ImageMetadataRepository
                .GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);

            if (imageMetadata == null)
                throw new ImageNotFoundOrExpiredException($"Image not found.");

            var objectName = imageMetadata.MinioObjectName;

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                await _unitOfWork.ImageMetadataRepository.DeleteImageMetadataAsync(imageMetadata.UniqueLinkId);

                await _imageUploadService.DeleteImageAsync(objectName);

                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting image metadata from database");
                await _unitOfWork.RollbackAsync();
                throw new Exception("Error deleting image metadata from database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the image: {Message}", ex.Message);
                await _unitOfWork.RollbackAsync();
                throw new Exception($"An error occurred while deleting the image: {ex.Message}");
            }      
        }

        public async Task<bool> IsImageExpiredAsync(Guid uniqueLinkId)
        {
            var imageMetadata = await _unitOfWork.ImageMetadataRepository
                .GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);

            return imageMetadata.ExpirationDateUtc < DateTime.UtcNow;
        }
    }
}
