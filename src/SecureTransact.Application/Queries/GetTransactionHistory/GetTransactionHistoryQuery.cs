using System;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Application.Queries.GetTransactionHistory;

/// <summary>
/// Query to get transaction history for an account.
/// </summary>
public sealed record GetTransactionHistoryQuery : IQuery<TransactionHistoryResponse>
{
    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public required Guid AccountId { get; init; }

    /// <summary>
    /// Gets the start date filter (optional).
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Gets the end date filter (optional).
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; } = 20;
}
