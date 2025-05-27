using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tempic.DTOs;
using Tempic.Repository;
using Tempic.Services;

namespace Tempic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageMetadataController : ControllerBase
    {
        private readonly IImageUploadService _iImageUploadService;
        private readonly IImageMetadataRepository _imageMetadataRepository;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;
        private readonly ILogger<ImageMetadataController> _logger;

        public ImageMetadataController(
            IImageUploadService iImageUploadService,
            IImageMetadataRepository imageMetadataRepository,
            FileExtensionContentTypeProvider contentTypeProvider,
            ILogger<ImageMetadataController> logger)
        {
            _iImageUploadService = iImageUploadService;
            _imageMetadataRepository = imageMetadataRepository;
            _contentTypeProvider = contentTypeProvider;
            _logger = logger;
        }

        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)] // 10 MB limit
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(
            [FromForm] List<UploadImageRequest> request)
        {
  
            _logger.LogInformation("Received file upload request.");
            var links = await _iImageUploadService.UploadImageAsync(request);

            _logger.LogInformation("File upload completed. Generating shorted links..");      
            var imagesUrls = links.Select(shortLink => Url.Action("RedirectToImage", "ShortUrl", new { shortCode = shortLink }, Request.Scheme)).ToList();

            return Ok(new UploadImageResponse { Links = imagesUrls });          
        }

        [HttpGet("{uniqueLinkId:guid}")]
        public async Task<IActionResult> GetImage(Guid uniqueLinkId)
        {
            try
            {
                _logger.LogInformation($"Received request to get image with UniqueLinkId: {uniqueLinkId}");
                var imageMetadata = await _imageMetadataRepository.GetImageMetadataByUniqueLinkIdAsync(uniqueLinkId);
             
                if (imageMetadata == null)  
                    return NotFound($"Image with UniqueLinkId {uniqueLinkId} not found.");        
                _logger.LogInformation($"Image metadata found for UniqueLinkId: {uniqueLinkId}");
                
                string contentType;
                if (!_contentTypeProvider.TryGetContentType(imageMetadata.OriginalFileName, out contentType))
                    contentType = "application/octet-stream"; // Default content type

                _logger.LogInformation($"Retrieving image stream for UniqueLinkId: {uniqueLinkId}");
                Stream imageStream = await _iImageUploadService.GetImageStreamAsync(imageMetadata);

                if (imageStream == null)
                    return NotFound($"Image with UniqueLinkId {uniqueLinkId} not found.");

                return File(imageStream, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
