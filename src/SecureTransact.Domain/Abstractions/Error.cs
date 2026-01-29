using System;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Represents a domain error with a code and description.
/// </summary>
public sealed record DomainError
{
    /// <summary>
    /// Represents no error (success case).
    /// </summary>
    public static readonly DomainError None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Represents a null value error.
    /// </summary>
    public static readonly DomainError NullValue = new(
        "General.NullValue",
        "A null value was provided.",
        ErrorType.Validation);

    /// <summary>
    /// Gets the unique error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the type of error.
    /// </summary>
    public ErrorType Type { get; }

    private DomainError(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static DomainError Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static DomainError NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static DomainError Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static DomainError Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static DomainError Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    /// <summary>
    /// Creates a failure error (general business rule violation).
    /// </summary>
    public static DomainError Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);
}

/// <summary>
/// Defines the types of errors that can occur in the domain.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error (success).
    /// </summary>
    None = 0,

    /// <summary>
    /// Validation error (invalid input).
    /// </summary>
    Validation = 1,

    /// <summary>
    /// Resource not found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Conflict with current state.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// Authentication required.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Access denied.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// General business rule failure.
    /// </summary>
    Failure = 6
}
