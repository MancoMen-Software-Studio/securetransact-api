using System;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using Xunit;

namespace SecureTransact.Domain.Tests.Abstractions;

public sealed class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id)
        {
            Id = id;
        }

        public string Name { get; init; } = string.Empty;
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenEntitiesHaveSameId()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity1 = new(id) { Name = "Entity 1" };
        TestEntity entity2 = new(id) { Name = "Entity 2" };

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenEntitiesHaveDifferentIds()
    {
        // Arrange
        TestEntity entity1 = new(Guid.NewGuid()) { Name = "Same Name" };
        TestEntity entity2 = new(Guid.NewGuid()) { Name = "Same Name" };

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        // Arrange
        TestEntity entity = new(Guid.NewGuid());

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingSameInstance()
    {
        // Arrange
        TestEntity entity = new(Guid.NewGuid());
        TestEntity sameReference = entity;

        // Act & Assert
        entity.Equals(sameReference).Should().BeTrue();
        (entity == sameReference).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_WhenEntitiesHaveSameId()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity1 = new(id);
        TestEntity entity2 = new(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeDifferent_WhenEntitiesHaveDifferentIds()
    {
        // Arrange
        TestEntity entity1 = new(Guid.NewGuid());
        TestEntity entity2 = new(Guid.NewGuid());

        // Act & Assert
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        TestEntity entity = new(Guid.NewGuid());
        string differentTypeObject = "not an entity";

        // Act & Assert
        entity.Equals(differentTypeObject).Should().BeFalse();
    }

    [Fact]
    public void NullEquality_ShouldReturnTrue_WhenBothAreNull()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity2).Should().BeFalse();
    }
}
