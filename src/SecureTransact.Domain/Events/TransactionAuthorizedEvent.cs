using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Events;

/// <summary>
/// Event raised when a transaction has been authorized.
/// </summary>
public sealed record TransactionAuthorizedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public TransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the authorization code from the payment processor.
    /// </summary>
    public string AuthorizationCode { get; init; } = string.Empty;
}
