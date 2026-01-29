using FluentAssertions;
using SecureTransact.Api.Configuration;
using Xunit;

namespace SecureTransact.Api.Tests.Configuration;

public sealed class RateLimitingOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeRateLimiting()
    {
        // Assert
        RateLimitingOptions.SectionName.Should().Be("RateLimiting");
    }

    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Act
        RateLimitingOptions options = new();

        // Assert
        options.PermitLimit.Should().Be(100);
        options.WindowSeconds.Should().Be(60);
        options.QueueLimit.Should().Be(10);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        RateLimitingOptions options = new()
        {
            PermitLimit = 50,
            WindowSeconds = 30,
            QueueLimit = 5
        };

        // Assert
        options.PermitLimit.Should().Be(50);
        options.WindowSeconds.Should().Be(30);
        options.QueueLimit.Should().Be(5);
    }
}
