using System;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Errors;
using SecureTransact.Domain.Events;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Domain.Aggregates;

/// <summary>
/// Aggregate root for financial transactions.
/// Uses event sourcing to maintain a complete audit trail.
/// </summary>
public sealed class TransactionAggregate : AggregateRoot<TransactionId>
{
    /// <summary>
    /// Gets the source account identifier.
    /// </summary>
    public AccountId SourceAccountId { get; private set; }

    /// <summary>
    /// Gets the destination account identifier.
    /// </summary>
    public AccountId DestinationAccountId { get; private set; }

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the current transaction status.
    /// </summary>
    public TransactionStatus Status { get; private set; } = null!;

    /// <summary>
    /// Gets the optional reference or description.
    /// </summary>
    public string? Reference { get; private set; }

    /// <summary>
    /// Gets the authorization code if authorized.
    /// </summary>
    public string? AuthorizationCode { get; private set; }

    /// <summary>
    /// Gets the failure reason if failed.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Gets the timestamp when the transaction was initiated.
    /// </summary>
    public DateTime InitiatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the timestamp when the transaction was completed, if applicable.
    /// </summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Gets the event stream version for optimistic concurrency.
    /// </summary>
    public long Version { get; private set; } = -1;

    private TransactionAggregate()
    {
    }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    public static Result<TransactionAggregate> Create(
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        Money amount,
        string? reference = null)
    {
        if (sourceAccountId == destinationAccountId)
        {
            return Result.Failure<TransactionAggregate>(TransactionErrors.SameAccount);
        }

        if (amount.IsZero)
        {
            return Result.Failure<TransactionAggregate>(TransactionErrors.InvalidAmount);
        }

        TransactionAggregate transaction = new()
        {
            Id = TransactionId.New(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Reference = reference,
            Status = TransactionStatus.Initiated,
            InitiatedAtUtc = DateTime.UtcNow
        };

        transaction.RaiseDomainEvent(new TransactionInitiatedEvent
        {
            TransactionId = transaction.Id,
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Reference = reference
        });

        return Result.Success(transaction);
    }

    /// <summary>
    /// Authorizes the transaction.
    /// </summary>
    public Result Authorize(string authorizationCode)
    {
        if (!Status.CanTransitionTo(TransactionStatus.Authorized))
        {
            return Result.Failure(TransactionErrors.InvalidStatusTransition(Status.Name, TransactionStatus.Authorized.Name));
        }

        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            return Result.Failure(TransactionErrors.AuthorizationFailed("Authorization code is required."));
        }

        Status = TransactionStatus.Authorized;
        AuthorizationCode = authorizationCode;

        RaiseDomainEvent(new TransactionAuthorizedEvent
        {
            TransactionId = Id,
            AuthorizationCode = authorizationCode
        });

        return Result.Success();
    }

    /// <summary>
    /// Completes the transaction.
    /// </summary>
    public Result Complete()
    {
        if (!Status.CanTransitionTo(TransactionStatus.Completed))
        {
            return Result.Failure(TransactionErrors.InvalidStatusTransition(Status.Name, TransactionStatus.Completed.Name));
        }

        Status = TransactionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new TransactionCompletedEvent
        {
            TransactionId = Id,
            CompletedAtUtc = CompletedAtUtc.Value
        });

        return Result.Success();
    }

    /// <summary>
    /// Fails the transaction.
    /// </summary>
    public Result Fail(string failureCode, string failureReason)
    {
        if (!Status.CanTransitionTo(TransactionStatus.Failed))
        {
            return Result.Failure(TransactionErrors.InvalidStatusTransition(Status.Name, TransactionStatus.Failed.Name));
        }

        Status = TransactionStatus.Failed;
        FailureReason = failureReason;

        RaiseDomainEvent(new TransactionFailedEvent
        {
            TransactionId = Id,
            FailureCode = failureCode,
            FailureReason = failureReason
        });

        return Result.Success();
    }

    /// <summary>
    /// Reverses a completed transaction.
    /// </summary>
    public Result Reverse(string reason)
    {
        if (!Status.CanTransitionTo(TransactionStatus.Reversed))
        {
            return Result.Failure(TransactionErrors.InvalidStatusTransition(Status.Name, TransactionStatus.Reversed.Name));
        }

        Status = TransactionStatus.Reversed;

        RaiseDomainEvent(new TransactionReversedEvent
        {
            TransactionId = Id,
            Reason = reason,
            ReversedAtUtc = DateTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// Marks the transaction as disputed.
    /// </summary>
    public Result Dispute(string reason)
    {
        if (!Status.CanTransitionTo(TransactionStatus.Disputed))
        {
            return Result.Failure(TransactionErrors.InvalidStatusTransition(Status.Name, TransactionStatus.Disputed.Name));
        }

        Status = TransactionStatus.Disputed;

        RaiseDomainEvent(new TransactionDisputedEvent
        {
            TransactionId = Id,
            Reason = reason,
            DisputedAtUtc = DateTime.UtcNow
        });

        return Result.Success();
    }

    /// <summary>
    /// Applies an event to reconstitute the aggregate state.
    /// Used by the event store for loading aggregates.
    /// </summary>
    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TransactionInitiatedEvent initiated:
                ApplyInitiated(initiated);
                break;
            case TransactionAuthorizedEvent authorized:
                ApplyAuthorized(authorized);
                break;
            case TransactionCompletedEvent completed:
                ApplyCompleted(completed);
                break;
            case TransactionFailedEvent failed:
                ApplyFailed(failed);
                break;
            case TransactionReversedEvent reversed:
                ApplyReversed(reversed);
                break;
            case TransactionDisputedEvent disputed:
                ApplyDisputed(disputed);
                break;
        }

        Version++;
    }

    private void ApplyInitiated(TransactionInitiatedEvent @event)
    {
        Id = @event.TransactionId;
        SourceAccountId = @event.SourceAccountId;
        DestinationAccountId = @event.DestinationAccountId;
        Amount = @event.Amount;
        Reference = @event.Reference;
        Status = TransactionStatus.Initiated;
        InitiatedAtUtc = @event.OccurredOnUtc;
    }

    private void ApplyAuthorized(TransactionAuthorizedEvent @event)
    {
        Status = TransactionStatus.Authorized;
        AuthorizationCode = @event.AuthorizationCode;
    }

    private void ApplyCompleted(TransactionCompletedEvent @event)
    {
        Status = TransactionStatus.Completed;
        CompletedAtUtc = @event.CompletedAtUtc;
    }

    private void ApplyFailed(TransactionFailedEvent @event)
    {
        Status = TransactionStatus.Failed;
        FailureReason = @event.FailureReason;
    }

    private void ApplyReversed(TransactionReversedEvent @event)
    {
        Status = TransactionStatus.Reversed;
    }

    private void ApplyDisputed(TransactionDisputedEvent @event)
    {
        Status = TransactionStatus.Disputed;
    }

    /// <summary>
    /// Reconstitutes an aggregate from a stream of events.
    /// </summary>
    public static TransactionAggregate LoadFromHistory(System.Collections.Generic.IEnumerable<IDomainEvent> events)
    {
        TransactionAggregate aggregate = new();
        foreach (IDomainEvent @event in events)
        {
            aggregate.Apply(@event);
        }

        return aggregate;
    }
}
