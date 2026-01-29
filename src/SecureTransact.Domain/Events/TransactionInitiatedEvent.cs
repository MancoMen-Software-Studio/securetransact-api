using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a new transaction is initiated.
/// </summary>
public sealed record TransactionInitiatedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the source account identifier.
    /// </summary>
    public AccountId SourceAccountId { get; init; }

    /// <summary>
    /// Gets the destination account identifier.
    /// </summary>
    public AccountId DestinationAccountId { get; init; }

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public Money Amount { get; init; } = null!;

    /// <summary>
    /// Gets the optional reference or description.
    /// </summary>
    public string? Reference { get; init; }
}
