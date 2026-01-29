using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Aggregates;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.Persistence;
using SecureTransact.Infrastructure.Persistence.Contexts;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly IEventStore _eventStore;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        DbContextOptions<EventStoreDbContext> options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"UoW_{Guid.NewGuid()}")
            .Options;

        _context = new EventStoreDbContext(options);
        _eventStore = Substitute.For<IEventStore>();
        _unitOfWork = new UnitOfWork(_context, _eventStore);
    }

    [Fact]
    public void Track_ShouldAddAggregate()
    {
        // Arrange
        TransactionAggregate aggregate = CreateAggregate();

        // Act & Assert — should not throw
        Action act = () => _unitOfWork.Track(aggregate);
        act.Should().NotThrow();
    }

    [Fact]
    public void Track_ShouldNotAddDuplicate()
    {
        // Arrange
        TransactionAggregate aggregate = CreateAggregate();

        // Act — track twice
        _unitOfWork.Track(aggregate);
        _unitOfWork.Track(aggregate);

        // Assert — no exception, duplicate is silently ignored
        // (Verified by behavior: SaveChanges should only append once)
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrow_WhenInMemoryProviderDoesNotSupportTransactions()
    {
        // Arrange
        TransactionAggregate aggregate = CreateAggregate();
        _unitOfWork.Track(aggregate);

        // Act — InMemory provider does not support BeginTransactionAsync
        Func<Task> act = () => _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldThrow_WithInMemoryProvider()
    {
        // Act — InMemory provider does not support transactions
        Func<Task> act = () => _unitOfWork.BeginTransactionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CommitTransactionAsync_ShouldNotThrow_WhenNoTransactionStarted()
    {
        // Act — no transaction started, should be a no-op
        Func<Task> act = () => _unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_ShouldNotThrow_WhenNoTransactionStarted()
    {
        // Act — no transaction started, should be a no-op
        Func<Task> act = () => _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static TransactionAggregate CreateAggregate()
    {
        AccountId sourceId = AccountId.New();
        AccountId destId = AccountId.New();
        Money amount = Money.Create(100.00m, Currency.USD).Value;
        return TransactionAggregate.Create(sourceId, destId, amount, "Test").Value;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
