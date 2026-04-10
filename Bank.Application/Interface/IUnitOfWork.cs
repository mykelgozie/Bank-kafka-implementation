

namespace Bank.Application.Interface
{
    public interface IUnitOfWork
    {
        ITransactionRepository TransactionRepository { get; }
        void Dispose();
        Task RollBackAsync();
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
