using System.Linq;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.ValueObjects;

public sealed class TransactionStatusTests
{
    [Fact]
    public void Initiated_CanTransitionTo_Authorized()
    {
        // Assert
        TransactionStatus.Initiated.CanTransitionTo(TransactionStatus.Authorized).Should().BeTrue();
    }

    [Fact]
    public void Initiated_CanTransitionTo_Failed()
    {
        // Assert
        TransactionStatus.Initiated.CanTransitionTo(TransactionStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public void Initiated_CannotTransitionTo_Completed()
    {
        // Assert
        TransactionStatus.Initiated.CanTransitionTo(TransactionStatus.Completed).Should().BeFalse();
    }

    [Fact]
    public void Authorized_CanTransitionTo_Completed()
    {
        // Assert
        TransactionStatus.Authorized.CanTransitionTo(TransactionStatus.Completed).Should().BeTrue();
    }

    [Fact]
    public void Authorized_CanTransitionTo_Failed()
    {
        // Assert
        TransactionStatus.Authorized.CanTransitionTo(TransactionStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public void Completed_CanTransitionTo_Reversed()
    {
        // Assert
        TransactionStatus.Completed.CanTransitionTo(TransactionStatus.Reversed).Should().BeTrue();
    }

    [Fact]
    public void Completed_CanTransitionTo_Disputed()
    {
        // Assert
        TransactionStatus.Completed.CanTransitionTo(TransactionStatus.Disputed).Should().BeTrue();
    }

    [Fact]
    public void Failed_IsTerminal()
    {
        // Assert
        TransactionStatus.Failed.IsTerminal.Should().BeTrue();
        TransactionStatus.Failed.CanTransitionTo(TransactionStatus.Initiated).Should().BeFalse();
        TransactionStatus.Failed.CanTransitionTo(TransactionStatus.Completed).Should().BeFalse();
    }

    [Fact]
    public void Reversed_IsTerminal()
    {
        // Assert
        TransactionStatus.Reversed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Disputed_CanTransitionTo_CompletedOrReversed()
    {
        // Assert
        TransactionStatus.Disputed.CanTransitionTo(TransactionStatus.Completed).Should().BeTrue();
        TransactionStatus.Disputed.CanTransitionTo(TransactionStatus.Reversed).Should().BeTrue();
    }

    [Fact]
    public void FromName_ShouldReturnCorrectStatus()
    {
        // Act
        TransactionStatus? status = TransactionStatus.FromName("Initiated");

        // Assert
        status.Should().Be(TransactionStatus.Initiated);
    }

    [Fact]
    public void FromName_ShouldBeCaseInsensitive()
    {
        // Act
        TransactionStatus? status = TransactionStatus.FromName("COMPLETED");

        // Assert
        status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public void FromName_ShouldReturnNull_WhenInvalid()
    {
        // Act
        TransactionStatus? status = TransactionStatus.FromName("Invalid");

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void FromName_ShouldReturnNull_WhenNull()
    {
        // Act
        TransactionStatus? status = TransactionStatus.FromName(null);

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        TransactionStatus[] statuses = TransactionStatus.GetAll().ToArray();

        // Assert
        statuses.Should().HaveCount(6);
        statuses.Should().Contain(TransactionStatus.Initiated);
        statuses.Should().Contain(TransactionStatus.Authorized);
        statuses.Should().Contain(TransactionStatus.Completed);
        statuses.Should().Contain(TransactionStatus.Failed);
        statuses.Should().Contain(TransactionStatus.Reversed);
        statuses.Should().Contain(TransactionStatus.Disputed);
    }

    [Fact]
    public void IsSuccessful_ShouldBeTrue_ForCompletedAndReversed()
    {
        // Assert
        TransactionStatus.Completed.IsSuccessful.Should().BeTrue();
        TransactionStatus.Reversed.IsSuccessful.Should().BeTrue();
        TransactionStatus.Failed.IsSuccessful.Should().BeFalse();
        TransactionStatus.Initiated.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void CanTransitionTo_ShouldReturnFalse_WhenTargetIsNull()
    {
        // Assert
        TransactionStatus.Initiated.CanTransitionTo(null!).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Assert
        TransactionStatus.Completed.ToString().Should().Be("Completed");
    }

    [Theory]
    [InlineData("Initiated", "Transaction initiated")]
    [InlineData("Completed", "Transaction completed")]
    [InlineData("Failed", "Transaction failed")]
    public void Description_ShouldBeCorrect(string name, string expectedDescription)
    {
        // Act
        TransactionStatus? status = TransactionStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status!.Description.Should().Be(expectedDescription);
    }
}
