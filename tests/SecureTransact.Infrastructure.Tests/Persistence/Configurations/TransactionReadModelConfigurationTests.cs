using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SecureTransact.Infrastructure.Persistence.Contexts;
using SecureTransact.Infrastructure.Persistence.ReadModels;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Persistence.Configurations;

public sealed class TransactionReadModelConfigurationTests : IDisposable
{
    private readonly TransactionDbContext _context;

    public TransactionReadModelConfigurationTests()
    {
        DbContextOptions<TransactionDbContext> options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReadModelConfig_{Guid.NewGuid()}")
            .Options;

        _context = new TransactionDbContext(options);
    }

    [Fact]
    public void TransactionReadModel_ShouldBeMappedToCorrectTable()
    {
        // Act
        IEntityType entityType = _context.Model.FindEntityType(typeof(TransactionReadModel))!;

        // Assert
        entityType.Should().NotBeNull();
        entityType.GetTableName().Should().Be("transactions");
        entityType.GetSchema().Should().Be("read_model");
    }

    [Fact]
    public void TransactionReadModel_ShouldHavePrimaryKey_OnId()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(TransactionReadModel));
        IKey? primaryKey = entityType?.FindPrimaryKey();

        // Assert
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == "Id");
    }

    [Fact]
    public void TransactionReadModel_ShouldHaveRequiredProperties()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(TransactionReadModel));

        // Assert
        entityType!.FindProperty("SourceAccountId")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("DestinationAccountId")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Amount")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Currency")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Status")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("InitiatedAtUtc")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("Version")!.IsNullable.Should().BeFalse();
        entityType.FindProperty("LastUpdatedAtUtc")!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void TransactionReadModel_ShouldHaveNullableProperties()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(TransactionReadModel));

        // Assert
        entityType!.FindProperty("Reference")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("AuthorizationCode")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("FailureCode")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("FailureReason")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("ReversalReason")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("DisputeReason")!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void TransactionReadModel_ShouldHaveIndexes()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(TransactionReadModel));

        // Assert — check that indexes exist on key query columns
        IProperty sourceAccountProp = entityType!.FindProperty("SourceAccountId")!;
        IProperty destAccountProp = entityType.FindProperty("DestinationAccountId")!;
        IProperty statusProp = entityType.FindProperty("Status")!;

        entityType.FindIndex(new[] { sourceAccountProp }).Should().NotBeNull();
        entityType.FindIndex(new[] { destAccountProp }).Should().NotBeNull();
        entityType.FindIndex(new[] { statusProp }).Should().NotBeNull();
    }

    [Fact]
    public void TransactionReadModel_CurrencyProperty_ShouldHaveMaxLength()
    {
        // Act
        IEntityType? entityType = _context.Model.FindEntityType(typeof(TransactionReadModel));
        IProperty? currencyProp = entityType?.FindProperty("Currency");

        // Assert
        currencyProp.Should().NotBeNull();
        currencyProp!.GetMaxLength().Should().Be(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
