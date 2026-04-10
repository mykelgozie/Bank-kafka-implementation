using Bank.Application.Interface;
using Bank.Infrastructure.Persistence;

namespace Bank.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private AppDbContext _appDbContext;

        public ITransactionRepository TransactionRepository { get; }

        public UnitOfWork(AppDbContext appDbContext, ITransactionRepository transactionRepository)
        {
            _appDbContext = appDbContext;
            TransactionRepository = transactionRepository;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _appDbContext.Dispose();
        }

        public async Task RollBackAsync()
        {
            foreach (var entry in _appDbContext.ChangeTracker.Entries())
            {
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }

            await Task.CompletedTask;
        }
    }
}
