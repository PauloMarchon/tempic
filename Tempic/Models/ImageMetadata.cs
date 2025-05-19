namespace Tempic.Models
{
    public class ImageMetadata
    {
        public int Id { get; set; }
        public Guid UniqueLinkId { get; set; }
        public string OriginalFileName { get; set; }
        public string MinioBucketName { get; set; }
        public string MinioObjectName { get; set; }
        public DateTime ExpirationDateUtc { get; set; }
        public DateTime UploadDateUtc { get; set; }
    }
}
