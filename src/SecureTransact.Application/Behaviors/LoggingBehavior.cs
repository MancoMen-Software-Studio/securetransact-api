using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request handling.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        LogHandlingRequest(_logger, requestName);

        Stopwatch stopwatch = Stopwatch.StartNew();

        TResponse response = await next();

        stopwatch.Stop();

        if (response.IsFailure)
        {
            LogRequestFailed(
                _logger,
                requestName,
                response.Error.Code,
                response.Error.Description,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            LogRequestSucceeded(_logger, requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Handling {RequestName}")]
    private static partial void LogHandlingRequest(ILogger logger, string requestName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Request {RequestName} failed with error {ErrorCode}: {ErrorDescription} in {ElapsedMs}ms")]
    private static partial void LogRequestFailed(
        ILogger logger,
        string requestName,
        string errorCode,
        string errorDescription,
        long elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Handled {RequestName} successfully in {ElapsedMs}ms")]
    private static partial void LogRequestSucceeded(ILogger logger, string requestName, long elapsedMs);
}
