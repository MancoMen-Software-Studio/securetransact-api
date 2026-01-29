using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Domain.Errors;

/// <summary>
/// Domain errors related to transactions.
/// </summary>
public static class TransactionErrors
{
    /// <summary>
    /// Transaction was not found.
    /// </summary>
    public static DomainError NotFound(string transactionId) => DomainError.NotFound(
        "Transaction.NotFound",
        $"Transaction with ID '{transactionId}' was not found.");

    /// <summary>
    /// Transaction amount is invalid.
    /// </summary>
    public static readonly DomainError InvalidAmount = DomainError.Validation(
        "Transaction.InvalidAmount",
        "Transaction amount must be greater than zero.");

    /// <summary>
    /// Source and destination accounts are the same.
    /// </summary>
    public static readonly DomainError SameAccount = DomainError.Validation(
        "Transaction.SameAccount",
        "Source and destination accounts cannot be the same.");

    /// <summary>
    /// Invalid status transition attempted.
    /// </summary>
    public static DomainError InvalidStatusTransition(string currentStatus, string targetStatus) =>
        DomainError.Conflict(
            "Transaction.InvalidStatusTransition",
            $"Cannot transition from '{currentStatus}' to '{targetStatus}'.");

    /// <summary>
    /// Transaction is already completed.
    /// </summary>
    public static readonly DomainError AlreadyCompleted = DomainError.Conflict(
        "Transaction.AlreadyCompleted",
        "Transaction has already been completed.");

    /// <summary>
    /// Transaction is already reversed.
    /// </summary>
    public static readonly DomainError AlreadyReversed = DomainError.Conflict(
        "Transaction.AlreadyReversed",
        "Transaction has already been reversed.");

    /// <summary>
    /// Transaction is already failed.
    /// </summary>
    public static readonly DomainError AlreadyFailed = DomainError.Conflict(
        "Transaction.AlreadyFailed",
        "Transaction has already failed.");

    /// <summary>
    /// Transaction cannot be reversed.
    /// </summary>
    public static readonly DomainError CannotReverse = DomainError.Conflict(
        "Transaction.CannotReverse",
        "Transaction cannot be reversed in its current state.");

    /// <summary>
    /// Transaction authorization failed.
    /// </summary>
    public static DomainError AuthorizationFailed(string reason) => DomainError.Failure(
        "Transaction.AuthorizationFailed",
        $"Transaction authorization failed: {reason}");

    /// <summary>
    /// Insufficient funds for the transaction.
    /// </summary>
    public static readonly DomainError InsufficientFunds = DomainError.Failure(
        "Transaction.InsufficientFunds",
        "Insufficient funds to complete the transaction.");

    /// <summary>
    /// Transaction has expired.
    /// </summary>
    public static readonly DomainError Expired = DomainError.Failure(
        "Transaction.Expired",
        "Transaction has expired and can no longer be processed.");
}
