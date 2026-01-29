using System;
using System.Linq.Expressions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Specifications;

/// <summary>
/// Specification that evaluates whether a transaction amount exceeds a threshold.
/// Used for compliance checks and high-value transaction detection.
/// </summary>
public sealed class TransactionAmountSpecification : Specification<TransactionAggregate>
{
    private readonly decimal _minimumAmount;
    private readonly string _currencyCode;

    /// <summary>
    /// Creates a specification for transactions exceeding the specified amount in the given currency.
    /// </summary>
    /// <param name="minimumAmount">The minimum amount threshold.</param>
    /// <param name="currencyCode">The ISO 4217 currency code to match.</param>
    public TransactionAmountSpecification(decimal minimumAmount, string currencyCode)
    {
        _minimumAmount = minimumAmount;
        _currencyCode = currencyCode ?? throw new ArgumentNullException(nameof(currencyCode));
    }

    /// <inheritdoc />
    public override Expression<Func<TransactionAggregate, bool>> Criteria =>
        transaction =>
            transaction.Amount.Currency.Code == _currencyCode &&
            transaction.Amount.Amount >= _minimumAmount;
}
