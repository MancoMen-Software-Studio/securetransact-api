using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using Xunit;

namespace SecureTransact.Domain.Tests.Abstractions;

public sealed class AggregateRootTests
{
    private sealed record TestDomainEvent(Guid EventId, DateTime OccurredOnUtc, string Data) : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate(Guid id)
        {
            Id = id;
        }

        public void DoSomething(string data)
        {
            RaiseDomainEvent(new TestDomainEvent(Guid.NewGuid(), DateTime.UtcNow, data));
        }

        public void DoMultipleThings()
        {
            RaiseDomainEvent(new TestDomainEvent(Guid.NewGuid(), DateTime.UtcNow, "First"));
            RaiseDomainEvent(new TestDomainEvent(Guid.NewGuid(), DateTime.UtcNow, "Second"));
            RaiseDomainEvent(new TestDomainEvent(Guid.NewGuid(), DateTime.UtcNow, "Third"));
        }
    }

    [Fact]
    public void DomainEvents_ShouldBeEmpty_WhenNoEventsRaised()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act & Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act
        aggregate.DoSomething("test data");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.Should().ContainSingle(e => ((TestDomainEvent)e).Data == "test data");
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddMultipleEventsInOrder()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act
        aggregate.DoMultipleThings();

        // Assert
        aggregate.DomainEvents.Should().HaveCount(3);
        TestDomainEvent[] events = aggregate.DomainEvents.Cast<TestDomainEvent>().ToArray();
        events[0].Data.Should().Be("First");
        events[1].Data.Should().Be("Second");
        events[2].Data.Should().Be("Third");
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());
        aggregate.DoMultipleThings();
        aggregate.DomainEvents.Should().HaveCount(3);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());
        aggregate.DoSomething("test");

        // Act & Assert
        aggregate.DomainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void Aggregate_ShouldInheritEntityEquality()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestAggregate aggregate1 = new(id);
        TestAggregate aggregate2 = new(id);

        // Act & Assert
        aggregate1.Should().Be(aggregate2);
        (aggregate1 == aggregate2).Should().BeTrue();
    }

    [Fact]
    public void Aggregate_ShouldImplementIAggregateRoot()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act & Assert
        aggregate.Should().BeAssignableTo<IAggregateRoot>();
    }
}
