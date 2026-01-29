using System;

namespace SecureTransact.Api.Contracts;

/// <summary>
/// Request to process a new transaction.
/// </summary>
public sealed record ProcessTransactionRequest
{
    /// <summary>
    /// Gets or sets the source account identifier.
    /// </summary>
    public required Guid SourceAccountId { get; init; }

    /// <summary>
    /// Gets or sets the destination account identifier.
    /// </summary>
    public required Guid DestinationAccountId { get; init; }

    /// <summary>
    /// Gets or sets the amount to transfer.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the currency code (ISO 4217).
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets or sets an optional reference or description.
    /// </summary>
    public string? Reference { get; init; }
}

/// <summary>
/// Request to reverse a transaction.
/// </summary>
public sealed record ReverseTransactionRequest
{
    /// <summary>
    /// Gets or sets the reason for reversal.
    /// </summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Request to dispute a transaction.
/// </summary>
public sealed record DisputeTransactionRequest
{
    /// <summary>
    /// Gets or sets the reason for dispute.
    /// </summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Standard API error response.
/// </summary>
public sealed record ApiErrorResponse
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the trace identifier for debugging.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets or sets validation errors if applicable.
    /// </summary>
    public ValidationError[]? ValidationErrors { get; init; }
}

/// <summary>
/// Validation error detail.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// Gets or sets the field that failed validation.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Paginated response wrapper.
/// </summary>
public sealed record PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the data items.
    /// </summary>
    public required T[] Data { get; init; }

    /// <summary>
    /// Gets or sets the total count of items.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets whether there are more pages.
    /// </summary>
    public bool HasMore => Page * PageSize < TotalCount;
}
