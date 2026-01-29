using System;
using System.Linq;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.Errors;
using SecureTransact.Domain.Events;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.Aggregates;

public sealed class TransactionAggregateTests
{
    private static Money CreateMoney(decimal amount = 100m) =>
        Money.Create(amount, Currency.USD).Value;

    [Fact]
    public void Create_ShouldSucceed_WithValidParameters()
    {
        // Arrange
        AccountId source = AccountId.New();
        AccountId destination = AccountId.New();
        Money amount = CreateMoney(100m);

        // Act
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            source, destination, amount, "Test reference");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SourceAccountId.Should().Be(source);
        result.Value.DestinationAccountId.Should().Be(destination);
        result.Value.Amount.Should().Be(amount);
        result.Value.Reference.Should().Be("Test reference");
        result.Value.Status.Should().Be(TransactionStatus.Initiated);
    }

    [Fact]
    public void Create_ShouldRaiseTransactionInitiatedEvent()
    {
        // Arrange
        AccountId source = AccountId.New();
        AccountId destination = AccountId.New();
        Money amount = CreateMoney();

        // Act
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            source, destination, amount);

        // Assert
        result.Value.DomainEvents.Should().HaveCount(1);
        result.Value.DomainEvents.First().Should().BeOfType<TransactionInitiatedEvent>();
    }

    [Fact]
    public void Create_ShouldFail_WhenSourceAndDestinationAreSame()
    {
        // Arrange
        AccountId account = AccountId.New();
        Money amount = CreateMoney();

        // Act
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            account, account, amount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TransactionErrors.SameAccount);
    }

    [Fact]
    public void Create_ShouldFail_WhenAmountIsZero()
    {
        // Arrange
        AccountId source = AccountId.New();
        AccountId destination = AccountId.New();
        Money amount = Money.Zero(Currency.USD);

        // Act
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            source, destination, amount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TransactionErrors.InvalidAmount);
    }

    [Fact]
    public void Authorize_ShouldSucceed_WhenInitiated()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();

        // Act
        Result result = transaction.Authorize("AUTH123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Authorized);
        transaction.AuthorizationCode.Should().Be("AUTH123");
    }

    [Fact]
    public void Authorize_ShouldRaiseTransactionAuthorizedEvent()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.Authorize("AUTH123");

        // Assert
        transaction.DomainEvents.Should().HaveCount(1);
        transaction.DomainEvents.First().Should().BeOfType<TransactionAuthorizedEvent>();
    }

    [Fact]
    public void Authorize_ShouldFail_WhenAlreadyCompleted()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");
        transaction.Complete();

        // Act
        Result result = transaction.Authorize("AUTH456");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Complete_ShouldSucceed_WhenAuthorized()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");

        // Act
        Result result = transaction.Complete();

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldFail_WhenNotAuthorized()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();

        // Act
        Result result = transaction.Complete();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Fail_ShouldSucceed_WhenInitiated()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();

        // Act
        Result result = transaction.Fail("INSUFFICIENT_FUNDS", "Not enough balance");

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Failed);
        transaction.FailureReason.Should().Be("Not enough balance");
    }

    [Fact]
    public void Fail_ShouldSucceed_WhenAuthorized()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");

        // Act
        Result result = transaction.Fail("TIMEOUT", "Processing timeout");

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public void Reverse_ShouldSucceed_WhenCompleted()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");
        transaction.Complete();

        // Act
        Result result = transaction.Reverse("Customer request");

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Reversed);
    }

    [Fact]
    public void Reverse_ShouldFail_WhenNotCompleted()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");

        // Act
        Result result = transaction.Reverse("Customer request");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Dispute_ShouldSucceed_WhenCompleted()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        transaction.Authorize("AUTH123");
        transaction.Complete();

        // Act
        Result result = transaction.Dispute("Unauthorized transaction");

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Status.Should().Be(TransactionStatus.Disputed);
    }

    [Fact]
    public void LoadFromHistory_ShouldReconstitute_FromEvents()
    {
        // Arrange
        TransactionId transactionId = TransactionId.New();
        AccountId source = AccountId.New();
        AccountId destination = AccountId.New();
        Money amount = CreateMoney(500m);

        IDomainEvent[] events =
        [
            new TransactionInitiatedEvent
            {
                TransactionId = transactionId,
                SourceAccountId = source,
                DestinationAccountId = destination,
                Amount = amount,
                Reference = "Test"
            },
            new TransactionAuthorizedEvent
            {
                TransactionId = transactionId,
                AuthorizationCode = "AUTH999"
            },
            new TransactionCompletedEvent
            {
                TransactionId = transactionId,
                CompletedAtUtc = DateTime.UtcNow
            }
        ];

        // Act
        TransactionAggregate transaction = TransactionAggregate.LoadFromHistory(events);

        // Assert
        transaction.Id.Should().Be(transactionId);
        transaction.SourceAccountId.Should().Be(source);
        transaction.DestinationAccountId.Should().Be(destination);
        transaction.Amount.Should().Be(amount);
        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.AuthorizationCode.Should().Be("AUTH999");
        transaction.Version.Should().Be(2); // 0-indexed, 3 events = version 2
    }

    [Fact]
    public void Apply_ShouldIncrementVersion()
    {
        // Arrange
        TransactionAggregate transaction = TransactionAggregate.LoadFromHistory([]);

        TransactionInitiatedEvent @event = new()
        {
            TransactionId = TransactionId.New(),
            SourceAccountId = AccountId.New(),
            DestinationAccountId = AccountId.New(),
            Amount = CreateMoney()
        };

        // Act
        transaction.Apply(@event);

        // Assert
        transaction.Version.Should().Be(0);
    }

    private static TransactionAggregate CreateTransaction()
    {
        Result<TransactionAggregate> result = TransactionAggregate.Create(
            AccountId.New(),
            AccountId.New(),
            CreateMoney());

        return result.Value;
    }
}
