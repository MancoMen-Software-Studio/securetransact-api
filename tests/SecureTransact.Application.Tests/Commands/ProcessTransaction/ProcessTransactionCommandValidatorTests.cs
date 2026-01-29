using System;
using FluentAssertions;
using FluentValidation.Results;
using SecureTransact.Application.Commands.ProcessTransaction;
using Xunit;

namespace SecureTransact.Application.Tests.Commands.ProcessTransaction;

public sealed class ProcessTransactionCommandValidatorTests
{
    private readonly ProcessTransactionCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Reference = "Test payment"
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSourceAccountIdIsEmpty()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.Empty,
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD"
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceAccountId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDestinationAccountIdIsEmpty()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.Empty,
            Amount = 100m,
            Currency = "USD"
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationAccountId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSourceAndDestinationAreSame()
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
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationAccountId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_ShouldFail_WhenAmountIsNotPositive(decimal amount)
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = amount,
            Currency = "USD"
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenCurrencyIsEmpty(string? currency)
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = currency!
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Validate_ShouldFail_WhenCurrencyIsNotThreeLetters(string currency)
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = currency
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCurrencyIsNotSupported()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "XXX"
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Validate_ShouldFail_WhenReferenceIsTooLong()
    {
        // Arrange
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Reference = new string('x', 257)
        };

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reference");
    }
}
