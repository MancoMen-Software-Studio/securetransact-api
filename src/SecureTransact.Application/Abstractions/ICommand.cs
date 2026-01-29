using MediatR;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Abstractions;

/// <summary>
/// Marker interface for commands (write operations).
/// Commands return a Result indicating success or failure.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Command that returns a value on success.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
