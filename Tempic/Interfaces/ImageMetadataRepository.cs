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

        public ImageMetadata GetImageMetadataByUniqueLinkId(Guid uniqueLinkId)
        {
            return context.ImageMetadata.FirstOrDefault(x => x.UniqueLinkId == uniqueLinkId) ?? throw new Exception("Image not found");
        }
        public void InsertImageMetadata(ImageMetadata imageMetadata)
        {
            context.ImageMetadata.Add(imageMetadata);
        }
        public void DeleteImageMetadata(Guid uniqueLinkId)
        {
            context.ImageMetadata.Remove(context.ImageMetadata.FirstOrDefault(x => x.UniqueLinkId == uniqueLinkId) ?? throw new Exception("Image not found")
);
        }
        public void SaveChanges()
        {
            context.SaveChanges();
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
