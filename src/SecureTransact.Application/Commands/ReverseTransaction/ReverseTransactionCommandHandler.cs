using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.Errors;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Application.Commands.ReverseTransaction;

/// <summary>
/// Handler for reversing transactions.
/// </summary>
public sealed class ReverseTransactionCommandHandler
    : ICommandHandler<ReverseTransactionCommand, TransactionResponse>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReverseTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionResponse>> Handle(
        ReverseTransactionCommand request,
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

        Result reverseResult = transaction.Reverse(request.Reason);
        if (reverseResult.IsFailure)
        {
            return Result.Failure<TransactionResponse>(reverseResult.Error);
        }

        _transactionRepository.Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
