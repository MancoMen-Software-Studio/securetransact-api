using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SecureTransact.Infrastructure.EventStore;
using SecureTransact.Infrastructure.Persistence.Contexts;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Persistence.Configurations;

public sealed class StoredEventConfigurationTests : IDisposable
{
    private readonly EventStoreDbContext _context;

    public StoredEventConfigurationTests()
    {
        DbContextOptions<EventStoreDbContext> options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"StoredEventConfig_{Guid.NewGuid()}")
            .Options;

        _context = new EventStoreDbContext(options);
    }

    [Fact]
    public void StoredEvent_ShouldBeMappedToCorrectTable()
    {
        // Act
        IEntityType entityType = _context.Model.FindEntityType(typeof(StoredEvent))!;

        // Assert
        entityType.Should().NotBeNull();
        entityType.GetTableName().Should().Be("events");
        entityType.GetSchema().Should().Be("event_store");
    }

    [Fact]
    public void StoredEvent_ShouldHavePrimaryKey_OnId()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(StoredEvent));
        IKey? primaryKey = entityType?.FindPrimaryKey();

        // Assert
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == "Id");
    }

    [Fact]
    public void StoredEvent_ShouldHaveUniqueIndex_OnAggregateIdAndVersion()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(StoredEvent));
        IIndex? index = entityType?.FindIndex(new[]
        {
            entityType.FindProperty("AggregateId")!,
            entityType.FindProperty("Version")!
        });

        // Assert
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void StoredEvent_ShouldHaveRequiredProperties()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(StoredEvent));

        // Assert
        entityType!.FindProperty("AggregateId")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("EventType")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("EventData")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Version")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("OccurredAtUtc")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("ChainHash")!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void StoredEvent_PreviousHash_ShouldBeNullable()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(StoredEvent));

        // Assert
        entityType!.FindProperty("PreviousHash")!.IsNullable.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
