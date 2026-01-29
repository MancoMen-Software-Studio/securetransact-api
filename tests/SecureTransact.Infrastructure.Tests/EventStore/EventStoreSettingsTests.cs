using FluentAssertions;
using SecureTransact.Infrastructure.EventStore;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore;

public sealed class EventStoreSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeEventStore()
    {
        // Assert
        EventStoreSettings.SectionName.Should().Be("EventStore");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        EventStoreSettings settings = new();

        // Assert
        settings.VerifyChainOnRead.Should().BeTrue();
        settings.ReadBatchSize.Should().Be(1000);
        settings.UseSnapshots.Should().BeTrue();
        settings.SnapshotThreshold.Should().Be(100);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        EventStoreSettings settings = new()
        {
            VerifyChainOnRead = false,
            ReadBatchSize = 500,
            UseSnapshots = false,
            SnapshotThreshold = 50
        };

        // Assert
        settings.VerifyChainOnRead.Should().BeFalse();
        settings.ReadBatchSize.Should().Be(500);
        settings.UseSnapshots.Should().BeFalse();
        settings.SnapshotThreshold.Should().Be(50);
    }
}
