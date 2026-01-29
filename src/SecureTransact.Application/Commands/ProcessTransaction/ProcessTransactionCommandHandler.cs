using System;
using System.Threading;
using System.Threading.Tasks;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Application.Commands.ProcessTransaction;

/// <summary>
/// Handler for processing new transactions.
/// </summary>
public sealed class ProcessTransactionCommandHandler
    : ICommandHandler<ProcessTransactionCommand, TransactionResponse>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionResponse>> Handle(
        ProcessTransactionCommand request,
        CancellationToken cancellationToken)
    {
        Result<Money> moneyResult = Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
        {
            return Result.Failure<TransactionResponse>(moneyResult.Error);
        }

        Result<TransactionAggregate> transactionResult = TransactionAggregate.Create(
            AccountId.From(request.SourceAccountId),
            AccountId.From(request.DestinationAccountId),
            moneyResult.Value,
            request.Reference);

        if (transactionResult.IsFailure)
        {
            return Result.Failure<TransactionResponse>(transactionResult.Error);
        }

        TransactionAggregate transaction = transactionResult.Value;

        string authorizationCode = GenerateAuthorizationCode();
        Result authorizeResult = transaction.Authorize(authorizationCode);
        if (authorizeResult.IsFailure)
        {
            return Result.Failure<TransactionResponse>(authorizeResult.Error);
        }

        Result completeResult = transaction.Complete();
        if (completeResult.IsFailure)
        {
            return Result.Failure<TransactionResponse>(completeResult.Error);
        }

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToResponse(transaction));
    }

    private static string GenerateAuthorizationCode()
    {
        return $"AUTH-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
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
