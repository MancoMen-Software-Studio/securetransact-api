using System;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidAmountAndCurrency()
    {
        // Act
        Result<Money> result = Money.Create(100.50m, Currency.USD);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.50m);
        result.Value.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Create_ShouldSucceed_WithCurrencyCode()
    {
        // Act
        Result<Money> result = Money.Create(100.50m, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.50m);
        result.Value.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Create_ShouldFail_WithNegativeAmount()
    {
        // Act
        Result<Money> result = Money.Create(-100m, Currency.USD);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.NegativeAmount);
    }

    [Fact]
    public void Create_ShouldFail_WithInvalidCurrencyCode()
    {
        // Act
        Result<Money> result = Money.Create(100m, "INVALID");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.InvalidCurrency);
    }

    [Fact]
    public void Create_ShouldRoundToCorrectDecimalPlaces()
    {
        // Arrange - USD has 2 decimal places
        decimal amount = 100.555m;

        // Act
        Result<Money> result = Money.Create(amount, Currency.USD);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.56m); // Banker's rounding
    }

    [Fact]
    public void Create_ShouldRoundToZeroDecimalPlaces_ForJPY()
    {
        // Arrange - JPY has 0 decimal places
        decimal amount = 100.99m;

        // Act
        Result<Money> result = Money.Create(amount, Currency.JPY);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(101m);
    }

    [Fact]
    public void Zero_ShouldCreateZeroAmount()
    {
        // Act
        Money money = Money.Zero(Currency.USD);

        // Assert
        money.Amount.Should().Be(0);
        money.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Add_ShouldSucceed_WithSameCurrency()
    {
        // Arrange
        Money money1 = Money.Create(100m, Currency.USD).Value;
        Money money2 = Money.Create(50m, Currency.USD).Value;

        // Act
        Result<Money> result = money1.Add(money2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(150m);
    }

    [Fact]
    public void Add_ShouldFail_WithDifferentCurrencies()
    {
        // Arrange
        Money usd = Money.Create(100m, Currency.USD).Value;
        Money eur = Money.Create(50m, Currency.EUR).Value;

        // Act
        Result<Money> result = usd.Add(eur);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.CurrencyMismatch);
    }

    [Fact]
    public void Subtract_ShouldSucceed_WithSameCurrency()
    {
        // Arrange
        Money money1 = Money.Create(100m, Currency.USD).Value;
        Money money2 = Money.Create(30m, Currency.USD).Value;

        // Act
        Result<Money> result = money1.Subtract(money2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(70m);
    }

    [Fact]
    public void Subtract_ShouldFail_WhenResultWouldBeNegative()
    {
        // Arrange
        Money money1 = Money.Create(30m, Currency.USD).Value;
        Money money2 = Money.Create(100m, Currency.USD).Value;

        // Act
        Result<Money> result = money1.Subtract(money2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.NegativeAmount);
    }

    [Fact]
    public void Subtract_ShouldFail_WithDifferentCurrencies()
    {
        // Arrange
        Money usd = Money.Create(100m, Currency.USD).Value;
        Money eur = Money.Create(50m, Currency.EUR).Value;

        // Act
        Result<Money> result = usd.Subtract(eur);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.CurrencyMismatch);
    }

    [Fact]
    public void Multiply_ShouldSucceed_WithPositiveFactor()
    {
        // Arrange
        Money money = Money.Create(100m, Currency.USD).Value;

        // Act
        Result<Money> result = money.Multiply(1.5m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(150m);
    }

    [Fact]
    public void Multiply_ShouldFail_WithNegativeFactor()
    {
        // Arrange
        Money money = Money.Create(100m, Currency.USD).Value;

        // Act
        Result<Money> result = money.Multiply(-1.5m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MoneyErrors.NegativeAmount);
    }

    [Fact]
    public void CompareTo_ShouldCompareAmounts_WithSameCurrency()
    {
        // Arrange
        Money money1 = Money.Create(100m, Currency.USD).Value;
        Money money2 = Money.Create(200m, Currency.USD).Value;

        // Act & Assert
        money1.CompareTo(money2).Should().BeLessThan(0);
        money2.CompareTo(money1).Should().BeGreaterThan(0);
        money1.CompareTo(money1).Should().Be(0);
    }

    [Fact]
    public void CompareTo_ShouldThrow_WithDifferentCurrencies()
    {
        // Arrange
        Money usd = Money.Create(100m, Currency.USD).Value;
        Money eur = Money.Create(100m, Currency.EUR).Value;

        // Act
        Action act = () => usd.CompareTo(eur);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void IsGreaterThan_ShouldReturnCorrectResult()
    {
        // Arrange
        Money money1 = Money.Create(200m, Currency.USD).Value;
        Money money2 = Money.Create(100m, Currency.USD).Value;

        // Assert
        money1.IsGreaterThan(money2).Should().BeTrue();
        money2.IsGreaterThan(money1).Should().BeFalse();
    }

    [Fact]
    public void IsLessThan_ShouldReturnCorrectResult()
    {
        // Arrange
        Money money1 = Money.Create(100m, Currency.USD).Value;
        Money money2 = Money.Create(200m, Currency.USD).Value;

        // Assert
        money1.IsLessThan(money2).Should().BeTrue();
        money2.IsLessThan(money1).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldFormatWithSymbol()
    {
        // Arrange
        Money money = Money.Create(1234.56m, Currency.USD).Value;

        // Act
        string result = money.ToString();

        // Assert
        result.Should().Contain("$");
        result.Should().Contain("1");
    }

    [Fact]
    public void ToStringWithCode_ShouldIncludeCurrencyCode()
    {
        // Arrange
        Money money = Money.Create(100m, Currency.USD).Value;

        // Act
        string result = money.ToStringWithCode();

        // Assert
        result.Should().Contain("USD");
    }

    [Fact]
    public void Equality_ShouldBeBasedOnAmountAndCurrency()
    {
        // Arrange
        Money money1 = Money.Create(100m, Currency.USD).Value;
        Money money2 = Money.Create(100m, Currency.USD).Value;
        Money money3 = Money.Create(100m, Currency.EUR).Value;

        // Assert
        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
    }
}
