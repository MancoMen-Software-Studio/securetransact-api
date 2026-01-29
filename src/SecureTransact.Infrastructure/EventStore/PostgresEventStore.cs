using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Infrastructure.Cryptography;
using SecureTransact.Infrastructure.Persistence.Contexts;

namespace SecureTransact.Infrastructure.EventStore;

/// <summary>
/// PostgreSQL-based event store with hash-chained cryptographic integrity.
/// </summary>
public sealed class PostgresEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly ICryptoService _cryptoService;
    private readonly IEventSerializer _serializer;
    private readonly EventStoreSettings _settings;

    public PostgresEventStore(
        EventStoreDbContext context,
        ICryptoService cryptoService,
        IEventSerializer serializer,
        IOptions<EventStoreSettings> settings)
    {
        _context = context;
        _cryptoService = cryptoService;
        _serializer = serializer;
        _settings = settings.Value;
    }

    /// <summary>
    /// Appends events to the event store with cryptographic chain integrity.
    /// </summary>
    public async Task<Result> AppendEventsAsync(
        Guid streamId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        List<IDomainEvent> eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return Result.Success();
        }

        StoredEvent? lastEvent = await _context.Events
            .Where(e => e.AggregateId == streamId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        long currentVersion = lastEvent?.Version ?? -1;
        if (currentVersion != expectedVersion)
        {
            return Result.Failure(DomainError.Conflict(
                "EventStore.ConcurrencyConflict",
                $"Expected version {expectedVersion} but found {currentVersion} for stream {streamId}"));
        }

        byte[]? previousHash = lastEvent?.ChainHash;
        int version = (int)currentVersion;

        foreach (IDomainEvent @event in eventList)
        {
            version++;

            byte[] serializedData = _serializer.Serialize(@event);
            byte[] encryptedData = _cryptoService.Encrypt(serializedData);
            byte[] chainHash = _cryptoService.ComputeChainHash(previousHash, serializedData);

            StoredEvent storedEvent = new()
            {
                Id = @event.EventId,
                AggregateId = streamId,
                EventType = _serializer.GetEventTypeName(@event),
                EventData = encryptedData,
                Version = version,
                OccurredAtUtc = @event.OccurredOnUtc,
                ChainHash = chainHash,
                PreviousHash = previousHash
            };

            _context.Events.Add(storedEvent);
            previousHash = chainHash;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (IsConcurrencyException(ex))
        {
            return Result.Failure(DomainError.Conflict(
                "EventStore.ConcurrencyConflict",
                $"Concurrency conflict when appending events to stream {streamId}"));
        }
    }

    /// <summary>
    /// Reads all events for a stream.
    /// </summary>
    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        return await GetEventsAsync(streamId, fromVersion: 0, cancellationToken);
    }

    /// <summary>
    /// Reads events for a stream from a specific version.
    /// </summary>
    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        long fromVersion,
        CancellationToken cancellationToken = default)
    {
        List<StoredEvent> storedEvents = await _context.Events
            .Where(e => e.AggregateId == streamId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (storedEvents.Count == 0)
        {
            return Array.Empty<IDomainEvent>();
        }

        List<IDomainEvent> events = new(storedEvents.Count);
        byte[]? previousHash = null;

        if (fromVersion > 0)
        {
            StoredEvent? previousEvent = await _context.Events
                .Where(e => e.AggregateId == streamId && e.Version == fromVersion)
                .FirstOrDefaultAsync(cancellationToken);

            previousHash = previousEvent?.ChainHash;
        }

        foreach (StoredEvent storedEvent in storedEvents)
        {
            byte[] decryptedData = _cryptoService.Decrypt(storedEvent.EventData);

            if (_settings.VerifyChainOnRead)
            {
                byte[] expectedHash = _cryptoService.ComputeChainHash(previousHash, decryptedData);
                if (!CryptographicOperations.FixedTimeEquals(expectedHash, storedEvent.ChainHash))
                {
                    throw new EventChainIntegrityException(
                        $"Chain integrity violation detected for event {storedEvent.Id} " +
                        $"at version {storedEvent.Version} of stream {streamId}");
                }
            }

            IDomainEvent @event = _serializer.Deserialize(decryptedData, storedEvent.EventType);
            events.Add(@event);

            previousHash = storedEvent.ChainHash;
        }

        return events;
    }

    /// <summary>
    /// Gets the current version of a stream.
    /// </summary>
    public async Task<int> GetVersionAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        int? maxVersion = await _context.Events
            .Where(e => e.AggregateId == streamId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return maxVersion ?? -1;
    }

    /// <summary>
    /// Verifies the integrity of the entire event chain for a stream.
    /// </summary>
    public async Task<Result<bool>> VerifyHashChainAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        List<StoredEvent> storedEvents = await _context.Events
            .Where(e => e.AggregateId == streamId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (storedEvents.Count == 0)
        {
            return Result.Success(true);
        }

        byte[]? previousHash = null;

        foreach (StoredEvent storedEvent in storedEvents)
        {
            if (!ByteArraysEqual(storedEvent.PreviousHash, previousHash))
            {
                return Result.Success(false);
            }

            byte[] decryptedData;
            try
            {
                decryptedData = _cryptoService.Decrypt(storedEvent.EventData);
            }
            catch
            {
                return Result.Success(false);
            }

            byte[] expectedHash = _cryptoService.ComputeChainHash(previousHash, decryptedData);

            if (!CryptographicOperations.FixedTimeEquals(expectedHash, storedEvent.ChainHash))
            {
                return Result.Success(false);
            }

            previousHash = storedEvent.ChainHash;
        }

        return Result.Success(true);
    }

    private static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static bool IsConcurrencyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("ix_events_aggregate_version") == true
            || ex.InnerException?.Message.Contains("duplicate key") == true;
    }
}

/// <summary>
/// Exception thrown when event chain integrity is violated.
/// </summary>
public sealed class EventChainIntegrityException : Exception
{
    public EventChainIntegrityException(string message) : base(message)
    {
    }
}
