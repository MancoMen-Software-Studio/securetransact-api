using System;
using System.Linq.Expressions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Specifications;

/// <summary>
/// Specification that evaluates whether a transaction involves a specific account
/// (either as source or destination).
/// </summary>
public sealed class TransactionAccountSpecification : Specification<TransactionAggregate>
{
    private readonly AccountId _accountId;

    /// <summary>
    /// Creates a specification for transactions involving the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier to match.</param>
    public TransactionAccountSpecification(AccountId accountId)
    {
        _accountId = accountId;
    }

    /// <inheritdoc />
    public override Expression<Func<TransactionAggregate, bool>> Criteria =>
        transaction =>
            transaction.SourceAccountId == _accountId ||
            transaction.DestinationAccountId == _accountId;
}
