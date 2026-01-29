using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Application.Queries.GetTransactionById;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Application.Tests.Queries.GetTransactionById;

public sealed class GetTransactionByIdQueryHandlerTests
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly GetTransactionByIdQueryHandler _handler;

    public GetTransactionByIdQueryHandlerTests()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _handler = new GetTransactionByIdQueryHandler(_transactionRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnTransaction_WhenTransactionExists()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        GetTransactionByIdQuery query = new() { TransactionId = transaction.Id.Value };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TransactionId.Should().Be(transaction.Id.Value);
        result.Value.SourceAccountId.Should().Be(transaction.SourceAccountId.Value);
        result.Value.DestinationAccountId.Should().Be(transaction.DestinationAccountId.Value);
        result.Value.Amount.Should().Be(100m);
        result.Value.Currency.Should().Be("USD");
        result.Value.Status.Should().Be("Initiated");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTransactionNotFound()
    {
        // Arrange
        Guid transactionId = Guid.NewGuid();
        GetTransactionByIdQuery query = new() { TransactionId = transactionId };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns((TransactionAggregate?)null);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly_WhenTransactionIsCompleted()
    {
        // Arrange
        TransactionAggregate transaction = CreateCompletedTransaction();
        GetTransactionByIdQuery query = new() { TransactionId = transaction.Id.Value };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");
        result.Value.AuthorizationCode.Should().Be("AUTH123");
        result.Value.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_WithCorrectTransactionId()
    {
        // Arrange
        Guid transactionId = Guid.NewGuid();
        GetTransactionByIdQuery query = new() { TransactionId = transactionId };

        _transactionRepository
            .GetByIdAsync(Arg.Any<TransactionId>(), Arg.Any<CancellationToken>())
            .Returns((TransactionAggregate?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _transactionRepository.Received(1).GetByIdAsync(
            Arg.Is<TransactionId>(id => id.Value == transactionId),
            Arg.Any<CancellationToken>());
    }

    private static TransactionAggregate CreateTransaction()
    {
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            AccountId.New(),
            AccountId.New(),
            Money.Create(100m, Currency.USD).Value);

        return result.Value;
    }

    private static TransactionAggregate CreateCompletedTransaction()
    {
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");
        transaction.Complete();
        return transaction;
    }
}
