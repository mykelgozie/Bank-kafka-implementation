using System;
using System.Text.Json;
using System.Threading.Tasks;
using Bank.Application.Interface;
using Bank.Application.Service;
using Bank.Domain.Dtos.Request;
using Bank.Domain.Entities;
using Bank.Domain.Dtos.Response;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bank.Application.Tests
{
    public class ProcessWebHookServiceTests
    {
        private readonly Mock<ITransactionService> _transactionServiceMock;
        private readonly Mock<IKafkaProducer> _kafkaProducerMock;
        private readonly Mock<ILogger<ProcessWebHookService>> _loggerMock;
        private readonly ProcessWebHookService _service;

        public ProcessWebHookServiceTests()
        {
            _transactionServiceMock = new Mock<ITransactionService>();
            _kafkaProducerMock = new Mock<IKafkaProducer>();
            _loggerMock = new Mock<ILogger<ProcessWebHookService>>();

            _service = new ProcessWebHookService(
                _transactionServiceMock.Object,
                _loggerMock.Object,
                _kafkaProducerMock.Object);
        }

        #region ProcessTransactionWebhook

        [Fact]
        public async Task ProcessTransactionWebhook_SuccessFlow_ReturnsOk()
        {
            // arrange
            var requestDto = new TransactionRequest
            {
                TransactionId = "TXN123",
                Amount = 100,
                Fee = 1,
                CurrencyCode = "NGN",
                Status = 1
            };

            var payload = JsonSerializer.Serialize(requestDto);

            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            var transactionEntity = new Transaction { TransactionId = requestDto.TransactionId };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transactionEntity));

            _transactionServiceMock
                .Setup(x => x.CreateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transactionEntity));

            _transactionServiceMock
                .Setup(x => x.ProcessTransaction(It.IsAny<Transaction>()))
                .ReturnsAsync(ApiResponse<string>.Ok("processed"));

            // act
            var result = await _service.ProcessTransactionWebhook(webhookRequest);

            // assert
            Assert.True(result.Success);
            Assert.Equal("Webhook processed successfully", result.Data);
        }

        [Fact]
        public async Task ProcessTransactionWebhook_ValidateFails_ReturnsFail()
        {
            // arrange
            var requestDto = new TransactionRequest { TransactionId = "TXN123" };
            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Fail("invalid", new System.Collections.Generic.List<string> { "err1" }));

            // act
            var result = await _service.ProcessTransactionWebhook(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.Equal("invalid", result.Message);
            Assert.Contains("err1", result.Errors);
        }

        [Fact]
        public async Task ProcessTransactionWebhook_CreateTransactionFails_ReturnsFail()
        {
            // arrange
            var requestDto = new TransactionRequest { TransactionId = "TXN123" };
            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(new Transaction()));

            _transactionServiceMock
                .Setup(x => x.CreateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Fail("create failed"));

            // act
            var result = await _service.ProcessTransactionWebhook(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.Equal("create failed", result.Message);
        }

        [Fact]
        public async Task ProcessTransactionWebhook_ProcessTransactionFails_ReturnsFail()
        {
            // arrange
            var requestDto = new TransactionRequest { TransactionId = "TXN123" };
            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(new Transaction()));

            _transactionServiceMock
                .Setup(x => x.CreateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(new Transaction()));

            _transactionServiceMock
                .Setup(x => x.ProcessTransaction(It.IsAny<Transaction>()))
                .ReturnsAsync(ApiResponse<string>.Fail("process failed", new System.Collections.Generic.List<string> { "err" }));

            // act
            var result = await _service.ProcessTransactionWebhook(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.Equal("process failed", result.Message);
            Assert.Contains("err", result.Errors);
        }

        [Fact]
        public async Task ProcessTransactionWebhook_NullPayload_ThrowsCaughtAndReturnsFail()
        {
            // arrange: null payload will cause Deserialize to return null and access to throw
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = null };

            // act
            var result = await _service.ProcessTransactionWebhook(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.StartsWith("An error occurred while processing the webhook", result.Message);
        }

        #endregion

        #region ProcessTransactionEvent

        [Fact]
        public async Task ProcessTransactionEvent_SuccessFlow_ReturnsOk()
        {
            // arrange
            var transactionId = "TXN-1";
            var transaction = new Transaction { TransactionId = transactionId };

            _transactionServiceMock
                .Setup(x => x.GetTransactionByTransactionId(transactionId))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transaction));

            _transactionServiceMock
                .Setup(x => x.ProcessTransaction(transaction))
                .ReturnsAsync(ApiResponse<string>.Ok("processed"));

            var evt = new TransactionEvent { TransactionId = transactionId };

            // act
            var result = await _service.ProcessTransactionEvent(evt);

            // assert
            Assert.True(result.Success);
            Assert.Equal("Webhook processed successfully", result.Data);
        }

        [Fact]
        public async Task ProcessTransactionEvent_GetTransactionFails_ReturnsFail()
        {
            // arrange
            var transactionId = "TXN-1";
            _transactionServiceMock
                .Setup(x => x.GetTransactionByTransactionId(transactionId))
                .ReturnsAsync(ApiResponse<Transaction>.Fail("not found"));

            var evt = new TransactionEvent { TransactionId = transactionId };

            // act
            var result = await _service.ProcessTransactionEvent(evt);

            // assert
            Assert.False(result.Success);
            Assert.Equal("not found", result.Message);
        }

        [Fact]
        public async Task ProcessTransactionEvent_ProcessTransactionFails_ReturnsFail()
        {
            // arrange
            var transactionId = "TXN-1";
            var transaction = new Transaction { TransactionId = transactionId };

            _transactionServiceMock
                .Setup(x => x.GetTransactionByTransactionId(transactionId))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transaction));

            _transactionServiceMock
                .Setup(x => x.ProcessTransaction(transaction))
                .ReturnsAsync(ApiResponse<string>.Fail("process failed"));

            var evt = new TransactionEvent { TransactionId = transactionId };

            // act
            var result = await _service.ProcessTransactionEvent(evt);

            // assert
            Assert.False(result.Success);
            Assert.Equal("process failed", result.Message);
        }

        [Fact]
        public async Task ProcessTransactionEvent_GetTransactionThrows_ReturnsTransactionFailed()
        {
            // arrange
            var transactionId = "TXN-1";
            _transactionServiceMock
                .Setup(x => x.GetTransactionByTransactionId(transactionId))
                .ThrowsAsync(new Exception("boom"));

            var evt = new TransactionEvent { TransactionId = transactionId };

            // act
            var result = await _service.ProcessTransactionEvent(evt);

            // assert
            Assert.False(result.Success);
            Assert.Equal("Transaction failed", result.Message);
        }

        #endregion

        #region ProcessTransactionWebhookV2

        [Fact]
        public async Task ProcessTransactionWebhookV2_SuccessFlow_SendsToKafkaAndReturnsOk()
        {
            // arrange
            var requestDto = new TransactionRequest
            {
                TransactionId = "TXN321",
                Amount = 200,
                Fee = 2,
                CurrencyCode = "NGN",
                Status = 1
            };

            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            var transactionEntity = new Transaction { TransactionId = requestDto.TransactionId };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transactionEntity));

            _transactionServiceMock
                .Setup(x => x.CreateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(transactionEntity));

            _kafkaProducerMock
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // act
            var result = await _service.ProcessTransactionWebhookV2(webhookRequest);

            // assert
            Assert.True(result.Success);
            Assert.Equal("procesing transaction", result.Data);

            _kafkaProducerMock.Verify(x =>
                x.SendAsync("tran-topic", It.Is<string>(s => s.Contains(requestDto.TransactionId))),
                Times.Once);
        }

        [Fact]
        public async Task ProcessTransactionWebhookV2_ValidateFails_ReturnsFail()
        {
            // arrange
            var requestDto = new TransactionRequest { TransactionId = "TXN321" };
            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Fail("invalid"));

            // act
            var result = await _service.ProcessTransactionWebhookV2(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.Equal("invalid", result.Message);
        }

        [Fact]
        public async Task ProcessTransactionWebhookV2_CreateTransactionFails_ReturnsFail()
        {
            // arrange
            var requestDto = new TransactionRequest { TransactionId = "TXN321" };
            var payload = JsonSerializer.Serialize(requestDto);
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = payload };

            _transactionServiceMock
                .Setup(x => x.ValidateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Ok(new Transaction()));

            _transactionServiceMock
                .Setup(x => x.CreateTransaction(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(ApiResponse<Transaction>.Fail("create failed"));

            // act
            var result = await _service.ProcessTransactionWebhookV2(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.Equal("create failed", result.Message);
        }

        [Fact]
        public async Task ProcessTransactionWebhookV2_NullPayload_ThrowsCaughtAndReturnsFail()
        {
            // arrange
            var webhookRequest = new TransactionWebHookRequest { TransactionPayload = null };

            // act
            var result = await _service.ProcessTransactionWebhookV2(webhookRequest);

            // assert
            Assert.False(result.Success);
            Assert.StartsWith("An error occurred while processing the webhook", result.Message);
        }

        #endregion
    }
}