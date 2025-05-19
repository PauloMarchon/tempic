using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tempic.Services;

namespace Tempic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageMetadataController : ControllerBase
    {
        private readonly IImageUploadService _iImageUploadService;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;
        private readonly ILogger<ImageMetadataController> _logger;

        public ImageMetadataController(
            IImageUploadService iImageUploadService, 
            FileExtensionContentTypeProvider contentTypeProvider,
            ILogger<ImageMetadataController> logger)
        {
            _iImageUploadService = iImageUploadService;
            _contentTypeProvider = contentTypeProvider;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(
            IFormFile file,
            [FromForm] int durationMinutes)
        {
            _logger.LogInformation("Received file upload request.");

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            if (durationMinutes <= 0)
            {
                return BadRequest("Invalid duration.");
            }

            try
            {
                _logger.LogInformation("Uploading file: {FileName}", file.FileName);
                var expirationDuration = TimeSpan.FromMinutes(durationMinutes);
                using var stream = file.OpenReadStream();
                var uniqueLinkId = await _iImageUploadService.UploadImageAsync(stream, file.FileName, expirationDuration);

                var imageUrl = Url.Action("GetImage", "ImageMetadata", new { uniqueLinkId }, Request.Scheme);

                return Ok(new { Link = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{uniqueLinkId:guid}")]
        public async Task<IActionResult> GetImage(Guid uniqueLinkId)
        {
            try
            {
                var imageMetadata = await _iImageUploadService.GetImageMetadataAsync(uniqueLinkId);
                
                if (imageMetadata == null)
                {
                    return NotFound("Image not found.");
                }

                string contentType;
                if (!_contentTypeProvider.TryGetContentType(imageMetadata.OriginalFileName, out contentType))
                {
                    contentType = "application/octet-stream"; // Default content type
                }

                Response.ContentType = contentType;

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{imageMetadata.OriginalFileName}\"";

                await _iImageUploadService.GetImageStreamAsync(uniqueLinkId, Response.BodyWriter.AsStream());

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
