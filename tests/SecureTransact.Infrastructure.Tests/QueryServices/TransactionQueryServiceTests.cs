using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureTransact.Application.DTOs;
using SecureTransact.Infrastructure.Persistence.Contexts;
using SecureTransact.Infrastructure.Persistence.ReadModels;
using SecureTransact.Infrastructure.QueryServices;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.QueryServices;

public sealed class TransactionQueryServiceTests : IDisposable
{
    private readonly TransactionDbContext _context;
    private readonly TransactionQueryService _queryService;
    private readonly Guid _accountId = Guid.NewGuid();

    public TransactionQueryServiceTests()
    {
        DbContextOptions<TransactionDbContext> options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: $"QueryService_{Guid.NewGuid()}")
            .Options;

        _context = new TransactionDbContext(options);
        _queryService = new TransactionQueryService(_context);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnEmpty_WhenNoTransactionsExist()
    {
        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 10, CancellationToken.None);

        // Assert
        transactions.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnTransactions_ForSourceAccount()
    {
        // Arrange
        Guid destId = Guid.NewGuid();
        await SeedTransaction(_accountId, destId, 100m, "USD", "Completed");

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 10, CancellationToken.None);

        // Assert
        transactions.Should().HaveCount(1);
        totalCount.Should().Be(1);
        transactions[0].Type.Should().Be("Debit");
        transactions[0].CounterpartyAccountId.Should().Be(destId);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnTransactions_ForDestinationAccount()
    {
        // Arrange
        Guid sourceId = Guid.NewGuid();
        await SeedTransaction(sourceId, _accountId, 200m, "EUR", "Completed");

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 10, CancellationToken.None);

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].Type.Should().Be("Credit");
        transactions[0].CounterpartyAccountId.Should().Be(sourceId);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByDateRange()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedTransaction(_accountId, Guid.NewGuid(), 100m, "USD", "Completed", now.AddDays(-5));
        await SeedTransaction(_accountId, Guid.NewGuid(), 200m, "USD", "Completed", now.AddDays(-1));
        await SeedTransaction(_accountId, Guid.NewGuid(), 300m, "USD", "Completed", now.AddDays(1));

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, now.AddDays(-3), now, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldPaginate()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await SeedTransaction(_accountId, Guid.NewGuid(), (i + 1) * 100m, "USD", "Completed",
                DateTime.UtcNow.AddMinutes(-i));
        }

        // Act — page 1 with size 2
        (IReadOnlyList<TransactionSummary> page1, int totalCount1) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 2, CancellationToken.None);

        // Act — page 2 with size 2
        (IReadOnlyList<TransactionSummary> page2, int totalCount2) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 2, 2, CancellationToken.None);

        // Assert
        totalCount1.Should().Be(5);
        totalCount2.Should().Be(5);
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldOrderByInitiatedAtDescending()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedTransaction(_accountId, Guid.NewGuid(), 100m, "USD", "Completed", now.AddHours(-2));
        await SeedTransaction(_accountId, Guid.NewGuid(), 200m, "USD", "Completed", now.AddHours(-1));
        await SeedTransaction(_accountId, Guid.NewGuid(), 300m, "USD", "Completed", now);

        // Act
        (IReadOnlyList<TransactionSummary> transactions, _) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 10, CancellationToken.None);

        // Assert
        transactions[0].Amount.Should().Be(300m);
        transactions[1].Amount.Should().Be(200m);
        transactions[2].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldMapAllFields()
    {
        // Arrange
        Guid destId = Guid.NewGuid();
        DateTime initiatedAt = DateTime.UtcNow;
        await SeedTransaction(_accountId, destId, 999.99m, "GBP", "Authorized", initiatedAt);

        // Act
        (IReadOnlyList<TransactionSummary> transactions, _) =
            await _queryService.GetHistoryAsync(_accountId, null, null, 1, 10, CancellationToken.None);

        // Assert
        TransactionSummary summary = transactions[0];
        summary.Amount.Should().Be(999.99m);
        summary.Currency.Should().Be("GBP");
        summary.Status.Should().Be("Authorized");
        summary.InitiatedAtUtc.Should().Be(initiatedAt);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByFromDateOnly()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedTransaction(_accountId, Guid.NewGuid(), 100m, "USD", "Completed", now.AddDays(-10));
        await SeedTransaction(_accountId, Guid.NewGuid(), 200m, "USD", "Completed", now);

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, now.AddDays(-5), null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        transactions[0].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByToDateOnly()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedTransaction(_accountId, Guid.NewGuid(), 100m, "USD", "Completed", now.AddDays(-10));
        await SeedTransaction(_accountId, Guid.NewGuid(), 200m, "USD", "Completed", now);

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _queryService.GetHistoryAsync(_accountId, null, now.AddDays(-5), 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        transactions[0].Amount.Should().Be(100m);
    }

    private async Task SeedTransaction(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string currency,
        string status,
        DateTime? initiatedAt = null)
    {
        TransactionReadModel readModel = new()
        {
            Id = Guid.NewGuid(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Currency = currency,
            Status = status,
            InitiatedAtUtc = initiatedAt ?? DateTime.UtcNow,
            Version = 1,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        _context.Transactions.Add(readModel);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
