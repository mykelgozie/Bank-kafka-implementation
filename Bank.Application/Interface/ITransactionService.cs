using Bank.Domain.Dtos.Request;
using Bank.Domain.Dtos.Response;
using Bank.Domain.Entities;

namespace Bank.Application.Interface
{
    public interface ITransactionService
    {
        Task<ApiResponse<Transaction>> CreateTransaction(TransactionRequest transactionRequest);
        Task<ApiResponse<Transaction>> GetTransactionByTransactionId(string transactionId);
        Task<ApiResponse<string>> ProcessTransaction(Transaction transaction);
        Task<ApiResponse<Transaction>> ValidateTransaction(TransactionRequest transactionRequest);
    }
}
