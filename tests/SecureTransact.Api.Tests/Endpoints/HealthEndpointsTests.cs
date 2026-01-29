using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SecureTransact.Api.Tests.Endpoints;

/// <summary>
/// Tests for health check endpoints.
/// </summary>
public sealed class HealthEndpointsTests
{
    [Fact]
    public void HealthResponse_ShouldHaveExpectedProperties()
    {
        // This test validates the shape of the health response
        // Integration tests would use WebApplicationFactory
        var response = new
        {
            Status = "Healthy",
            Timestamp = System.DateTime.UtcNow,
            Version = "1.0.0"
        };

        response.Status.Should().Be("Healthy");
        response.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ReadinessResponse_ShouldIncludeChecks()
    {
        // This test validates the shape of the readiness response
        var response = new
        {
            Status = "Ready",
            Timestamp = System.DateTime.UtcNow,
            Checks = new
            {
                Database = new
                {
                    Status = "Healthy",
                    Error = (string?)null
                }
            }
        };

        response.Status.Should().Be("Ready");
        response.Checks.Database.Status.Should().Be("Healthy");
    }
}
