using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureTransact.Api.Configuration;

namespace SecureTransact.Api.Extensions;

/// <summary>
/// Extensions for configuring security headers and rate limiting.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds rate limiting services with configurable options.
    /// </summary>
    public static IServiceCollection AddSecurityRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RateLimitingOptions rateLimitingOptions = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitingOptions.PermitLimit;
                limiterOptions.Window = System.TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitingOptions.QueueLimit;
            });
        });

        return services;
    }

    /// <summary>
    /// Adds security response headers to every request.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "0";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";

            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });
    }
}
