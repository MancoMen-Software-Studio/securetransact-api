using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Application.Queries.GetTransactionHistory;
using SecureTransact.Domain.Abstractions;
using Xunit;

namespace SecureTransact.Application.Tests.Queries.GetTransactionHistory;

public sealed class GetTransactionHistoryQueryHandlerTests
{
    private readonly ITransactionQueryService _queryService;
    private readonly GetTransactionHistoryQueryHandler _handler;

    public GetTransactionHistoryQueryHandlerTests()
    {
        _queryService = Substitute.For<ITransactionQueryService>();
        _handler = new GetTransactionHistoryQueryHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ShouldReturnHistory_WithTransactions()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        List<TransactionSummary> transactions = CreateTransactionSummaries(3);

        _queryService
            .GetHistoryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((transactions, 3));

        GetTransactionHistoryQuery query = new()
        {
            AccountId = accountId,
            Page = 1,
            PageSize = 10
        };

        // Act
        Result<TransactionHistoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccountId.Should().Be(accountId);
        result.Value.Transactions.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoTransactionsExist()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();

        _queryService
            .GetHistoryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<TransactionSummary>(), 0));

        GetTransactionHistoryQuery query = new()
        {
            AccountId = accountId,
            Page = 1,
            PageSize = 10
        };

        // Act
        Result<TransactionHistoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Transactions.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPassDateFiltersToQueryService()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        DateTime fromDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toDate = new(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        _queryService
            .GetHistoryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<TransactionSummary>(), 0));

        GetTransactionHistoryQuery query = new()
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 10
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _queryService.Received(1).GetHistoryAsync(
            accountId,
            fromDate,
            toDate,
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassPaginationParametersToQueryService()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();

        _queryService
            .GetHistoryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<TransactionSummary>(), 0));

        GetTransactionHistoryQuery query = new()
        {
            AccountId = accountId,
            Page = 5,
            PageSize = 25
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _queryService.Received(1).GetHistoryAsync(
            accountId,
            null,
            null,
            5,
            25,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAlwaysReturnSuccess()
    {
        // Arrange
        _queryService
            .GetHistoryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<TransactionSummary>(), 0));

        GetTransactionHistoryQuery query = new()
        {
            AccountId = Guid.NewGuid(),
            Page = 1,
            PageSize = 10
        };

        // Act
        Result<TransactionHistoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    private static List<TransactionSummary> CreateTransactionSummaries(int count)
    {
        List<TransactionSummary> summaries = new();

        for (int i = 0; i < count; i++)
        {
            summaries.Add(new TransactionSummary
            {
                TransactionId = Guid.NewGuid(),
                Type = i % 2 == 0 ? "Debit" : "Credit",
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 100m * (i + 1),
                Currency = "USD",
                Status = "Completed",
                InitiatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        return summaries;
    }
}
