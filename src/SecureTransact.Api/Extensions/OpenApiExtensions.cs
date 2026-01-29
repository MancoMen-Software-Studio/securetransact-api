using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace SecureTransact.Api.Extensions;

/// <summary>
/// Extensions for configuring OpenAPI/Swagger documentation.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds OpenAPI documentation services.
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "SecureTransact API";
                document.Info.Version = "v1";
                document.Info.Description = "Secure financial transaction processing API with event sourcing and cryptographic integrity.";

                HttpRequest? request = context.ApplicationServices
                    .GetService<IHttpContextAccessor>()?.HttpContext?.Request;

                if (request is not null)
                {
                    string scheme = request.Headers["X-Forwarded-Proto"].ToString() is { Length: > 0 } proto
                        ? proto
                        : request.Scheme;
                    string host = request.Headers["X-Forwarded-Host"].ToString() is { Length: > 0 } fwdHost
                        ? fwdHost
                        : request.Host.ToString();

                    document.Servers = new List<OpenApiServer>
                    {
                        new() { Url = $"{scheme}://{host}" }
                    };
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Maps OpenAPI documentation endpoints.
    /// </summary>
    public static IApplicationBuilder UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("SecureTransact API")
                .WithTheme(ScalarTheme.BluePlanet)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return app;
    }
}
