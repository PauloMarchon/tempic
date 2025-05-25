
using Microsoft.EntityFrameworkCore;
using Tempic.Data;
using Tempic.Models;

namespace Tempic.Repository
{
    public class ShortenedUrlRepository : IShortenedUrlRepository, IDisposable
    {
        private AppDbContext context;

        public ShortenedUrlRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<ShortenedUrl> GetShortenedUrlByShortCodeAsync(string shortCode)
        {
            return await context.ShortenedUrls
                .Include(x => x.ImageMetadata)
                .FirstOrDefaultAsync(x => x.ShortCode == shortCode);
        }

        public async Task InsertShortenedUrlAsync(ShortenedUrl shortenedUrl)
        {
            await context.ShortenedUrls.AddAsync(shortenedUrl);
        }

        public async Task DeleteShortenedUrlAsync(Guid imageUniqueLinkId)
        {
            var entity = await context.ShortenedUrls
            .Include(x => x.ImageMetadata)
            .FirstOrDefaultAsync(x => x.ImageUniqueLinkId == imageUniqueLinkId);
            
            if (entity != null)
            {
                context.ShortenedUrls.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
