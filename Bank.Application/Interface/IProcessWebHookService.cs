using Bank.Domain.Dtos.Request;
using Bank.Domain.Dtos.Response;

namespace Bank.Application.Interface
{
    public interface IProcessWebHookService
    {
        Task<ApiResponse<string>> ProcessTransactionEvent(TransactionEvent transactionEvent);
        Task<ApiResponse<string>> ProcessTransactionWebhook(TransactionWebHookRequest transactionWebHookRequest);
        Task<ApiResponse<string>> ProcessTransactionWebhookV2(TransactionWebHookRequest transactionWebHookRequest);
    }
}
