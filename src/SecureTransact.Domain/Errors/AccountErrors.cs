using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Domain.Errors;

/// <summary>
/// Domain errors related to accounts.
/// </summary>
public static class AccountErrors
{
    /// <summary>
    /// Account was not found.
    /// </summary>
    public static DomainError NotFound(string accountId) => DomainError.NotFound(
        "Account.NotFound",
        $"Account with ID '{accountId}' was not found.");

    /// <summary>
    /// Account is inactive.
    /// </summary>
    public static readonly DomainError Inactive = DomainError.Failure(
        "Account.Inactive",
        "Account is inactive and cannot be used for transactions.");

    /// <summary>
    /// Account is frozen.
    /// </summary>
    public static readonly DomainError Frozen = DomainError.Failure(
        "Account.Frozen",
        "Account is frozen and cannot be used for transactions.");

    /// <summary>
    /// Account has insufficient balance.
    /// </summary>
    public static readonly DomainError InsufficientBalance = DomainError.Failure(
        "Account.InsufficientBalance",
        "Account has insufficient balance for this operation.");

    /// <summary>
    /// Daily transaction limit exceeded.
    /// </summary>
    public static readonly DomainError DailyLimitExceeded = DomainError.Failure(
        "Account.DailyLimitExceeded",
        "Daily transaction limit has been exceeded.");

    /// <summary>
    /// Single transaction limit exceeded.
    /// </summary>
    public static readonly DomainError TransactionLimitExceeded = DomainError.Failure(
        "Account.TransactionLimitExceeded",
        "Single transaction limit has been exceeded.");
}
