using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SecureTransact.Api.Contracts;
using SecureTransact.Api.Middleware;
using SecureTransact.Infrastructure.EventStore;
using Xunit;

namespace SecureTransact.Api.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoException()
    {
        // Arrange
        bool nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        DefaultHttpContext context = new();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_ForArgumentException()
    {
        // Arrange
        RequestDelegate next = _ => throw new ArgumentException("Invalid parameter value");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.Code.Should().Be("Request.InvalidArgument");
        response.Message.Should().Be("Invalid parameter value");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_ForUnauthorizedAccessException()
    {
        // Arrange
        RequestDelegate next = _ => throw new UnauthorizedAccessException();
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.Code.Should().Be("Auth.Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_ForInvalidOperationException()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Operation not valid");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.Code.Should().Be("Operation.Invalid");
        response.Message.Should().Be("Operation not valid");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_ForEventChainIntegrityException()
    {
        // Arrange
        RequestDelegate next = _ => throw new EventChainIntegrityException("Chain tampered");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.Code.Should().Be("EventStore.IntegrityViolation");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_ForUnhandledException()
    {
        // Arrange
        RequestDelegate next = _ => throw new TimeoutException("Something went wrong");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.Code.Should().Be("Internal.Error");
        response.Message.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetContentTypeToJson()
    {
        // Arrange
        RequestDelegate next = _ => throw new TimeoutException("test");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, _) = CreateContextWithBody();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeTraceId_InResponse()
    {
        // Arrange
        RequestDelegate next = _ => throw new TimeoutException("test");
        ExceptionHandlingMiddleware middleware = new(next, _logger);
        (DefaultHttpContext context, MemoryStream body) = CreateContextWithBody();
        context.TraceIdentifier = "test-trace-123";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        ApiErrorResponse? response = await DeserializeResponseBody(body);
        response!.TraceId.Should().Be("test-trace-123");
    }

    private static (DefaultHttpContext context, MemoryStream body) CreateContextWithBody()
    {
        MemoryStream body = new();
        DefaultHttpContext context = new();
        context.Response.Body = body;
        return (context, body);
    }

    private static async Task<ApiErrorResponse?> DeserializeResponseBody(MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ApiErrorResponse>(body, JsonOptions);
    }
}
