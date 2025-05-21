using System.ComponentModel.DataAnnotations;

namespace Tempic.DTOs
{
    public record UploadImageRequest (
        [Required]
        FormFile File,

        [Required]
        [Range(1,60)]
        int DurationMinutes)
    {

    }
}
