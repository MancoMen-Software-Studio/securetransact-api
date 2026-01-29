using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.Persistence.Contexts;

namespace SecureTransact.Infrastructure.Persistence;

/// <summary>
/// Unit of work implementation coordinating event store and read model persistence.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly EventStoreDbContext _eventStoreContext;
    private readonly IEventStore _eventStore;
    private readonly List<TransactionAggregate> _trackedAggregates = new();
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        EventStoreDbContext eventStoreContext,
        IEventStore eventStore)
    {
        _eventStoreContext = eventStoreContext;
        _eventStore = eventStore;
    }

    /// <summary>
    /// Tracks an aggregate for pending event persistence.
    /// </summary>
    public void Track(TransactionAggregate aggregate)
    {
        if (!_trackedAggregates.Contains(aggregate))
        {
            _trackedAggregates.Add(aggregate);
        }
    }

    /// <summary>
    /// Saves all pending changes atomically.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int changes = 0;

        await using IDbContextTransaction transaction = await _eventStoreContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (TransactionAggregate aggregate in _trackedAggregates)
            {
                IReadOnlyList<IDomainEvent> events = aggregate.GetDomainEvents();
                if (events.Count == 0)
                {
                    continue;
                }

                int eventCount = events.Count;
                long expectedVersion = aggregate.Version - eventCount;

                Result result = await _eventStore.AppendEventsAsync(
                    aggregate.Id.Value,
                    events,
                    expectedVersion,
                    cancellationToken);

                if (result.IsFailure)
                {
                    throw new InvalidOperationException($"Failed to append events: {result.Error.Description}");
                }

                changes += events.Count;
                aggregate.ClearDomainEvents();
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _trackedAggregates.Clear();
        }

        return changes;
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _eventStoreContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
