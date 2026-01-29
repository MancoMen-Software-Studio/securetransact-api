using MediatR;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Abstractions;

/// <summary>
/// Handler for queries.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
