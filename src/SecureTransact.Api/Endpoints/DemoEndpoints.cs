using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.Cryptography;
using SecureTransact.Infrastructure.EventStore;
using SecureTransact.Infrastructure.Persistence.Contexts;

namespace SecureTransact.Api.Endpoints;

/// <summary>
/// Demo endpoints for showcasing the application features.
/// These endpoints are for demonstration purposes only and should be disabled in production.
/// </summary>
public static class DemoEndpoints
{
    private static readonly string[] SecurityFeatures =
    [
        "AES-256-GCM encryption for event data",
        "HMAC-SHA512 hash chain linking events",
        "Tamper detection via chain verification",
        "Immutable append-only storage"
    ];

    private static readonly string[] ArchitectureLayers =
    [
        "Domain - Business logic and domain events",
        "Application - Commands, Queries, Validators",
        "Infrastructure - Event Store, Crypto, Persistence",
        "API - RESTful endpoints with JWT auth"
    ];

    /// <summary>
    /// Maps all demo endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/demo")
            .WithTags("Demo")
            .AllowAnonymous();

        group.MapPost("/simulate", SimulateTransactions)
            .WithName("SimulateTransactions")
            .WithSummary("Simulate multiple transactions for demonstration")
            .WithDescription("Creates multiple transactions with different statuses to showcase the system.");

        group.MapGet("/stats", GetStatistics)
            .WithName("GetDemoStatistics")
            .WithSummary("Get transaction statistics")
            .WithDescription("Returns aggregated statistics about all transactions in the system.");

        group.MapGet("/events", GetEventStore)
            .WithName("GetEventStore")
            .WithSummary("View event store contents")
            .WithDescription("Returns all events in the event store showing the hash chain.");

        group.MapGet("/events/{aggregateId:guid}", GetAggregateEvents)
            .WithName("GetAggregateEvents")
            .WithSummary("View events for a specific aggregate")
            .WithDescription("Returns all events for a specific transaction showing state reconstruction.");

        group.MapGet("/verify/{aggregateId:guid}", VerifyIntegrity)
            .WithName("VerifyIntegrity")
            .WithSummary("Verify hash chain integrity")
            .WithDescription("Verifies the cryptographic hash chain for a specific transaction.");

        group.MapGet("/verify-all", VerifyAllIntegrity)
            .WithName("VerifyAllIntegrity")
            .WithSummary("Verify all hash chains")
            .WithDescription("Verifies the cryptographic integrity of all transactions.");

        group.MapPost("/tamper/{aggregateId:guid}", SimulateTamper)
            .WithName("SimulateTamper")
            .WithSummary("Simulate tampering (for demo)")
            .WithDescription("Demonstrates how tampering is detected by modifying event data.");

        group.MapGet("/showcase", GetShowcase)
            .WithName("GetShowcase")
            .WithSummary("Complete showcase of all features")
            .WithDescription("Returns a comprehensive view of the system for video demonstrations.");

        group.MapDelete("/reset", ResetDemo)
            .WithName("ResetDemo")
            .WithSummary("Reset demo data")
            .WithDescription("Clears all demo data to start fresh.");

