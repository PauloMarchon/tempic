using Microsoft.EntityFrameworkCore;
using Tempic.Data;
using Tempic.Exceptions;
using Tempic.Models;

namespace Tempic.Interfaces
{
    public class ImageMetadataRepository : IImageMetadataRepository, IDisposable
    {
        private AppDbContext context;
        public ImageMetadataRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<ImageMetadata> GetImageMetadataByUniqueLinkIdAsync(Guid uniqueLinkId)
        {
            return await context.ImageMetadata.FirstOrDefaultAsync(x => x.UniqueLinkId == uniqueLinkId)
                   ?? throw new ImageNotFoundOrExpiredException("Image not found");
        }
        public async Task InsertImageMetadataAsync(ImageMetadata imageMetadata)
        {
            await context.ImageMetadata.AddAsync(imageMetadata);
        }
        public async Task DeleteImageMetadataAsync(Guid uniqueLinkId)
        {
            var entity = await context.ImageMetadata.FirstOrDefaultAsync(x => x.UniqueLinkId == uniqueLinkId);

            if (entity != null)     
                context.ImageMetadata.Remove(entity);
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
