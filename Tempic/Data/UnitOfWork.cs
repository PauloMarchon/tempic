using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tempic.Repository;

namespace Tempic.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;

        public IImageMetadataRepository ImageMetadataRepository { get; }
        public IShortenedUrlRepository ShortenedUrlRepository { get; }

        public UnitOfWork(AppDbContext context,
                          IImageMetadataRepository imageMetadataRepository,
                          IShortenedUrlRepository shortenedUrlRepository)
        {
            _context = context;
            ImageMetadataRepository = imageMetadataRepository;
            ShortenedUrlRepository = shortenedUrlRepository;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            catch
            {
                await RollbackAsync();
                throw; // Re-throw the exception to be handled by the caller
            }

        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
        

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
