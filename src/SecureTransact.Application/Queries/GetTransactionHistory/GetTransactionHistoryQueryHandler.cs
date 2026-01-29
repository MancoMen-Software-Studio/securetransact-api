using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Queries.GetTransactionHistory;

/// <summary>
/// Handler for getting transaction history.
/// </summary>
public sealed class GetTransactionHistoryQueryHandler
    : IQueryHandler<GetTransactionHistoryQuery, TransactionHistoryResponse>
{
    private readonly ITransactionQueryService _queryService;

    public GetTransactionHistoryQueryHandler(ITransactionQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<TransactionHistoryResponse>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(
                request.AccountId,
                request.FromDate,
                request.ToDate,
                request.Page,
                request.PageSize,
                cancellationToken);

        TransactionHistoryResponse response = new()
        {
            AccountId = request.AccountId,
            Transactions = transactions,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
