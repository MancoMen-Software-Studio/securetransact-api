using System;
using FluentAssertions;
using SecureTransact.Infrastructure.Persistence.ReadModels;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Persistence.ReadModels;

public sealed class TransactionReadModelTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        TransactionReadModel readModel = new();

        // Assert
        readModel.Id.Should().Be(Guid.Empty);
        readModel.SourceAccountId.Should().Be(Guid.Empty);
        readModel.DestinationAccountId.Should().Be(Guid.Empty);
        readModel.Amount.Should().Be(0m);
        readModel.Currency.Should().BeEmpty();
        readModel.Status.Should().BeEmpty();
        readModel.Reference.Should().BeNull();
        readModel.AuthorizationCode.Should().BeNull();
        readModel.FailureCode.Should().BeNull();
        readModel.FailureReason.Should().BeNull();
        readModel.ReversalReason.Should().BeNull();
        readModel.DisputeReason.Should().BeNull();
        readModel.AuthorizedAtUtc.Should().BeNull();
        readModel.CompletedAtUtc.Should().BeNull();
        readModel.FailedAtUtc.Should().BeNull();
        readModel.ReversedAtUtc.Should().BeNull();
        readModel.DisputedAtUtc.Should().BeNull();
        readModel.Version.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Guid sourceId = Guid.NewGuid();
        Guid destId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        // Act
        TransactionReadModel readModel = new()
        {
            Id = id,
            SourceAccountId = sourceId,
            DestinationAccountId = destId,
            Amount = 1500.75m,
            Currency = "EUR",
            Status = "Completed",
            Reference = "INV-2024-001",
            AuthorizationCode = "AUTH-999",
            FailureCode = null,
            FailureReason = null,
            ReversalReason = null,
            DisputeReason = null,
            InitiatedAtUtc = now.AddMinutes(-5),
            AuthorizedAtUtc = now.AddMinutes(-3),
            CompletedAtUtc = now,
            FailedAtUtc = null,
            ReversedAtUtc = null,
            DisputedAtUtc = null,
            Version = 3,
            LastUpdatedAtUtc = now
        };

        // Assert
        readModel.Id.Should().Be(id);
        readModel.SourceAccountId.Should().Be(sourceId);
        readModel.DestinationAccountId.Should().Be(destId);
        readModel.Amount.Should().Be(1500.75m);
        readModel.Currency.Should().Be("EUR");
        readModel.Status.Should().Be("Completed");
        readModel.Reference.Should().Be("INV-2024-001");
        readModel.AuthorizationCode.Should().Be("AUTH-999");
        readModel.Version.Should().Be(3);
        readModel.CompletedAtUtc.Should().Be(now);
    }
}
