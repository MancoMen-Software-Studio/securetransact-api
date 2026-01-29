using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Application.Abstractions;

/// <summary>
/// Query service for transaction read operations.
/// Optimized for querying, separate from the write model.
/// </summary>
public interface ITransactionQueryService
{
    /// <summary>
    /// Gets transaction history for an account.
    /// </summary>
    Task<(IReadOnlyList<TransactionSummary> Transactions, int TotalCount)> GetHistoryAsync(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
