using Microsoft.EntityFrameworkCore;
using Tempic.Models;

namespace Tempic.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<ImageMetadata> ImageMetadatas { get; set; }
        public DbSet<ShortenedUrl> ShortenedUrls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortenedUrl>(entity =>
            {
                entity.HasIndex(e => e.ShortCode)
                      .IsUnique();

                entity.HasOne<ImageMetadata>()
                .WithOne()
                .HasForeignKey<ShortenedUrl>(s => s.ImageUniqueLinkId)
                .HasPrincipalKey<ImageMetadata>(i => i.UniqueLinkId);
            });
                
            modelBuilder.Entity<ImageMetadata>()
                .HasIndex(i => i.UniqueLinkId)
                .IsUnique();
        }
    }
}
