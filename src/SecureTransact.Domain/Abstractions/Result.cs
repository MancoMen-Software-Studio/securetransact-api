using System;
using System.Diagnostics.CodeAnalysis;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, DomainError error)
    {
        if (isSuccess && error != DomainError.None)
        {
            throw new ArgumentException("Success result cannot have an error.", nameof(error));
        }

        if (!isSuccess && error == DomainError.None)
        {
            throw new ArgumentException("Failure result must have an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed.
    /// </summary>
    public DomainError Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, DomainError.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(DomainError error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, DomainError.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<TValue> Failure<TValue>(DomainError error) => new(default!, false, error);

    /// <summary>
    /// Creates a result based on a nullable value. Returns failure if value is null.
    /// </summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(DomainError.NullValue);
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccess, DomainError error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value if the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value of a failed result.</exception>
    public TValue Value
    {
        get
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException("Cannot access value of a failed result.");
            }

            return _value;
        }
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue? value) => Create(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result<TValue>(DomainError error) => Failure<TValue>(error);

    /// <summary>
    /// Maps the value to a new type if the result is successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper) =>
        IsSuccess ? Success(mapper(_value)) : Failure<TNew>(Error);

    /// <summary>
    /// Binds to another result if this result is successful.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> binder) =>
        IsSuccess ? binder(_value) : Failure<TNew>(Error);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<TValue> Tap(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(_value);
        }

        return this;
    }

    /// <summary>
    /// Returns the value if successful, or the default value if failed.
    /// </summary>
    public TValue GetValueOrDefault(TValue defaultValue = default!) =>
        IsSuccess ? _value : defaultValue;

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<DomainError, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value) : onFailure(Error);
}
