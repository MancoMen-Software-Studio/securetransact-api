using System;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a transaction is disputed.
/// </summary>
public sealed record TransactionDisputedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the dispute reason.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the dispute was filed.
    /// </summary>
    public DateTime DisputedAtUtc { get; init; }
}
