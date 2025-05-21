using FluentValidation;
using Tempic.DTOs;

namespace Tempic.Validator
{
    public class UploadImageRequestValidator : AbstractValidator<UploadImageRequest>
    {
        private readonly string[] _allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public UploadImageRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("Image is required.")
                .Must(BeAValidImage)
                .WithMessage("Invalid image format. Allowed formats are: jpg, jpeg, png, gif, bmp, webp.")
                .Must(BeAValidExtension)
                .WithMessage("Invalid file extension. Allowed extensions are: jpg, jpeg, png, gif, bmp, webp.")
                .Must(x => x.Length <= MaxFileSizeBytes);

            RuleFor(x => x.DurationMinutes)
                .NotNull()
                .WithMessage("Duration is required.")
                .GreaterThan(0)
                .WithMessage("Duration must be greater than 1 minute.");
        }
        private bool BeAValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (string.IsNullOrWhiteSpace(file.FileName))
                return false;

            return true;
        }

        private bool BeAValidExtension(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }
    }
}
