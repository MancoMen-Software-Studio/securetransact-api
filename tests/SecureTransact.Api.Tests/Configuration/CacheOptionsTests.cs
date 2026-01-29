using FluentAssertions;
using SecureTransact.Api.Configuration;
using Xunit;

namespace SecureTransact.Api.Tests.Configuration;

public sealed class CacheOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeCache()
    {
        // Assert
        CacheOptions.SectionName.Should().Be("Cache");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        CacheOptions options = new();

        // Assert
        options.ConnectionString.Should().Be("localhost:6379");
        options.InstanceName.Should().Be("SecureTransact:");
        options.DefaultExpirationMinutes.Should().Be(5);
        options.SlidingExpirationMinutes.Should().Be(2);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        CacheOptions options = new()
        {
            ConnectionString = "redis.production:6380",
            InstanceName = "Prod:",
            DefaultExpirationMinutes = 15,
            SlidingExpirationMinutes = 5
        };

        // Assert
        options.ConnectionString.Should().Be("redis.production:6380");
        options.InstanceName.Should().Be("Prod:");
        options.DefaultExpirationMinutes.Should().Be(15);
        options.SlidingExpirationMinutes.Should().Be(5);
    }
}
