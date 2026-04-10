using Bank.Application.Interface;
using Bank.Domain.Dtos.Request;
using Bank.Domain.Dtos.Response;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Bank.Application.Service
{
    public class ProcessWebHookService : IProcessWebHookService
    {
        private ITransactionService _transactionService;
        private ILogger<ProcessWebHookService> _logger;
        private IKafkaProducer _kafkaProducer;

        public ProcessWebHookService(ITransactionService transactionService, ILogger<ProcessWebHookService> logger, IKafkaProducer kafkaProducer)
        {
            _transactionService = transactionService;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
        }


        public async Task<ApiResponse<string>> ProcessTransactionWebhook(TransactionWebHookRequest transactionWebHookRequest)
        {
            try
            {

                _logger.LogInformation("Processing webhook with Payload: {payload}", transactionWebHookRequest.TransactionPayload);

                var transactionData = JsonSerializer.Deserialize<TransactionRequest>(transactionWebHookRequest.TransactionPayload);
                transactionData.Payload = transactionWebHookRequest.TransactionPayload;


                var validateResponse = await _transactionService.ValidateTransaction(transactionData);
                if (!validateResponse.Success)
                {
                    _logger.LogError("Validation failed for webhook with transaction Id {transactionId}", transactionData.TransactionId);
                    return ApiResponse<string>.Fail(validateResponse.Message, validateResponse.Errors);
                }

                var transationResponse = await _transactionService.CreateTransaction(transactionData);
                if (!transationResponse.Success)
                {
                    _logger.LogError("Failed to process webhook: {message}", transationResponse.Message);
                    return ApiResponse<string>.Fail(transationResponse.Message, transationResponse.Errors);
                }


                var processResponse = await _transactionService.ProcessTransaction(transationResponse.Data);
                if (!processResponse.Success)
                {
                    _logger.LogError("Failed to process transaction: {message}", processResponse.Message);
                    return ApiResponse<string>.Fail(processResponse.Message, processResponse.Errors);
                }

                _logger.LogInformation("Successfully processed webhook for transaction Id {transactionId}", transactionData.TransactionId);
                return ApiResponse<string>.Ok("Webhook processed successfully");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An error occurred while processing the webhook");
                return ApiResponse<string>.Fail("An error occurred while processing the webhook: " + ex.Message);
            }
        }

        public async Task<ApiResponse<string>> ProcessTransactionEvent(TransactionEvent transactionEvent)
        {
            try
            {
                var tranResponse = await _transactionService.GetTransactionByTransactionId(transactionEvent.TransactionId);
                if (!tranResponse.Success)
                {
                    return ApiResponse<string>.Fail(tranResponse.Message);
                }


                var processResponse = await _transactionService.ProcessTransaction(tranResponse.Data);
                if (!processResponse.Success)
                {
                    _logger.LogError("Failed to process transaction: {message}", processResponse.Message);
                    return ApiResponse<string>.Fail(processResponse.Message, processResponse.Errors);
                }

                _logger.LogInformation("Successfully processed webhook for transaction Id {transactionId}", transactionEvent.TransactionId);
                return ApiResponse<string>.Ok("Webhook processed successfully");

            }
            catch (Exception ex)
            {

                _logger.LogError("Failed to process transaction: {message}", ex.Message);
                return ApiResponse<string>.Fail("Transaction failed");
            }
        }


        public async Task<ApiResponse<string>> ProcessTransactionWebhookV2(TransactionWebHookRequest transactionWebHookRequest)
        {
            try
            {
                string json = "{\"TransactionId\":\"TXN12345\",\"Amount\":1500.75,\"Fee\":25.50,\"CurrencyCode\":\"NGN\",\"Status\":1}";
                transactionWebHookRequest.TransactionPayload = json;
       

                _logger.LogInformation("Processing webhook with Payload: {payload}", transactionWebHookRequest.TransactionPayload);

                var transactionData = JsonSerializer.Deserialize<TransactionRequest>(transactionWebHookRequest.TransactionPayload);
                transactionData.Payload = transactionWebHookRequest.TransactionPayload;
                transactionData.TransactionId  = Guid.NewGuid().ToString().Substring(0, 8);

                var validateResponse = await _transactionService.ValidateTransaction(transactionData);
                if (!validateResponse.Success)
                {
                    _logger.LogError("Validation failed for webhook with transaction Id {transactionId}", transactionData.TransactionId);
                    return ApiResponse<string>.Fail(validateResponse.Message, validateResponse.Errors);
                }

                var transationResponse = await _transactionService.CreateTransaction(transactionData);
                if (!transationResponse.Success)
                {
                    _logger.LogError("Failed to process webhook: {message}", transationResponse.Message);
                    return ApiResponse<string>.Fail(transationResponse.Message, transationResponse.Errors);
                }

                await _kafkaProducer.SendAsync("tran-topic", JsonSerializer.Serialize(new TransactionEvent { TransactionId = transactionData.TransactionId }));

                _logger.LogInformation("processing transaction {transactionId}", transactionData.TransactionId);
                return ApiResponse<string>.Ok("procesing transaction");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the webhook");
                return ApiResponse<string>.Fail("An error occurred while processing the webhook: " + ex.Message);
            }
        }
    }
}
