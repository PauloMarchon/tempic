using Tempic.Repository;

namespace Tempic.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IShortenedUrlRepository ShortenedUrlRepository { get; }
        IImageMetadataRepository ImageMetadataRepository { get; }
        
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();

        Task<int> SaveChangesAsync();
    }
}
