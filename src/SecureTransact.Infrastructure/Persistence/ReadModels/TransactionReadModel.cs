using System;

namespace SecureTransact.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Read model for transaction queries (CQRS query side).
/// Denormalized for efficient querying.
/// </summary>
public sealed class TransactionReadModel
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the source account identifier.
    /// </summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>
    /// Gets or sets the destination account identifier.
    /// </summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional reference.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets the authorization code.
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Gets or sets the failure code if failed.
    /// </summary>
    public string? FailureCode { get; set; }

    /// <summary>
    /// Gets or sets the failure reason if failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the reversal reason if reversed.
    /// </summary>
    public string? ReversalReason { get; set; }

    /// <summary>
    /// Gets or sets the dispute reason if disputed.
    /// </summary>
    public string? DisputeReason { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was initiated.
    /// </summary>
    public DateTime InitiatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was authorized.
    /// </summary>
    public DateTime? AuthorizedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the transaction failed.
    /// </summary>
    public DateTime? FailedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was reversed.
    /// </summary>
    public DateTime? ReversedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was disputed.
    /// </summary>
    public DateTime? DisputedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the aggregate version for optimistic concurrency.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets when the read model was last updated.
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; set; }
}
