using System;
using FluentAssertions;
using SecureTransact.Infrastructure.EventStore;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore;

public sealed class StoredEventTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        StoredEvent storedEvent = new();

        // Assert
        storedEvent.Id.Should().Be(Guid.Empty);
        storedEvent.AggregateId.Should().Be(Guid.Empty);
        storedEvent.EventType.Should().BeEmpty();
        storedEvent.EventData.Should().BeEmpty();
        storedEvent.Version.Should().Be(0);
        storedEvent.ChainHash.Should().BeEmpty();
        storedEvent.PreviousHash.Should().BeNull();
        storedEvent.GlobalSequence.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid aggregateId = Guid.NewGuid();
        byte[] eventData = new byte[] { 1, 2, 3 };
        byte[] chainHash = new byte[] { 4, 5, 6 };
        byte[] previousHash = new byte[] { 7, 8, 9 };
        DateTime occurredAt = DateTime.UtcNow;

        // Act
        StoredEvent storedEvent = new()
        {
            Id = id,
            AggregateId = aggregateId,
            EventType = "TestEvent",
            EventData = eventData,
            Version = 5,
            OccurredAtUtc = occurredAt,
            ChainHash = chainHash,
            PreviousHash = previousHash,
            GlobalSequence = 42
        };

        // Assert
        storedEvent.Id.Should().Be(id);
        storedEvent.AggregateId.Should().Be(aggregateId);
        storedEvent.EventType.Should().Be("TestEvent");
        storedEvent.EventData.Should().BeEquivalentTo(eventData);
        storedEvent.Version.Should().Be(5);
        storedEvent.OccurredAtUtc.Should().Be(occurredAt);
        storedEvent.ChainHash.Should().BeEquivalentTo(chainHash);
        storedEvent.PreviousHash.Should().BeEquivalentTo(previousHash);
        storedEvent.GlobalSequence.Should().Be(42);
    }

    [Fact]
    public void PreviousHash_ShouldBeNullable_ForFirstEventInChain()
    {
        // Arrange & Act
        StoredEvent firstEvent = new()
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "TransactionInitiatedEvent",
            EventData = new byte[] { 1, 2, 3 },
            Version = 0,
            OccurredAtUtc = DateTime.UtcNow,
            ChainHash = new byte[] { 10, 20, 30 },
            PreviousHash = null
        };

        // Assert
        firstEvent.PreviousHash.Should().BeNull();
    }
}
