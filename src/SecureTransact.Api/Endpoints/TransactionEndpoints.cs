using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecureTransact.Api.Contracts;
using SecureTransact.Application.Commands.ProcessTransaction;
using SecureTransact.Application.Commands.ReverseTransaction;
using SecureTransact.Application.DTOs;
using SecureTransact.Application.Queries.GetTransactionById;
using SecureTransact.Application.Queries.GetTransactionHistory;
using SecureTransact.Domain.Abstractions;

namespace SecureTransact.Api.Endpoints;

/// <summary>
/// Transaction API endpoints.
/// </summary>
public static class TransactionEndpoints
{
    /// <summary>
    /// Maps all transaction endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapPost("/", ProcessTransaction)
            .WithName("ProcessTransaction")
            .WithSummary("Process a new transaction")
            .WithDescription("Creates and processes a new financial transaction between two accounts.")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapGet("/{transactionId:guid}", GetTransaction)
            .WithName("GetTransaction")
            .WithSummary("Get transaction by ID")
            .WithDescription("Retrieves a transaction by its unique identifier.")
            .Produces<TransactionResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{transactionId:guid}/reverse", ReverseTransaction)
            .WithName("ReverseTransaction")
            .WithSummary("Reverse a transaction")
            .WithDescription("Reverses a completed transaction.")
            .Produces<TransactionResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/history/{accountId:guid}", GetTransactionHistory)
            .WithName("GetTransactionHistory")
            .WithSummary("Get transaction history for an account")
            .WithDescription("Retrieves paginated transaction history for a specific account.")
            .Produces<PaginatedResponse<TransactionSummary>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> ProcessTransaction(
        ProcessTransactionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ProcessTransactionCommand command = new()
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference
        };

        Result<TransactionResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapErrorToResult(result.Error);
        }

        return Results.Created($"/api/v1/transactions/{result.Value.TransactionId}", result.Value);
    }

    private static async Task<IResult> GetTransaction(
        Guid transactionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        GetTransactionByIdQuery query = new() { TransactionId = transactionId };

        Result<TransactionResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return MapErrorToResult(result.Error);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ReverseTransaction(
        Guid transactionId,
        ReverseTransactionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ReverseTransactionCommand command = new()
        {
            TransactionId = transactionId,
            Reason = request.Reason
        };

        Result<TransactionResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapErrorToResult(result.Error);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetTransactionHistory(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        GetTransactionHistoryQuery query = new()
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        Result<TransactionHistoryResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return MapErrorToResult(result.Error);
        }

        PaginatedResponse<TransactionSummary> response = new()
        {
            Data = result.Value.Transactions.ToArray(),
            TotalCount = result.Value.TotalCount,
            Page = result.Value.Page,
            PageSize = result.Value.PageSize
        };

        return Results.Ok(response);
    }

    private static IResult MapErrorToResult(DomainError error)
    {
        ApiErrorResponse errorResponse = new()
        {
            Code = error.Code,
            Message = error.Description
        };

        return error.Type switch
        {
            ErrorType.Validation => Results.BadRequest(errorResponse),
            ErrorType.NotFound => Results.NotFound(errorResponse),
            ErrorType.Conflict => Results.Conflict(errorResponse),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.Forbid(),
            _ => Results.Problem(error.Description, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
