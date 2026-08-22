using System.Text.Json;
using MarqSpec.Client.ProjectX.FakeGateway.Auth;
using MarqSpec.Client.ProjectX.FakeGateway.Endpoints;
using MarqSpec.Client.ProjectX.FakeGateway.Hubs;
using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.FakeGateway;

/// <summary>
/// Builds the fake gateway. Factored out of <c>Program</c> so the integration tests can host it on a real
/// Kestrel port in-process, while <c>docker compose</c> runs exactly the same wiring as a service.
/// </summary>
/// <remarks>
/// Hosting it in-process is what lets <c>dotnet test</c> exercise real HTTP <i>and real WebSocket</i> transport
/// with no Docker installed. That matters: a test environment nobody can run is the failure mode being fixed
/// here, and requiring a container daemon would reintroduce it in a new shape.
/// </remarks>
public static class FakeGatewayHost
{
    /// <summary>Builds a configured, unstarted application.</summary>
    /// <param name="args">Host arguments; pass <c>[]</c> when hosting in-process.</param>
    /// <param name="urls">Addresses to listen on. Use <c>http://127.0.0.1:0</c> for an ephemeral port.</param>
    public static WebApplication Build(string[] args, string? urls = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (!string.IsNullOrWhiteSpace(urls))
        {
            builder.WebHost.UseUrls(urls);
        }

        builder.Services.AddSingleton<GatewayState>();
        builder.Services.AddSingleton<JwtIssuer>();
        builder.Services.AddSignalR();

        // Enums travel as INTEGERS, matching the gateway's schema and the client's _gatewaySettings. Adding a
        // JsonStringEnumConverter here would make the fake accept payloads the real gateway rejects — the worst
        // possible failure for a test double: green locally, broken against the venue (ADR-0001).
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();

        // Fault injection and bearer enforcement wrap every /api route.
        app.UseMiddleware<GatewayMiddleware>();

        app.MapAuthEndpoints();
        app.MapAccountEndpoints();
        app.MapContractEndpoints();
        app.MapHistoryEndpoints();
        app.MapOrderEndpoints();
        app.MapPositionEndpoints();
        app.MapTradeEndpoints();
        app.MapStatusEndpoints();
        app.MapControlEndpoints();

        app.MapHub<UserHub>("/hubs/user");
        app.MapHub<MarketHub>("/hubs/market");

        return app;
    }
}
