using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureTransact.Infrastructure.Persistence.Contexts;

namespace SecureTransact.Api.Endpoints;

/// <summary>
/// Health check endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health check endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealth)
            .WithName("HealthCheck")
            .WithTags("Health")
            .WithSummary("Basic health check")
            .AllowAnonymous();

        app.MapGet("/health/ready", GetReadiness)
            .WithName("ReadinessCheck")
            .WithTags("Health")
            .WithSummary("Readiness check including database connectivity")
            .AllowAnonymous();

        app.MapGet("/health/live", GetLiveness)
            .WithName("LivenessCheck")
            .WithTags("Health")
            .WithSummary("Liveness check")
            .AllowAnonymous();

        return app;
    }

    private static IResult GetHealth()
    {
        return Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = typeof(HealthEndpoints).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }

    private static async Task<IResult> GetReadiness(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        bool databaseHealthy = false;
        string? databaseError = null;

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            EventStoreDbContext context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
            await context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            databaseHealthy = true;
        }
        catch (Exception ex)
        {
            databaseError = ex.Message;
        }

        bool isReady = databaseHealthy;

        object response = new
        {
            Status = isReady ? "Ready" : "NotReady",
            Timestamp = DateTime.UtcNow,
            Checks = new
            {
                Database = new
                {
                    Status = databaseHealthy ? "Healthy" : "Unhealthy",
                    Error = databaseError
                }
            }
        };

        return isReady
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static IResult GetLiveness()
    {
        return Results.Ok(new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        });
    }
}
