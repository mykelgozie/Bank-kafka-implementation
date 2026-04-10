using Bank.Application.Interface;
using Bank.Domain.Entities;
using Bank.Infrastructure.Persistence;
using Bank.Infrastructure.Repository;

namespace Bank.Application.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        private AppDbContext _appDbContext;

        public TransactionRepository(AppDbContext appDbContext) :base(appDbContext)
        {
            _appDbContext = appDbContext;
        }
    }
}
