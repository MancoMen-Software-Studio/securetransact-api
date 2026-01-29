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
    /// Applies a relaxed CSP for API documentation paths (Scalar/OpenAPI)
    /// and a strict CSP for all other endpoints.
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

            string path = context.Request.Path.Value ?? string.Empty;
            bool isDocumentationPath = path.StartsWith("/scalar", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/openapi", System.StringComparison.OrdinalIgnoreCase);

            if (isDocumentationPath)
            {
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: blob:; connect-src 'self'";
            }
            else
            {
                context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
            }

            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });
    }
}
