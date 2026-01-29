using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Interface for the event store that persists domain events.
/// The event store provides append-only semantics with hash chaining for tamper detection.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to a stream with optimistic concurrency control.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream (typically aggregate ID).</param>
    /// <param name="events">The events to append.</param>
    /// <param name="expectedVersion">The expected version of the stream for optimistic concurrency. Use -1 for new streams.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure (e.g., concurrency conflict).</returns>
    Task<Result> AppendEventsAsync(
        Guid streamId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events for a stream in order.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The events in the stream, ordered by version.</returns>
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a stream starting from a specific version.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="fromVersion">The version to start reading from (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The events in the stream from the specified version.</returns>
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId,
        long fromVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of the hash chain for a stream.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the chain is valid or has been tampered with.</returns>
    Task<Result<bool>> VerifyHashChainAsync(
        Guid streamId,
        CancellationToken cancellationToken = default);
}
