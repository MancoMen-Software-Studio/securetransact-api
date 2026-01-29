using System;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a transaction has been successfully completed.
/// </summary>
public sealed record TransactionCompletedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the timestamp when the transaction was completed.
    /// </summary>
    public DateTime CompletedAtUtc { get; init; }
}
