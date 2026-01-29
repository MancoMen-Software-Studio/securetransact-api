using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.Commands.ProcessTransaction;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using Xunit;

namespace SecureTransact.Application.Tests.Commands.ProcessTransaction;

public sealed class ProcessTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProcessTransactionCommandHandler _handler;

    public ProcessTransactionCommandHandlerTests()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new ProcessTransactionCommandHandler(_transactionRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithValidCommand()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Reference = "Test"
        };

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");
        result.Value.Amount.Should().Be(100m);
        result.Value.Currency.Should().Be("USD");
        result.Value.AuthorizationCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPersistTransaction()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _transactionRepository.Received(1)
            .AddAsync(Arg.Any<TransactionAggregate>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCurrencyIsInvalid()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "INVALID"
        };

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Currency");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAccountsAreSame()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = accountId,
            DestinationAccountId = accountId,
            Amount = 100m,
            Currency = "USD"
        };

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("SameAccount");
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectAccountIds()
    {
        // Arrange
        Guid sourceId = Guid.NewGuid();
        Guid destinationId = Guid.NewGuid();
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = 100m,
            Currency = "USD"
        };

        // Act
        Result<TransactionResponse> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SourceAccountId.Should().Be(sourceId);
        result.Value.DestinationAccountId.Should().Be(destinationId);
    }
}
