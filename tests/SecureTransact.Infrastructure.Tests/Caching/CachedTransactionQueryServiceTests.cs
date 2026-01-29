using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;
using SecureTransact.Infrastructure.Caching;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Caching;

public sealed class CachedTransactionQueryServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITransactionQueryService _innerService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedTransactionQueryService> _logger;
    private readonly CachedTransactionQueryService _sut;

    public CachedTransactionQueryServiceTests()
    {
        _innerService = Substitute.For<ITransactionQueryService>();
        _cache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<CachedTransactionQueryService>>();
        _sut = new CachedTransactionQueryService(_innerService, _cache, _logger);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldCallInnerService_WhenCacheIsEmpty()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        List<TransactionSummary> expected = new()
        {
            new TransactionSummary
            {
                TransactionId = Guid.NewGuid(),
                Type = "Debit",
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 100m,
                Currency = "USD",
                Status = "Completed",
                InitiatedAtUtc = DateTime.UtcNow
            }
        };

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _innerService.GetHistoryAsync(accountId, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((expected, 1));

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _sut.GetHistoryAsync(accountId, null, null, 1, 10);

        // Assert
        transactions.Should().HaveCount(1);
        totalCount.Should().Be(1);
        await _innerService.Received(1).GetHistoryAsync(accountId, null, null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnCachedData_WhenCacheIsPopulated()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        List<TransactionSummary> cached = new()
        {
            new TransactionSummary
            {
                TransactionId = Guid.NewGuid(),
                Type = "Credit",
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 200m,
                Currency = "EUR",
                Status = "Initiated",
                InitiatedAtUtc = DateTime.UtcNow
            }
        };

        CachedResult cacheEntry = new()
        {
            Transactions = cached,
            TotalCount = 1
        };

        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(cacheEntry, JsonOptions);

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(serialized);

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _sut.GetHistoryAsync(accountId, null, null, 1, 10);

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].Currency.Should().Be("EUR");
        totalCount.Should().Be(1);
        await _innerService.DidNotReceive().GetHistoryAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFallbackToInner_WhenCacheThrows()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        List<TransactionSummary> expected = new()
        {
            new TransactionSummary
            {
                TransactionId = Guid.NewGuid(),
                Type = "Debit",
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 300m,
                Currency = "USD",
                Status = "Completed",
                InitiatedAtUtc = DateTime.UtcNow
            }
        };

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Redis unavailable"));

        _innerService.GetHistoryAsync(accountId, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((expected, 1));

        // Act
        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _sut.GetHistoryAsync(accountId, null, null, 1, 10);

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().Be(300m);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldWriteToCache_AfterFetchingFromInner()
    {
        // Arrange
        Guid accountId = Guid.NewGuid();
        List<TransactionSummary> expected = new()
        {
            new TransactionSummary
            {
                TransactionId = Guid.NewGuid(),
                Type = "Debit",
                CounterpartyAccountId = Guid.NewGuid(),
                Amount = 100m,
                Currency = "USD",
                Status = "Completed",
                InitiatedAtUtc = DateTime.UtcNow
            }
        };

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _innerService.GetHistoryAsync(accountId, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((expected, 1));

        // Act
        await _sut.GetHistoryAsync(accountId, null, null, 1, 10);

        // Assert
        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Mirror DTO for deserializing cached data in tests.
    /// </summary>
    private sealed class CachedResult
    {
        public IReadOnlyList<TransactionSummary> Transactions { get; init; } =
            Array.Empty<TransactionSummary>();

        public int TotalCount { get; init; }
    }
}
