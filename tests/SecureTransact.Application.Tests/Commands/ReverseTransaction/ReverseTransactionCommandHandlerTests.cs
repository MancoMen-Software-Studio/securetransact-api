using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.Commands.ReverseTransaction;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Application.Tests.Commands.ReverseTransaction;

public sealed class ReverseTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReverseTransactionCommandHandler _handler;

    public ReverseTransactionCommandHandlerTests()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new ReverseTransactionCommandHandler(_transactionRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTransactionCanBeReversed()
    {
        // Arrange
        TransactionAggregate transaction = CreateCompletedTransaction();
        ReverseTransactionCommand command = new()
        {
            TransactionId = transaction.Id.Value,
            Reason = "Customer request"
        };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Reversed");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTransactionNotFound()
    {
        // Arrange
        ReverseTransactionCommand command = new()
        {
            TransactionId = Guid.NewGuid(),
            Reason = "Customer request"
        };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns((TransactionAggregate?)null);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTransactionCannotBeReversed()
    {
        // Arrange
        TransactionAggregate transaction = CreateInitiatedTransaction();
        ReverseTransactionCommand command = new()
        {
            TransactionId = transaction.Id.Value,
            Reason = "Customer request"
        };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        TransactionAggregate transaction = CreateCompletedTransaction();
        ReverseTransactionCommand command = new()
        {
            TransactionId = transaction.Id.Value,
            Reason = "Customer request"
        };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _transactionRepository.Received(1).Update(transaction);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static TransactionAggregate CreateInitiatedTransaction()
    {
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            AccountId.New(),
            AccountId.New(),
            Money.Create(100m, Currency.USD).Value);

        return result.Value;
    }

    private static TransactionAggregate CreateCompletedTransaction()
    {
        TransactionAggregate transaction = CreateInitiatedTransaction();
        transaction.Authorize("AUTH123");
        transaction.Complete();
        return transaction;
    }
}
