using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tempic.DTOs;
using Tempic.Services;

namespace Tempic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageMetadataController : ControllerBase
    {
        private readonly IImageUploadService _iImageUploadService;
        private readonly ILogger<ImageMetadataController> _logger;

        public ImageMetadataController(
            IImageUploadService iImageUploadService,
            ILogger<ImageMetadataController> logger)
        {
            _iImageUploadService = iImageUploadService;
            _logger = logger;
        }

        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)] // 10 MB limit
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(
            [FromForm] List<UploadImageRequest> request)
        {
  
            _logger.LogInformation("Received file upload request.");
            var links = await _iImageUploadService.UploadImageAsync(request);
            
            var imageUrls = links.Select(link => Url.Action("GetImage", "ImageMetadata", new { link }, Request.Scheme)).ToList();

            UploadImageResponse uploadImageResponse = new UploadImageResponse
            {
                Links = imageUrls
            };

            return Ok(uploadImageResponse);          
        }

        [HttpGet("{uniqueLinkId:guid}")]
        public async Task<IActionResult> GetImage(Guid uniqueLinkId)
        {
            try
            {  
                Response.ContentType = "application/octet-stream";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{uniqueLinkId.ToString()}\"";

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
