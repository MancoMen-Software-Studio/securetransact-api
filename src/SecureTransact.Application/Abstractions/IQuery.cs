using MediatR;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Abstractions;

/// <summary>
/// Marker interface for queries (read operations).
/// Queries always return a value wrapped in a Result.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
