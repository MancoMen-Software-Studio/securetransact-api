using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SecureTransact.Application.Abstractions;
using SecureTransact.Application.DTOs;

namespace SecureTransact.Infrastructure.Caching;

/// <summary>
/// Caching decorator for ITransactionQueryService using Redis distributed cache.
/// Implements the decorator pattern to add caching without modifying the original service.
/// </summary>
public sealed partial class CachedTransactionQueryService : ITransactionQueryService
{
    private readonly ITransactionQueryService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedTransactionQueryService> _logger;

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CachedTransactionQueryService(
        ITransactionQueryService inner,
        IDistributedCache cache,
        ILogger<CachedTransactionQueryService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets transaction history with Redis caching.
    /// Cache key is composed from account ID, date range, and pagination parameters.
    /// </summary>
    public async Task<(IReadOnlyList<TransactionSummary> Transactions, int TotalCount)> GetHistoryAsync(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = BuildCacheKey(accountId, fromDate, toDate, page, pageSize);

        try
        {
            byte[]? cachedData = await _cache.GetAsync(cacheKey, cancellationToken);

            if (cachedData is not null)
            {
                CachedHistoryResult? cached = JsonSerializer.Deserialize<CachedHistoryResult>(cachedData, JsonOptions);
                if (cached is not null)
                {
                    LogCacheHit(cacheKey);
                    return (cached.Transactions, cached.TotalCount);
                }
            }
        }
        catch (Exception ex)
        {
            LogCacheReadFailure(ex, cacheKey);
        }

        (IReadOnlyList<TransactionSummary> transactions, int totalCount) =
            await _inner.GetHistoryAsync(accountId, fromDate, toDate, page, pageSize, cancellationToken);

        try
        {
            CachedHistoryResult cacheEntry = new()
            {
                Transactions = transactions,
                TotalCount = totalCount
            };

            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(cacheEntry, JsonOptions);

            DistributedCacheEntryOptions cacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = DefaultExpiration,
                SlidingExpiration = SlidingExpiration
            };

            await _cache.SetAsync(cacheKey, serialized, cacheOptions, cancellationToken);

            LogCacheWrite(cacheKey);
        }
        catch (Exception ex)
        {
            LogCacheWriteFailure(ex, cacheKey);
        }

        return (transactions, totalCount);
    }

    private static string BuildCacheKey(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        string from = fromDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "any";
        string to = toDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "any";
        return $"txn:history:{accountId}:{from}:{to}:p{page}:s{pageSize}";
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for transaction history: {CacheKey}")]
    private partial void LogCacheHit(string cacheKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to read from cache for key {CacheKey}. Falling back to database.")]
    private partial void LogCacheReadFailure(Exception ex, string cacheKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached transaction history: {CacheKey}")]
    private partial void LogCacheWrite(string cacheKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write to cache for key {CacheKey}.")]
    private partial void LogCacheWriteFailure(Exception ex, string cacheKey);

    /// <summary>
    /// Internal DTO for serializing cached history results.
    /// </summary>
    private sealed class CachedHistoryResult
    {
        public IReadOnlyList<TransactionSummary> Transactions { get; init; } =
            Array.Empty<TransactionSummary>();

        public int TotalCount { get; init; }
    }
}
