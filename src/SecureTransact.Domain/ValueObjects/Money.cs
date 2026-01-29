using System;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with a specific currency.
/// Immutable value object with validation at creation time.
/// </summary>
public sealed record Money : IComparable<Money>
{
    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency of this money value.
    /// </summary>
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a Money value object with validation.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency.</param>
    /// <returns>A Result containing the Money or an error.</returns>
    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (currency is null)
        {
            return Result.Failure<Money>(MoneyErrors.InvalidCurrency);
        }

        if (amount < 0)
        {
            return Result.Failure<Money>(MoneyErrors.NegativeAmount);
        }

        decimal roundedAmount = Math.Round(amount, currency.DecimalPlaces, MidpointRounding.ToEven);

        return Result.Success(new Money(roundedAmount, currency));
    }

    /// <summary>
    /// Creates a Money value object from amount and currency code.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <returns>A Result containing the Money or an error.</returns>
    public static Result<Money> Create(decimal amount, string currencyCode)
    {
        Currency? currency = Currency.FromCode(currencyCode);
        if (currency is null)
        {
            return Result.Failure<Money>(MoneyErrors.InvalidCurrency);
        }

        return Create(amount, currency);
    }

    /// <summary>
    /// Creates a zero value in the specified currency.
    /// </summary>
    public static Money Zero(Currency currency) => new(0, currency);

    /// <summary>
    /// Adds two money values. They must have the same currency.
    /// </summary>
    public Result<Money> Add(Money other)
    {
        if (!Currency.Equals(other.Currency))
        {
            return Result.Failure<Money>(MoneyErrors.CurrencyMismatch);
        }

        return Create(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtracts another money value. They must have the same currency.
    /// </summary>
    public Result<Money> Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency))
        {
            return Result.Failure<Money>(MoneyErrors.CurrencyMismatch);
        }

        decimal newAmount = Amount - other.Amount;
        if (newAmount < 0)
        {
            return Result.Failure<Money>(MoneyErrors.NegativeAmount);
        }

        return Create(newAmount, Currency);
    }

    /// <summary>
    /// Multiplies the amount by a factor.
    /// </summary>
    public Result<Money> Multiply(decimal factor)
    {
        if (factor < 0)
        {
            return Result.Failure<Money>(MoneyErrors.NegativeAmount);
        }

        return Create(Amount * factor, Currency);
    }

    /// <summary>
    /// Compares this Money with another of the same currency.
    /// </summary>
    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (!Currency.Equals(other.Currency))
        {
            throw new InvalidOperationException("Cannot compare money values with different currencies.");
        }

        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Checks if this amount is greater than another.
    /// </summary>
    public bool IsGreaterThan(Money other)
    {
        if (!Currency.Equals(other.Currency))
        {
            throw new InvalidOperationException("Cannot compare money values with different currencies.");
        }

        return Amount > other.Amount;
    }

    /// <summary>
    /// Checks if this amount is less than another.
    /// </summary>
    public bool IsLessThan(Money other)
    {
        if (!Currency.Equals(other.Currency))
        {
            throw new InvalidOperationException("Cannot compare money values with different currencies.");
        }

        return Amount < other.Amount;
    }

    /// <summary>
    /// Checks if this is a zero amount.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Returns a formatted string representation.
    /// </summary>
    public override string ToString() => $"{Currency.Symbol}{Amount:N}";

    /// <summary>
    /// Returns a string with the currency code.
    /// </summary>
    public string ToStringWithCode() => $"{Amount:N} {Currency.Code}";

    /// <summary>
    /// Less than operator.
    /// </summary>
    public static bool operator <(Money left, Money right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    public static bool operator <=(Money left, Money right) =>
        left is null || left.CompareTo(right) <= 0;

    /// <summary>
    /// Greater than operator.
    /// </summary>
    public static bool operator >(Money left, Money right) =>
        left is not null && left.CompareTo(right) > 0;

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    public static bool operator >=(Money left, Money right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}

/// <summary>
/// Domain errors related to Money value object.
/// </summary>
public static class MoneyErrors
{
    /// <summary>
    /// Error for negative monetary amounts.
    /// </summary>
    public static readonly DomainError NegativeAmount = DomainError.Validation(
        "Money.NegativeAmount",
        "Monetary amount cannot be negative.");

    /// <summary>
    /// Error for invalid or unsupported currency.
    /// </summary>
    public static readonly DomainError InvalidCurrency = DomainError.Validation(
        "Money.InvalidCurrency",
        "The specified currency is invalid or not supported.");

    /// <summary>
    /// Error for operations with different currencies.
    /// </summary>
    public static readonly DomainError CurrencyMismatch = DomainError.Validation(
        "Money.CurrencyMismatch",
        "Cannot perform operation on money values with different currencies.");
}
