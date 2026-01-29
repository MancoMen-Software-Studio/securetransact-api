using System;
using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Application.Abstractions;

/// <summary>
/// Repository interface for transactions.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Gets a transaction by its identifier.
    /// </summary>
    Task<TransactionAggregate?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new transaction.
    /// </summary>
    Task AddAsync(TransactionAggregate transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    void Update(TransactionAggregate transaction);
}
