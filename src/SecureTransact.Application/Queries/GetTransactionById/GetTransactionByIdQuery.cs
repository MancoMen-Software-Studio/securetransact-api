using System;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Application.Queries.GetTransactionById;

/// <summary>
/// Query to get a transaction by its identifier.
/// </summary>
public sealed record GetTransactionByIdQuery : IQuery<TransactionResponse>
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required Guid TransactionId { get; init; }
}
