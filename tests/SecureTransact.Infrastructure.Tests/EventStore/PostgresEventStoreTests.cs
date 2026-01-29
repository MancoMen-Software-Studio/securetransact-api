using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Events;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.Cryptography;
using SecureTransact.Infrastructure.EventStore;
using SecureTransact.Infrastructure.Persistence.Contexts;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore;

public sealed class PostgresEventStoreTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly AesGcmCryptoService _cryptoService;
    private readonly EventSerializer _serializer;
    private readonly PostgresEventStore _eventStore;

    public PostgresEventStoreTests()
    {
        DbContextOptions<EventStoreDbContext> options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"EventStore_{Guid.NewGuid()}")
            .Options;

        _context = new EventStoreDbContext(options);

        byte[] encryptionKey = new byte[32];
        byte[] hmacKey = new byte[64];
        RandomNumberGenerator.Fill(encryptionKey);
        RandomNumberGenerator.Fill(hmacKey);
        _cryptoService = new AesGcmCryptoService(encryptionKey, hmacKey);

        _serializer = new EventSerializer();

        EventStoreSettings settings = new() { VerifyChainOnRead = true };
        IOptions<EventStoreSettings> settingsOptions = Options.Create(settings);

        _eventStore = new PostgresEventStore(_context, _cryptoService, _serializer, settingsOptions);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldSucceed_ForNewStream()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new() { CreateInitiatedEvent() };

        // Act
        Result result = await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.Events.Count().Should().Be(1);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldReturnFailure_WhenVersionConflict()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new() { CreateInitiatedEvent() };
        await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Act — wrong expected version
        List<IDomainEvent> moreEvents = new() { CreateAuthorizedEvent() };
        Result result = await _eventStore.AppendEventsAsync(streamId, moreEvents, expectedVersion: -1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ConcurrencyConflict");
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldSucceed_WhenAppendingMultipleEvents()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new()
        {
            CreateInitiatedEvent(),
            CreateAuthorizedEvent(),
            CreateCompletedEvent()
        };

        // Act
        Result result = await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.Events.Count().Should().Be(3);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldReturnSuccess_WhenEventListIsEmpty()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new();

        // Act
        Result result = await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldSetChainHash_WithNullPreviousForFirst()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new() { CreateInitiatedEvent() };

        // Act
        await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        StoredEvent stored = _context.Events.First();
        stored.PreviousHash.Should().BeNull();
        stored.ChainHash.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldChainHashes_AcrossMultipleEvents()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new()
        {
            CreateInitiatedEvent(),
            CreateAuthorizedEvent()
        };

        // Act
        await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        List<StoredEvent> stored = _context.Events.OrderBy(e => e.Version).ToList();
        stored[0].PreviousHash.Should().BeNull();
        stored[1].PreviousHash.Should().BeEquivalentTo(stored[0].ChainHash);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldEncryptEventData()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        TransactionInitiatedEvent originalEvent = CreateInitiatedEvent();
        List<IDomainEvent> events = new() { originalEvent };

        // Act
        await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Assert
        StoredEvent stored = _context.Events.First();
        byte[] serialized = _serializer.Serialize(originalEvent);
        stored.EventData.Should().NotBeEquivalentTo(serialized, "data should be encrypted");
        stored.EventData.Length.Should().BeGreaterThan(serialized.Length, "encrypted data includes nonce + tag");
    }

    [Fact]
    public async Task GetEventsAsync_ShouldReturnEmpty_WhenStreamDoesNotExist()
    {
        // Act
        IReadOnlyList<IDomainEvent> events = await _eventStore.GetEventsAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_ShouldDecryptAndDeserializeEvents()
    {
        // Arrange — use a store without chain verification to isolate decrypt/deserialize behavior
        DbContextOptions<EventStoreDbContext> options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"EventStore_Decrypt_{Guid.NewGuid()}")
            .Options;
        EventStoreDbContext context = new(options);
        EventStoreSettings settings = new() { VerifyChainOnRead = false };
        PostgresEventStore store = new(context, _cryptoService, _serializer, Options.Create(settings));

        Guid streamId = Guid.NewGuid();
        TransactionInitiatedEvent originalEvent = CreateInitiatedEvent();
        TransactionAuthorizedEvent authEvent = CreateAuthorizedEvent();
        await store.AppendEventsAsync(streamId, new List<IDomainEvent> { originalEvent, authEvent }, expectedVersion: -1, CancellationToken.None);

        // Act — fromVersion: 0 means "after version 0", returns version 1+
        IReadOnlyList<IDomainEvent> events = await store.GetEventsAsync(streamId, CancellationToken.None);

        // Assert
        events.Should().HaveCount(1);
        events[0].Should().BeOfType<TransactionAuthorizedEvent>();

        context.Dispose();
    }

    [Fact]
    public async Task GetEventsAsync_WithFromVersion_ShouldReturnOnlyNewerEvents()
    {
        // Arrange — events stored at versions 0, 1, 2
        Guid streamId = Guid.NewGuid();
        List<IDomainEvent> events = new()
        {
            CreateInitiatedEvent(),
            CreateAuthorizedEvent(),
            CreateCompletedEvent()
        };
        await _eventStore.AppendEventsAsync(streamId, events, expectedVersion: -1, CancellationToken.None);

        // Act — fromVersion: 1 means "after version 1", returns version 2 only
        IReadOnlyList<IDomainEvent> result = await _eventStore.GetEventsAsync(streamId, fromVersion: 1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEventsAsync_ShouldVerifyChainIntegrity_WhenEnabled()
    {
        // Arrange — append 2 events (versions 0, 1); read with fromVersion:0 returns version 1
        Guid streamId = Guid.NewGuid();
        await _eventStore.AppendEventsAsync(
            streamId,
            new List<IDomainEvent> { CreateInitiatedEvent(), CreateAuthorizedEvent() },
            expectedVersion: -1,
            CancellationToken.None);

        // Tamper with the chain hash of the second event (version 1)
        StoredEvent secondEvent = _context.Events.OrderBy(e => e.Version).Last();
        secondEvent.ChainHash = new byte[64]; // zeros
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = () => _eventStore.GetEventsAsync(streamId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EventChainIntegrityException>();
    }

    [Fact]
    public async Task GetEventsAsync_ShouldNotVerifyChain_WhenDisabled()
    {
        // Arrange
        DbContextOptions<EventStoreDbContext> options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"EventStore_NoVerify_{Guid.NewGuid()}")
            .Options;
        EventStoreDbContext context = new(options);
        EventStoreSettings settings = new() { VerifyChainOnRead = false };
        PostgresEventStore store = new(context, _cryptoService, _serializer, Options.Create(settings));

        Guid streamId = Guid.NewGuid();
        await store.AppendEventsAsync(
            streamId,
            new List<IDomainEvent> { CreateInitiatedEvent(), CreateAuthorizedEvent() },
            expectedVersion: -1,
            CancellationToken.None);

        // Tamper with chain hash of second event
        StoredEvent secondEvent = context.Events.OrderBy(e => e.Version).Last();
        secondEvent.ChainHash = new byte[64];
        await context.SaveChangesAsync();

        // Act — should NOT throw because verification is disabled
        IReadOnlyList<IDomainEvent> events = await store.GetEventsAsync(streamId, CancellationToken.None);

        // Assert — returns events after version 0, so 1 event
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetVersionAsync_ShouldReturnNegativeOne_ForEmptyStream()
    {
        // Act
        int version = await _eventStore.GetVersionAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        version.Should().Be(-1);
    }

    [Fact]
    public async Task GetVersionAsync_ShouldReturnCurrentVersion()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        await _eventStore.AppendEventsAsync(streamId, new List<IDomainEvent> { CreateInitiatedEvent(), CreateAuthorizedEvent() }, expectedVersion: -1, CancellationToken.None);

        // Act
        int version = await _eventStore.GetVersionAsync(streamId, CancellationToken.None);

        // Assert
        version.Should().Be(1);
    }

    [Fact]
    public async Task VerifyHashChainAsync_ShouldReturnTrue_ForValidChain()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        await _eventStore.AppendEventsAsync(
            streamId,
            new List<IDomainEvent> { CreateInitiatedEvent(), CreateAuthorizedEvent() },
            expectedVersion: -1,
            CancellationToken.None);

        // Act
        Result<bool> result = await _eventStore.VerifyHashChainAsync(streamId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyHashChainAsync_ShouldReturnTrue_ForEmptyStream()
    {
        // Act
        Result<bool> result = await _eventStore.VerifyHashChainAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyHashChainAsync_ShouldReturnFalse_WhenChainIsTampered()
    {
        // Arrange
        Guid streamId = Guid.NewGuid();
        await _eventStore.AppendEventsAsync(
            streamId,
            new List<IDomainEvent> { CreateInitiatedEvent(), CreateAuthorizedEvent() },
            expectedVersion: -1,
            CancellationToken.None);

        // Tamper with chain hash of second event
        StoredEvent secondEvent = _context.Events.OrderBy(e => e.Version).Last();
        secondEvent.ChainHash = new byte[64];
        await _context.SaveChangesAsync();

        // Act
        Result<bool> result = await _eventStore.VerifyHashChainAsync(streamId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void EventChainIntegrityException_ShouldContainMessage()
    {
        // Act
        EventChainIntegrityException exception = new("Test integrity violation");

        // Assert
        exception.Message.Should().Be("Test integrity violation");
    }

    private static TransactionInitiatedEvent CreateInitiatedEvent()
    {
        return new TransactionInitiatedEvent
        {
            TransactionId = TransactionId.New(),
            SourceAccountId = AccountId.New(),
            DestinationAccountId = AccountId.New(),
            Amount = Money.Create(100.00m, Currency.USD).Value,
            Reference = "Test transaction"
        };
    }

    private static TransactionAuthorizedEvent CreateAuthorizedEvent()
    {
        return new TransactionAuthorizedEvent
        {
            TransactionId = TransactionId.New(),
            AuthorizationCode = "AUTH-TEST-001"
        };
    }

    private static TransactionCompletedEvent CreateCompletedEvent()
    {
        return new TransactionCompletedEvent
        {
            TransactionId = TransactionId.New(),
            CompletedAtUtc = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        _cryptoService.Dispose();
    }
}
