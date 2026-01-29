using System;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Application.Commands.ReverseTransaction;

/// <summary>
/// Command to reverse a completed transaction.
/// </summary>
public sealed record ReverseTransactionCommand : ICommand<TransactionResponse>
{
    /// <summary>
    /// Gets the transaction identifier to reverse.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// Gets the reason for the reversal.
    /// </summary>
    public required string Reason { get; init; }
}
