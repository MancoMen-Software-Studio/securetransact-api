using System;
using System.Collections.Generic;

namespace SecureTransact.Application.DTOs;

/// <summary>
/// Response DTO for transaction history queries.
/// </summary>
public sealed record TransactionHistoryResponse
{
    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public required Guid AccountId { get; init; }

    /// <summary>
    /// Gets the list of transactions.
    /// </summary>
    public required IReadOnlyList<TransactionSummary> Transactions { get; init; }

    /// <summary>
    /// Gets the total count of transactions matching the query.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public required int PageSize { get; init; }
}

/// <summary>
/// Summary DTO for a transaction in a list.
/// </summary>
public sealed record TransactionSummary
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// Gets the transaction type (Debit/Credit relative to the account).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the counterparty account identifier.
    /// </summary>
    public required Guid CounterpartyAccountId { get; init; }

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
    /// Gets the timestamp when initiated.
    /// </summary>
    public required DateTime InitiatedAtUtc { get; init; }
}
