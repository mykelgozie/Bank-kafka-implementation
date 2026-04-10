using Bank.Application.Interface;
using Bank.Domain.Dtos.Request;
using Bank.Domain.Dtos.Response;
using Bank.Domain.Entities;
using Bank.Domain.Enum;
using Microsoft.Extensions.Logging;


namespace Bank.Application.Service
{
    public class TransactionService : ITransactionService
    {
        private IUnitOfWork _unitOfWork;
        private ILogger<TransactionService> _logger;

        public TransactionService(IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<Transaction>> ValidateTransaction(TransactionRequest transactionRequest)
        {
            try
            {
                var transaction = await _unitOfWork.TransactionRepository.GetFirstOrDefaultAsync(x => x.TransactionId == transactionRequest.TransactionId);
                if (transaction != null)
                {
                    return ApiResponse<Transaction>.Fail("Transaction Exist");
                }

                return ApiResponse<Transaction>.Ok(null, "Transaction Validated Successfully");
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while validating the transaction with ID: {TransactionId}", transactionRequest.TransactionId);
                return ApiResponse<Transaction>.Fail("An error occurred while validating the transaction");
            }
        }


        public async Task<ApiResponse<Transaction>> CreateTransaction(TransactionRequest transactionRequest)
        {
            try
            {
                var transaction = new Transaction
                {
                    TransactionId = transactionRequest.TransactionId,
                    Amount = transactionRequest.Amount,
                    Reference = GenerateSecureReference(),
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Initiated,
                    CurrencyCode = transactionRequest.CurrencyCode,
                    Payload = transactionRequest.Payload
                };
                await _unitOfWork.TransactionRepository.AddAsync(transaction);
                await _unitOfWork.SaveAsync();
                return ApiResponse<Transaction>.Ok(transaction, "Transaction Created Successfully");
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
                _logger.LogError("An error occurred while creating the transaction with ID: {TransactionId}", transactionRequest.TransactionId);
                return ApiResponse<Transaction>.Fail("An error occurred while creating the transaction");
            }
        }

        private string GenerateSecureReference()
        {
            return $"TXN-{Guid.NewGuid().ToString("N").ToUpper()}";
        }


        public async Task<ApiResponse<string>> ProcessTransaction(Transaction transaction)
        {
            try
            {
                _ = await ApplyFee(transaction);
                return ApiResponse<string>.Ok("Transaction Processed Successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the transaction webhook");
                return ApiResponse<string>.Fail("An error occurred while processing the transaction webhook");
            }
        }

        public async Task<ApiResponse<Transaction>> GetTransactionByTransactionId(string transactionId)
        {
            try
            {
                var transaction = await _unitOfWork.TransactionRepository.GetFirstOrDefaultAsync(x => x.TransactionId == transactionId);
                if (transaction == null)
                {
                    return ApiResponse<Transaction>.Fail("Transaction Not Found");
                }

                return ApiResponse<Transaction>.Ok(transaction);

            }
            catch (Exception)
            {

                return ApiResponse<Transaction>.Fail("Invalid");
            }
        }


        public async Task<ApiResponse<Transaction>> ApplyFee(Transaction transaction)
        {
            try
            {

                transaction.Fee = transaction.Amount * 0.02m;
                transaction.NetAmount = transaction.Amount - transaction.Fee;
                transaction.Status = TransactionStatus.Completed;
                _unitOfWork.TransactionRepository.Update(transaction);
                await _unitOfWork.SaveAsync();
                return ApiResponse<Transaction>.Ok(transaction, "Fee Applied Successfully");

            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
                _logger.LogError("An error occurred while applying fee for transaction ID: {TransactionId}", transaction.TransactionId);
                return ApiResponse<Transaction>.Fail("An error occurred while applying fee");
            }
        }
    }
}
