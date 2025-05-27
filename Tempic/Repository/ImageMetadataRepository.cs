using Microsoft.EntityFrameworkCore;
using Tempic.Data;
using Tempic.Exceptions;
using Tempic.Models;

namespace Tempic.Repository
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
            return await context.ImageMetadatas.FirstOrDefaultAsync(x => x.UniqueLinkId == uniqueLinkId);
        }
        public async Task InsertImageMetadataAsync(ImageMetadata imageMetadata)
        {
            await context.ImageMetadatas.AddAsync(imageMetadata);
        }
        public async Task DeleteImageMetadataAsync(Guid uniqueLinkId)
        {
            var entity = await context.ImageMetadatas.FirstOrDefaultAsync(x => x.UniqueLinkId == uniqueLinkId);

            if (entity != null)     
                context.ImageMetadatas.Remove(entity);
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
