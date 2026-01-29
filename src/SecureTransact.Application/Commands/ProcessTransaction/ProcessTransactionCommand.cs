using System;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Application.Commands.ProcessTransaction;

/// <summary>
/// Command to process a new transaction.
/// </summary>
public sealed record ProcessTransactionCommand : ICommand<TransactionResponse>
{
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
    /// Gets the currency code (ISO 4217).
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the optional reference or description.
    /// </summary>
    public string? Reference { get; init; }
}
