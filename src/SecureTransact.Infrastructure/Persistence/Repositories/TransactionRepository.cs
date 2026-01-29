using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.Abstractions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for transaction aggregates using event sourcing.
/// </summary>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly IEventStore _eventStore;

    public TransactionRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    /// <summary>
    /// Gets a transaction by its identifier, reconstructing from events.
    /// </summary>
    public async Task<TransactionAggregate?> GetByIdAsync(
        TransactionId id,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IDomainEvent> events = await _eventStore.GetEventsAsync(
            id.Value,
            fromVersion: 0,
            cancellationToken);

        if (events.Count == 0)
        {
            return null;
        }

        return TransactionAggregate.LoadFromHistory(events);
    }

    /// <summary>
    /// Adds a new transaction aggregate to the event store.
    /// </summary>
    public async Task AddAsync(TransactionAggregate transaction, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IDomainEvent> events = transaction.GetDomainEvents();
        if (events.Count == 0)
        {
            return;
        }

        await _eventStore.AppendEventsAsync(
            transaction.Id.Value,
            events,
            expectedVersion: 0,
            cancellationToken);

        transaction.ClearDomainEvents();
    }

    /// <summary>
    /// Updates an existing transaction in the event store.
    /// </summary>
    public void Update(TransactionAggregate transaction)
    {
    }
}
