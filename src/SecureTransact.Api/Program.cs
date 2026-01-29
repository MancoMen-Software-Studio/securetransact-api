using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureTransact.Api.Endpoints;
using SecureTransact.Api.Extensions;
using SecureTransact.Api.Middleware;
using SecureTransact.Application;
using SecureTransact.Infrastructure;
using Serilog;

namespace SecureTransact.Api;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.AddSerilogLogging();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocumentation();

        builder.Services.AddJwtAuthentication(builder.Configuration);

        builder.Services.AddApplication();

        builder.Services.AddInfrastructure(builder.Configuration);

        WebApplication app = builder.Build();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
        });

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApiDocumentation();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthEndpoints();
        app.MapTransactionEndpoints();


        if (app.Environment.IsDevelopment())
        {
            app.MapDemoEndpoints();
        }

        app.Run();
    }
}
