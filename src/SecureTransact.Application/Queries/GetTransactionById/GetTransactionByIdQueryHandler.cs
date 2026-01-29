using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.Errors;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Application.Queries.GetTransactionById;

/// <summary>
/// Handler for getting a transaction by ID.
/// </summary>
public sealed class GetTransactionByIdQueryHandler
    : IQueryHandler<GetTransactionByIdQuery, TransactionResponse>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<TransactionResponse>> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        TransactionId transactionId = TransactionId.From(request.TransactionId);
        TransactionAggregate? transaction = await _transactionRepository.GetByIdAsync(
            transactionId,
            cancellationToken);

        if (transaction is null)
        {
            return Result.Failure<TransactionResponse>(
                TransactionErrors.NotFound(request.TransactionId.ToString()));
        }

        return Result.Success(MapToResponse(transaction));
    }

    private static TransactionResponse MapToResponse(TransactionAggregate transaction)
    {
        return new TransactionResponse
        {
            TransactionId = transaction.Id.Value,
            SourceAccountId = transaction.SourceAccountId.Value,
            DestinationAccountId = transaction.DestinationAccountId.Value,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.Currency.Code,
            Status = transaction.Status.Name,
            Reference = transaction.Reference,
            AuthorizationCode = transaction.AuthorizationCode,
            InitiatedAtUtc = transaction.InitiatedAtUtc,
            CompletedAtUtc = transaction.CompletedAtUtc
        };
    }
}
