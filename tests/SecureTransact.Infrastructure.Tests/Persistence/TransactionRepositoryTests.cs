using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.Events;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Persistence;

public sealed class TransactionRepositoryTests
{
    private readonly IEventStore _eventStore;
    private readonly TransactionRepository _repository;

    public TransactionRepositoryTests()
    {
        _eventStore = Substitute.For<IEventStore>();
        _repository = new TransactionRepository(_eventStore);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNoEventsExist()
    {
        // Arrange
        TransactionId id = TransactionId.New();
        _eventStore.GetEventsAsync(id.Value, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDomainEvent>());

        // Act
        TransactionAggregate? result = await _repository.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReconstructAggregate_FromEvents()
    {
        // Arrange
        TransactionId txId = TransactionId.New();
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(500.00m, Currency.USD).Value;

        List<IDomainEvent> events = new()
        {
            new TransactionInitiatedEvent
            {
                TransactionId = txId,
                SourceAccountId = sourceId,
                DestinationAccountId = destId,
                Amount = amount,
                Reference = "Test"
            }
        };

        _eventStore.GetEventsAsync(txId.Value, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(events);

        // Act
        TransactionAggregate? result = await _repository.GetByIdAsync(txId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Value.Should().Be(txId.Value);
    }

    [Fact]
    public async Task AddAsync_ShouldAppendEvents_ToEventStore()
    {
        // Arrange
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(100.00m, Currency.USD).Value;

        Result<TransactionAggregate> createResult = TransactionAggregate.Create(sourceId, destId, amount, "Test");
        TransactionAggregate aggregate = createResult.Value;

        _eventStore.AppendEventsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _repository.AddAsync(aggregate, CancellationToken.None);

        // Assert
        await _eventStore.Received(1).AppendEventsAsync(
            aggregate.Id.Value,
            Arg.Any<IEnumerable<IDomainEvent>>(),
            expectedVersion: 0,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ShouldClearDomainEvents_AfterPersisting()
    {
        // Arrange
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(100.00m, Currency.USD).Value;

        Result<TransactionAggregate> createResult = TransactionAggregate.Create(sourceId, destId, amount, "Test");
        TransactionAggregate aggregate = createResult.Value;

        _eventStore.AppendEventsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _repository.AddAsync(aggregate, CancellationToken.None);

        // Assert
        aggregate.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ShouldNotCallEventStore_WhenNoEvents()
    {
        // Arrange — create aggregate and clear events manually
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(100.00m, Currency.USD).Value;

        Result<TransactionAggregate> createResult = TransactionAggregate.Create(sourceId, destId, amount, "Test");
        TransactionAggregate aggregate = createResult.Value;
        aggregate.ClearDomainEvents();

        // Act
        await _repository.AddAsync(aggregate, CancellationToken.None);

        // Assert
        await _eventStore.DidNotReceive().AppendEventsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Update_ShouldNotThrow()
    {
        // Arrange
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(100.00m, Currency.USD).Value;
        TransactionAggregate aggregate = TransactionAggregate.Create(sourceId, destId, amount, "Test").Value;

        // Act
        Action act = () => _repository.Update(aggregate);

        // Assert
        act.Should().NotThrow();
    }
}
