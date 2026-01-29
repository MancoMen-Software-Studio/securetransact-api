using System;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.ValueObjects;

public sealed class TransactionIdTests
{
    [Fact]
    public void New_ShouldCreateUniqueId()
    {
        // Act
        TransactionId id1 = TransactionId.New();
        TransactionId id2 = TransactionId.New();

        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ShouldCreateFromValidGuid()
    {
        // Arrange
        Guid guid = Guid.NewGuid();

        // Act
        TransactionId id = TransactionId.From(guid);

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void From_ShouldThrow_WhenGuidIsEmpty()
    {
        // Arrange
        Guid emptyGuid = Guid.Empty;

        // Act
        Action act = () => TransactionId.From(emptyGuid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_WhenStringIsValidGuid()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        string stringValue = guid.ToString();

        // Act
        bool result = TransactionId.TryParse(stringValue, out TransactionId id);

        // Assert
        result.Should().BeTrue();
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenStringIsInvalid()
    {
        // Act
        bool result = TransactionId.TryParse("not-a-guid", out TransactionId id);

        // Assert
        result.Should().BeFalse();
        id.Should().Be(default(TransactionId));
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenStringIsEmptyGuid()
    {
        // Act
        bool result = TransactionId.TryParse(Guid.Empty.ToString(), out TransactionId id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenStringIsNull()
    {
        // Act
        bool result = TransactionId.TryParse(null, out TransactionId id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertToGuid()
    {
        // Arrange
        Guid expected = Guid.NewGuid();
        TransactionId id = TransactionId.From(expected);

        // Act
        Guid actual = id;

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        TransactionId id = TransactionId.From(guid);

        // Act
        string result = id.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_ShouldBeBasedOnValue()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        TransactionId id1 = TransactionId.From(guid);
        TransactionId id2 = TransactionId.From(guid);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }
}
