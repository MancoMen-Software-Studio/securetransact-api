using FluentAssertions;
using SecureTransact.Api.Configuration;
using Xunit;

namespace SecureTransact.Api.Tests.Configuration;

public sealed class SecurityHeadersOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeSecurityHeaders()
    {
        // Assert
        SecurityHeadersOptions.SectionName.Should().Be("SecurityHeaders");
    }

    [Fact]
    public void DefaultValues_ShouldBeSecure()
    {
        // Act
        SecurityHeadersOptions options = new();

        // Assert
        options.EnableHsts.Should().BeTrue();
        options.ContentSecurityPolicy.Should().Be("default-src 'self'");
        options.EnableNoSniff.Should().BeTrue();
        options.FrameOptions.Should().Be("DENY");
        options.ReferrerPolicy.Should().Be("strict-origin-when-cross-origin");
        options.RemoveServerHeader.Should().BeTrue();
        options.PermissionsPolicy.Should().Be("camera=(), microphone=(), geolocation=()");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        SecurityHeadersOptions options = new()
        {
            EnableHsts = false,
            ContentSecurityPolicy = "default-src 'none'",
            EnableNoSniff = false,
            FrameOptions = "SAMEORIGIN",
            ReferrerPolicy = "no-referrer",
            RemoveServerHeader = false,
            PermissionsPolicy = "camera=(self)"
        };

        // Assert
        options.EnableHsts.Should().BeFalse();
        options.ContentSecurityPolicy.Should().Be("default-src 'none'");
        options.EnableNoSniff.Should().BeFalse();
        options.FrameOptions.Should().Be("SAMEORIGIN");
        options.ReferrerPolicy.Should().Be("no-referrer");
        options.RemoveServerHeader.Should().BeFalse();
        options.PermissionsPolicy.Should().Be("camera=(self)");
    }
}
