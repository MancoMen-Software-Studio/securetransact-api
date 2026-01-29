using System;

namespace SecureTransact.Infrastructure.EventStore;

/// <summary>
/// Represents a persisted domain event with cryptographic integrity.
/// </summary>
public sealed class StoredEvent
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the aggregate identifier.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified event type name.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted and serialized event data.
    /// </summary>
    public byte[] EventData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the event version within the aggregate.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the hash chain value linking to the previous event.
    /// </summary>
    public byte[] ChainHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the hash of the previous event in the chain.
    /// Null for the first event of an aggregate.
    /// </summary>
    public byte[]? PreviousHash { get; set; }

    /// <summary>
    /// Gets or sets the global sequence number for ordering across all aggregates.
    /// </summary>
    public long GlobalSequence { get; set; }
}
