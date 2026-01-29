using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a transaction has failed.
/// </summary>
public sealed record TransactionFailedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the failure reason code.
    /// </summary>
    public string FailureCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets the human-readable failure reason.
    /// </summary>
    public string FailureReason { get; init; } = string.Empty;
}