        return app;
    }

    private static async Task<IResult> SimulateTransactions(
        IServiceProvider serviceProvider,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        using IServiceScope scope = serviceProvider.CreateScope();

        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        ICryptoService cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
        IEventSerializer serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();

        AccountId[] accounts = Enumerable.Range(0, 5).Select(_ => AccountId.New()).ToArray();
        Currency[] currencies = [Currency.USD, Currency.EUR, Currency.GBP];
        string[] references = ["Payment", "Transfer", "Refund", "Invoice", "Subscription", "Purchase"];
        Random random = new();

        List<SimulatedTransaction> transactions = [];
        int completed = 0, failed = 0, reversed = 0;

        for (int i = 0; i < count; i++)
        {
            AccountId source = accounts[random.Next(accounts.Length)];
            AccountId dest;
            do { dest = accounts[random.Next(accounts.Length)]; } while (dest == source);

            Currency currency = currencies[random.Next(currencies.Length)];
            decimal amount = Math.Round((decimal)(random.NextDouble() * 1000 + 10), 2);
            string reference = $"{references[random.Next(references.Length)]}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            Result<Money> moneyResult = Money.Create(amount, currency);
            if (moneyResult.IsFailure) continue;

            Result<TransactionAggregate> txResult = TransactionAggregate.Create(source, dest, moneyResult.Value, reference);
            if (txResult.IsFailure) continue;

            TransactionAggregate tx = txResult.Value;

            int outcome = random.Next(100);
            string status;

            if (outcome < 70)
            {
                tx.Authorize($"AUTH-{Guid.NewGuid().ToString().Substring(0, 8).ToUpperInvariant()}");
                tx.Complete();
                status = "Completed";
                completed++;
            }
            else if (outcome < 85)
            {
                tx.Fail("INSUFFICIENT_FUNDS", "Simulated failure for demo");
                status = "Failed";
                failed++;
            }
            else
            {
                tx.Authorize($"AUTH-{Guid.NewGuid().ToString().Substring(0, 8).ToUpperInvariant()}");
                tx.Complete();
                tx.Reverse("Customer requested reversal");
                status = "Reversed";
                reversed++;
            }

            byte[]? previousHash = null;
            int version = 0;

            foreach (IDomainEvent evt in tx.GetDomainEvents())
            {
                version++;
                byte[] serializedData = serializer.Serialize(evt);
                byte[] encryptedData = cryptoService.Encrypt(serializedData);
                byte[] chainHash = cryptoService.ComputeChainHash(previousHash, serializedData);

                StoredEvent storedEvent = new()
                {
                    Id = evt.EventId,
                    AggregateId = tx.Id.Value,
                    EventType = serializer.GetEventTypeName(evt),
                    EventData = encryptedData,
                    Version = version,
                    OccurredAtUtc = evt.OccurredOnUtc,
                    ChainHash = chainHash,
                    PreviousHash = previousHash
                };

                context.Events.Add(storedEvent);
                previousHash = chainHash;
            }

            transactions.Add(new SimulatedTransaction
            {
                TransactionId = tx.Id.Value,
                Amount = amount,
                Currency = currency.Code,
                Status = status,
                EventCount = tx.GetDomainEvents().Count
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        return Results.Ok(new
        {
            Message = $"Successfully simulated {count} transactions",
            Duration = $"{stopwatch.ElapsedMilliseconds}ms",
            Summary = new
            {
                Total = count,
                Completed = completed,
                Failed = failed,
                Reversed = reversed
            },
            Transactions = transactions.Take(10)
        });
    }

    private static async Task<IResult> GetStatistics(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        int totalEvents = await context.Events.CountAsync(cancellationToken);
        int totalAggregates = await context.Events
            .Select(e => e.AggregateId)
            .Distinct()
            .CountAsync(cancellationToken);

        var eventsByType = await context.Events
            .GroupBy(e => e.EventType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        long totalDataSize = await context.Events.SumAsync(e => (long)e.EventData.Length, cancellationToken);

        return Results.Ok(new
        {
            EventStore = new
            {
                TotalEvents = totalEvents,
                TotalAggregates = totalAggregates,
                TotalDataSize = $"{totalDataSize / 1024.0:F2} KB",
                EventsByType = eventsByType.Select(e => new
                {
                    EventType = GetShortTypeName(e.Type),
                    e.Count
                })
            },
            DatabaseInfo = new
            {
                Provider = "PostgreSQL",
                Schema = "event_store",
                Table = "events"
            }
        });
    }

    private static async Task<IResult> GetEventStore(
        IServiceProvider serviceProvider,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        var rawEvents = await context.Events
            .OrderByDescending(e => e.GlobalSequence)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var events = rawEvents.Select(e => new
        {
            e.Id,
            e.AggregateId,
            EventType = GetShortTypeName(e.EventType),
            e.Version,
            e.OccurredAtUtc,
            ChainHash = TruncateHex(Convert.ToHexString(e.ChainHash), 16),
            PreviousHash = e.PreviousHash != null ? TruncateHex(Convert.ToHexString(e.PreviousHash), 16) : "GENESIS",
            e.GlobalSequence,
            DataSize = e.EventData.Length
        }).ToList();

        return Results.Ok(new
        {
            Description = "Event Store with Cryptographic Hash Chain",
            SecurityFeatures,
            TotalShown = events.Count,
            Events = events
        });
    }

    private static async Task<IResult> GetAggregateEvents(
        Guid aggregateId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        ICryptoService cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
        IEventSerializer serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();

        var storedEvents = await context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (storedEvents.Count == 0)
        {
            return Results.NotFound(new { Message = $"No events found for aggregate {aggregateId}" });
        }

        List<object> eventDetails = [];
        byte[]? previousHash = null;
        bool chainValid = true;

        foreach (StoredEvent evt in storedEvents)
        {
            byte[] decryptedData = cryptoService.Decrypt(evt.EventData);
            byte[] expectedHash = cryptoService.ComputeChainHash(previousHash, decryptedData);
            bool hashValid = expectedHash.SequenceEqual(evt.ChainHash);

            if (!hashValid) chainValid = false;

            IDomainEvent domainEvent = serializer.Deserialize(decryptedData, evt.EventType);

            eventDetails.Add(new
            {
                Version = evt.Version,
                EventType = GetShortTypeName(evt.EventType),
                OccurredAt = evt.OccurredAtUtc,
                ChainHash = TruncateHex(Convert.ToHexString(evt.ChainHash), 32),
                PreviousHash = evt.PreviousHash != null ? TruncateHex(Convert.ToHexString(evt.PreviousHash), 32) : "GENESIS",
                HashValid = hashValid,
                EventData = domainEvent
            });

            previousHash = evt.ChainHash;
        }

        return Results.Ok(new
        {
            AggregateId = aggregateId,
            TotalEvents = storedEvents.Count,
            ChainIntegrity = chainValid ? "VALID ✓" : "COMPROMISED ✗",
            Events = eventDetails
        });
    }

    private static async Task<IResult> VerifyIntegrity(
        Guid aggregateId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IEventStore eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        Stopwatch stopwatch = Stopwatch.StartNew();
        Result<bool> result = await eventStore.VerifyHashChainAsync(aggregateId, cancellationToken);
        stopwatch.Stop();

        bool isValid = result.IsSuccess && result.Value;

        return Results.Ok(new
        {
            AggregateId = aggregateId,
            Integrity = isValid ? "VERIFIED ✓" : "COMPROMISED ✗",
            VerificationTime = $"{stopwatch.ElapsedMilliseconds}ms",
            Details = new
            {
                Algorithm = "HMAC-SHA512",
                ChainType = "Hash-linked events",
                Description = isValid
                    ? "All events are cryptographically linked and unmodified"
                    : "Chain integrity violation detected - data may have been tampered"
            }
        });
    }

    private static async Task<IResult> VerifyAllIntegrity(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        IEventStore eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        Stopwatch stopwatch = Stopwatch.StartNew();

        var aggregateIds = await context.Events
            .Select(e => e.AggregateId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int verified = 0, compromised = 0;
        List<Guid> compromisedAggregates = [];

        foreach (Guid id in aggregateIds)
        {
            Result<bool> result = await eventStore.VerifyHashChainAsync(id, cancellationToken);
            if (result.IsSuccess && result.Value)
            {
                verified++;
            }
            else
            {
                compromised++;
                compromisedAggregates.Add(id);
            }
        }

        stopwatch.Stop();

        return Results.Ok(new
        {
            TotalAggregates = aggregateIds.Count,
            Verified = verified,
            Compromised = compromised,
            OverallIntegrity = compromised == 0 ? "ALL VERIFIED ✓" : "INTEGRITY ISSUES DETECTED ✗",
            VerificationTime = $"{stopwatch.ElapsedMilliseconds}ms",
            CompromisedAggregates = compromisedAggregates.Take(10)
        });
    }

    private static async Task<IResult> SimulateTamper(
        Guid aggregateId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        IEventStore eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        Result<bool> beforeResult = await eventStore.VerifyHashChainAsync(aggregateId, cancellationToken);
        bool beforeValid = beforeResult.IsSuccess && beforeResult.Value;

        StoredEvent? eventToTamper = await context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (eventToTamper == null)
        {
            return Results.NotFound(new { Message = "No events found to tamper with" });
        }

        byte[] originalData = eventToTamper.EventData.ToArray();
        eventToTamper.EventData[0] ^= 0xFF;

        await context.SaveChangesAsync(cancellationToken);

        Result<bool> afterResult = await eventStore.VerifyHashChainAsync(aggregateId, cancellationToken);
        bool afterValid = afterResult.IsSuccess && afterResult.Value;

        eventToTamper.EventData = originalData;
        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            Message = "Tamper simulation completed",
            AggregateId = aggregateId,
            TamperedEventVersion = eventToTamper.Version,
            Results = new
            {
                BeforeTamper = beforeValid ? "VALID ✓" : "ALREADY COMPROMISED",
                AfterTamper = afterValid ? "NOT DETECTED (unexpected!)" : "TAMPERING DETECTED ✓",
                AfterRestore = "Data restored to original state"
            },
            Explanation = "This demonstrates how the hash chain detects any modification to event data. " +
                         "Even changing a single byte causes the hash verification to fail."
        });
    }

    private static async Task<IResult> GetShowcase(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        int totalEvents = await context.Events.CountAsync(cancellationToken);
        int totalAggregates = await context.Events.Select(e => e.AggregateId).Distinct().CountAsync(cancellationToken);

        var latestTransactions = await context.Events
            .GroupBy(e => e.AggregateId)
            .Select(g => new
            {
                AggregateId = g.Key,
                EventCount = g.Count(),
                LastEvent = g.Max(e => e.OccurredAtUtc)
            })
            .OrderByDescending(x => x.LastEvent)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            Title = "SecureTransact API - Demo Showcase",
            Description = "Enterprise-grade secure transaction processing with event sourcing",
            Architecture = new
            {
                Pattern = "Clean Architecture + CQRS + Event Sourcing",
                Layers = ArchitectureLayers
            },
            Security = new
            {
                Encryption = "AES-256-GCM for event data at rest",
                Integrity = "HMAC-SHA512 hash chain for tamper detection",
                Authentication = "JWT Bearer tokens",
                AuditTrail = "Complete event sourcing for full history"
            },
            Statistics = new
            {
                TotalEvents = totalEvents,
                TotalTransactions = totalAggregates,
                RecentTransactions = latestTransactions
            },
            DemoEndpoints = new
            {
                SimulateTransactions = "POST /api/demo/simulate?count=N",
                ViewStatistics = "GET /api/demo/stats",
                ViewEventStore = "GET /api/demo/events",
                ViewAggregateEvents = "GET /api/demo/events/{aggregateId}",
                VerifyIntegrity = "GET /api/demo/verify/{aggregateId}",
                VerifyAll = "GET /api/demo/verify-all",
                SimulateTamper = "POST /api/demo/tamper/{aggregateId}",
                ResetData = "DELETE /api/demo/reset"
            },
            ExternalTools = new
            {
                Database = "pgAdmin at http://localhost:5050 (admin@securetransact.local / admin123)",
                Logs = "Seq at http://localhost:8081",
                API = "OpenAPI docs at http://localhost:5000/openapi/v1.json"
            }
        });
    }

    private static async Task<IResult> ResetDemo(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        int deletedCount = await context.Events.ExecuteDeleteAsync(cancellationToken);

        return Results.Ok(new
        {
            Message = "Demo data reset complete",
            DeletedEvents = deletedCount
        });
    }

    private static string GetShortTypeName(string fullTypeName)
    {
        string[] parts = fullTypeName.Split(',');
        if (parts.Length > 0)
        {
            string[] nameParts = parts[0].Split('.');
            return nameParts.Length > 0 ? nameParts[nameParts.Length - 1] : fullTypeName;
        }
        return fullTypeName;
    }

    private static string TruncateHex(string hex, int length)
    {
        if (hex.Length <= length)
            return hex;
        return string.Concat(hex.AsSpan(0, length), "...");
    }

    private sealed record SimulatedTransaction
    {
        public Guid TransactionId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int EventCount { get; init; }
    }
}
