using System;

namespace SecureTransact.Application.DTOs;

/// <summary>
/// Response DTO for transaction operations.
/// </summary>
public sealed record TransactionResponse
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// Gets the source account identifier.
    /// </summary>
    public required Guid SourceAccountId { get; init; }

    /// <summary>
    /// Gets the destination account identifier.
    /// </summary>
    public required Guid DestinationAccountId { get; init; }

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the optional reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Gets the authorization code if authorized.
    /// </summary>
    public string? AuthorizationCode { get; init; }

    /// <summary>
    /// Gets the timestamp when initiated.
    /// </summary>
    public required DateTime InitiatedAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when completed, if applicable.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }
}
