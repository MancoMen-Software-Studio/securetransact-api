using System;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Base record for all domain events providing common properties.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
