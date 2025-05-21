using Microsoft.EntityFrameworkCore;
using Tempic.Models;

namespace Tempic.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<ImageMetadata> ImageMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           modelBuilder.Entity<ImageMetadata>()
                .HasIndex(i => i.UniqueLinkId)
                .IsUnique();
        }
    }
}
