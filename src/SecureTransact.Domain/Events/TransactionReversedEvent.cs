using System;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a completed transaction has been reversed.
/// </summary>
public sealed record TransactionReversedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the reason for the reversal.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the reversal occurred.
    /// </summary>
    public DateTime ReversedAtUtc { get; init; }
}
