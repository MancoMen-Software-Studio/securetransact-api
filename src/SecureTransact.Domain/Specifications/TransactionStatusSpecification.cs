using System;
using System.Linq.Expressions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Specifications;

/// <summary>
/// Specification that evaluates whether a transaction has a specific status.
/// Used for filtering transactions by state.
/// </summary>
public sealed class TransactionStatusSpecification : Specification<TransactionAggregate>
{
    private readonly TransactionStatus _status;

    /// <summary>
    /// Creates a specification for transactions with the specified status.
    /// </summary>
    /// <param name="status">The transaction status to match.</param>
    public TransactionStatusSpecification(TransactionStatus status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <inheritdoc />
    public override Expression<Func<TransactionAggregate, bool>> Criteria =>
        transaction => transaction.Status == _status;
}
