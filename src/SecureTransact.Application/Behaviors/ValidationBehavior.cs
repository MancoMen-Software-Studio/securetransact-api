using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        return CreateValidationResult<TResponse>(failures);
    }

    private static TResult CreateValidationResult<TResult>(List<ValidationFailure> failures)
        where TResult : Result
    {
        string errorDescription = string.Join("; ", failures.Select(f => f.ErrorMessage));

        DomainError error = DomainError.Validation(
            "Validation.Failed",
            errorDescription);

        if (typeof(TResult) == typeof(Result))
        {
            return (TResult)Result.Failure(error);
        }

        System.Type resultType = typeof(TResult);
        System.Type valueType = resultType.GetGenericArguments()[0];
        System.Reflection.MethodInfo failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(DomainError)])!
            .MakeGenericMethod(valueType);

        return (TResult)failureMethod.Invoke(null, [error])!;
    }
}
