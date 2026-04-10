using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bank.Application.Interface;
using Bank.Application.Service;
using Bank.Domain.Dtos.Request;
using Bank.Domain.Dtos.Response;
using Bank.Domain.Entities;
using Bank.Domain.Enum;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bank.Application.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ILogger<TransactionService>> _loggerMock;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _loggerMock = new Mock<ILogger<TransactionService>>();

            _unitOfWorkMock
                .SetupGet(u => u.TransactionRepository)
                .Returns(_transactionRepoMock.Object);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.RollBackAsync())
                .Returns(Task.CompletedTask);

            _service = new TransactionService(_unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ValidateTransaction_ReturnsFail_WhenTransactionExists()
        {
            // arrange
            var request = new TransactionRequest { TransactionId = "TXN1" };
            var existing = new Transaction { TransactionId = "TXN1" };

            _transactionRepoMock
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(existing);

            // act
            var result = await _service.ValidateTransaction(request);

            // assert
            Assert.False(result.Success);
            Assert.Equal("Transaction Exist", result.Message);
        }

        [Fact]
        public async Task ValidateTransaction_ReturnsOk_WhenTransactionDoesNotExist()
        {
            // arrange
            var request = new TransactionRequest { TransactionId = "TXN2" };

            _transactionRepoMock
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync((Transaction)null);

            // act
            var result = await _service.ValidateTransaction(request);

            // assert
            Assert.True(result.Success);
            Assert.Equal("Transaction Validated Successfully", result.Message);
        }

        [Fact]
        public async Task ValidateTransaction_Exception_ReturnsFail()
        {
            // arrange
            var request = new TransactionRequest { TransactionId = "TXN3" };

            _transactionRepoMock
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ThrowsAsync(new Exception("db error"));

            // act
            var result = await _service.ValidateTransaction(request);

            // assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred while validating the transaction", result.Message);
        }

        [Fact]
        public async Task CreateTransaction_Success_AddsAndSavesTransaction()
        {
            // arrange
            var request = new TransactionRequest
            {
                TransactionId = "TXN-CREATE",
                Amount = 100m,
                CurrencyCode = "USD",
                Payload = "{}"
            };

            // act
            var result = await _service.CreateTransaction(request);

            // assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(request.TransactionId, result.Data.TransactionId);
            Assert.Equal(request.Amount, result.Data.Amount);
            Assert.Equal(request.CurrencyCode, result.Data.CurrencyCode);
            Assert.Equal(TransactionStatus.Initiated, result.Data.Status);
            Assert.StartsWith("TXN-", result.Data.Reference);
            _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateTransaction_OnException_RollsBackAndReturnsFail()
        {
            // arrange
            var request = new TransactionRequest { TransactionId = "TXN-ERR", Amount = 50m };

            _transactionRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ThrowsAsync(new Exception("add failed"));

            // act
            var result = await _service.CreateTransaction(request);

            // assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred while creating the transaction", result.Message);
            _unitOfWorkMock.Verify(u => u.RollBackAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTransactionByTransactionId_ReturnsFail_WhenNotFound()
        {
            // arrange
            var txId = "NOTFOUND";
            _transactionRepoMock
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync((Transaction)null);

            // act
            var result = await _service.GetTransactionByTransactionId(txId);

            // assert
            Assert.False(result.Success);
            Assert.Equal("Transaction Not Found", result.Message);
        }

        [Fact]
        public async Task GetTransactionByTransactionId_ReturnsOk_WhenFound()
        {
            // arrange
            var txId = "FOUND";
            var tx = new Transaction { TransactionId = txId };
            _transactionRepoMock
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(tx);

            // act
            var result = await _service.GetTransactionByTransactionId(txId);

            // assert
            Assert.True(result.Success);
            Assert.Equal(tx, result.Data);
        }

        [Fact]
        public async Task ApplyFee_Success_UpdatesTransactionAndSaves()
        {
            // arrange
            var tx = new Transaction { TransactionId = "TXN-FEE", Amount = 200m, Status = TransactionStatus.Processing };

            // act
            var result = await _service.ApplyFee(tx);

            // assert
            Assert.True(result.Success);
            Assert.Equal(TransactionStatus.Completed, result.Data.Status);
            Assert.Equal(tx.Amount * 0.02m, result.Data.Fee);
            Assert.Equal(tx.Amount - result.Data.Fee, result.Data.NetAmount);
            _transactionRepoMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyFee_OnException_RollsBackAndReturnsFail()
        {
            // arrange
            var tx = new Transaction { TransactionId = "TXN-FEE-ERR", Amount = 100m };

            _transactionRepoMock
                .Setup(r => r.Update(It.IsAny<Transaction>()))
                .Throws(new Exception("update failed"));

            // act
            var result = await _service.ApplyFee(tx);

            // assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred while applying fee", result.Message);
            _unitOfWorkMock.Verify(u => u.RollBackAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessTransaction_CallsApplyFeeAndReturnsOk()
        {
            // arrange
            var tx = new Transaction { TransactionId = "TXN-PROC", Amount = 150m };

            // Ensure ApplyFee completes successfully by allowing Update and Save to run (no setup needed)
            var result = await _service.ProcessTransaction(tx);

            // assert
            Assert.True(result.Success);
            Assert.Equal("Transaction Processed Successfully", result.Data);
        }
    }
}           