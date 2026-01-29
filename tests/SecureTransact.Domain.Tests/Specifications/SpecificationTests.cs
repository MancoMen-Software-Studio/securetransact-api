using System;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.Specifications;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.Specifications;

public sealed class SpecificationTests
{
    private static TransactionAggregate CreateTransaction(
        decimal amount = 1000m,
        string currencyCode = "USD",
        Guid? sourceAccountId = null,
        Guid? destinationAccountId = null)
    {
        AccountId source = AccountId.From(sourceAccountId ?? Guid.NewGuid());
        AccountId destination = AccountId.From(destinationAccountId ?? Guid.NewGuid());
        Currency currency = Currency.FromCode(currencyCode)!;
        Result<Money> money = Money.Create(amount, currency);
        Result<TransactionAggregate> result = TransactionAggregate.Create(source, destination, money.Value);
        return result.Value;
    }

    [Fact]
    public void TransactionAmountSpecification_ShouldBeSatisfied_WhenAmountExceedsThreshold()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 5000m, currencyCode: "USD");
        TransactionAmountSpecification spec = new(1000m, "USD");

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TransactionAmountSpecification_ShouldNotBeSatisfied_WhenAmountBelowThreshold()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 500m, currencyCode: "USD");
        TransactionAmountSpecification spec = new(1000m, "USD");

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TransactionAmountSpecification_ShouldBeSatisfied_WhenAmountEqualsThreshold()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 1000m, currencyCode: "USD");
        TransactionAmountSpecification spec = new(1000m, "USD");

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TransactionAmountSpecification_ShouldNotBeSatisfied_WhenCurrencyMismatch()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 5000m, currencyCode: "EUR");
        TransactionAmountSpecification spec = new(1000m, "USD");

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TransactionStatusSpecification_ShouldBeSatisfied_WhenStatusMatches()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        TransactionStatusSpecification spec = new(TransactionStatus.Initiated);

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TransactionStatusSpecification_ShouldNotBeSatisfied_WhenStatusDoesNotMatch()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        TransactionStatusSpecification spec = new(TransactionStatus.Completed);

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TransactionAccountSpecification_ShouldBeSatisfied_WhenAccountIsSource()
    {
        // Arrange
        Guid accountGuid = Guid.NewGuid();
        TransactionAggregate transaction = CreateTransaction(sourceAccountId: accountGuid);
        TransactionAccountSpecification spec = new(AccountId.From(accountGuid));

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TransactionAccountSpecification_ShouldBeSatisfied_WhenAccountIsDestination()
    {
        // Arrange
        Guid accountGuid = Guid.NewGuid();
        TransactionAggregate transaction = CreateTransaction(destinationAccountId: accountGuid);
        TransactionAccountSpecification spec = new(AccountId.From(accountGuid));

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TransactionAccountSpecification_ShouldNotBeSatisfied_WhenAccountNotInvolved()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        TransactionAccountSpecification spec = new(AccountId.From(Guid.NewGuid()));

        // Act
        bool result = spec.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_ShouldBeSatisfied_WhenBothSpecificationsAreSatisfied()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 5000m, currencyCode: "USD");
        TransactionAmountSpecification amountSpec = new(1000m, "USD");
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Initiated);

        Specification<TransactionAggregate> combined = amountSpec.And(statusSpec);

        // Act
        bool result = combined.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_ShouldNotBeSatisfied_WhenOneSpecificationFails()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 500m, currencyCode: "USD");
        TransactionAmountSpecification amountSpec = new(1000m, "USD");
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Initiated);

        Specification<TransactionAggregate> combined = amountSpec.And(statusSpec);

        // Act
        bool result = combined.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Or_ShouldBeSatisfied_WhenAtLeastOneSpecificationIsSatisfied()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 500m, currencyCode: "USD");
        TransactionAmountSpecification amountSpec = new(1000m, "USD");
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Initiated);

        Specification<TransactionAggregate> combined = amountSpec.Or(statusSpec);

        // Act
        bool result = combined.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_ShouldNotBeSatisfied_WhenNeitherSpecificationIsSatisfied()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction(amount: 500m, currencyCode: "USD");
        TransactionAmountSpecification amountSpec = new(1000m, "USD");
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Completed);

        Specification<TransactionAggregate> combined = amountSpec.Or(statusSpec);

        // Act
        bool result = combined.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_ShouldNegateSatisfaction()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Initiated);

        Specification<TransactionAggregate> negated = statusSpec.Not();

        // Act
        bool result = negated.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_ShouldBeTrue_WhenOriginalIsFalse()
    {
        // Arrange
        TransactionAggregate transaction = CreateTransaction();
        TransactionStatusSpecification statusSpec = new(TransactionStatus.Completed);

        Specification<TransactionAggregate> negated = statusSpec.Not();

        // Act
        bool result = negated.IsSatisfiedBy(transaction);

        // Assert
        result.Should().BeTrue();
    }
}
