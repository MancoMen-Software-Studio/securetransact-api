using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecureTransact.Api.Contracts;
using SecureTransact.Infrastructure.EventStore;

namespace SecureTransact.Api.Middleware;

/// <summary>
/// Middleware for global exception handling.
/// </summary>
public sealed partial class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string traceId = context.TraceIdentifier;

        (int statusCode, string errorCode, string message) = exception switch
        {
            EventChainIntegrityException => (
                StatusCodes.Status500InternalServerError,
                "EventStore.IntegrityViolation",
                "Event chain integrity violation detected. This incident has been logged."),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Request.InvalidArgument",
                argEx.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Auth.Unauthorized",
                "You are not authorized to access this resource."),

            InvalidOperationException opEx => (
                StatusCodes.Status400BadRequest,
                "Operation.Invalid",
                opEx.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal.Error",
                "An unexpected error occurred. Please try again later.")
        };

        if (statusCode >= 500)
        {
            LogUnhandledException(_logger, traceId, errorCode, exception);
        }
        else
        {
            LogHandledException(_logger, traceId, errorCode, exception);
        }

        ApiErrorResponse errorResponse = new()
        {
            Code = errorCode,
            Message = message,
            TraceId = traceId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred. TraceId: {TraceId}, ErrorCode: {ErrorCode}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string traceId,
        string errorCode,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Handled exception occurred. TraceId: {TraceId}, ErrorCode: {ErrorCode}")]
    private static partial void LogHandledException(
        ILogger logger,
        string traceId,
        string errorCode,
        Exception exception);
}
