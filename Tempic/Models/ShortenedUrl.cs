namespace Tempic.Models
{
    public class ShortenedUrl
    {
        public int Id { get; set; }
        public string ShortCode { get; set; }
        public Guid ImageUniqueLinkId { get; set; }
        public DateTime CreationDateUtc { get; set; }
        public DateTime? ExpirationDateUtc { get; set; }
    }
}
