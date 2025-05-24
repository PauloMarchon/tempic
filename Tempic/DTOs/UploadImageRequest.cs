using System.ComponentModel.DataAnnotations;

namespace Tempic.DTOs
{
    public class UploadImageRequest
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        [Range(1, 60)]
        public double DurationMinutes { get; set; }
    }
}
