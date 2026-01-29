using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Infrastructure.Persistence.Contexts;
using SecureTransact.Infrastructure.Persistence.ReadModels;

namespace SecureTransact.Infrastructure.QueryServices;

/// <summary>
/// Query service for transaction read operations using the read model.
/// </summary>
public sealed class TransactionQueryService : ITransactionQueryService
{
    private readonly TransactionDbContext _context;

    public TransactionQueryService(TransactionDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets transaction history for an account with pagination and date filtering.
    /// </summary>
    public async Task<(IReadOnlyList<TransactionSummary> Transactions, int TotalCount)> GetHistoryAsync(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TransactionReadModel> query = _context.Transactions
            .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.InitiatedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.InitiatedAtUtc <= toDate.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<TransactionReadModel> transactions = await query
            .OrderByDescending(t => t.InitiatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<TransactionSummary> summaries = transactions
            .Select(t => MapToSummary(t, accountId))
            .ToList();

        return (summaries, totalCount);
    }

    private static TransactionSummary MapToSummary(TransactionReadModel transaction, Guid accountId)
    {
        bool isSource = transaction.SourceAccountId == accountId;

        return new TransactionSummary
        {
            TransactionId = transaction.Id,
            Type = isSource ? "Debit" : "Credit",
            CounterpartyAccountId = isSource ? transaction.DestinationAccountId : transaction.SourceAccountId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Status = transaction.Status,
            InitiatedAtUtc = transaction.InitiatedAtUtc
        };
    }
}
