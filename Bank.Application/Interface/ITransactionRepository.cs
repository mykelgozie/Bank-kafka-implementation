using Bank.Domain.Entities;
using Bank.Infrastructure.Repository;

namespace Bank.Application.Interface
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
    }
}
