using System.Net.Http.Json;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.DependencyInjection;
using MarqSpec.Client.ProjectX.FakeGateway;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the fake gateway on an ephemeral Kestrel port and hands out clients pointed at it.
/// </summary>
/// <remarks>
/// Started in-process rather than requiring a container, so the tier runs from a bare <c>dotnet test</c> with no
/// Docker and no credentials. Set <c>PROJECTX_FAKE_GATEWAY_URL</c> to run against a composed instance instead —
/// which is the same application, since <see cref="FakeGatewayHost"/> is what the container runs too.
/// <para>
/// Real Kestrel, not <c>TestServer</c>: the client's most valuable surface is the SignalR hub client, and that
/// needs a real WebSocket transport to be exercised at all.
/// </para>
/// </remarks>
public sealed class FakeGatewayFixture : IAsyncLifetime
{
    private WebApplication? _app;

    /// <summary>Base address of the running gateway, e.g. <c>http://127.0.0.1:51234</c>.</summary>
    public string BaseAddress { get; private set; } = string.Empty;

    /// <summary>A bare HTTP client for driving the <c>/_control</c> surface.</summary>
    public HttpClient Control { get; private set; } = new();

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("PROJECTX_FAKE_GATEWAY_URL");
        if (!string.IsNullOrWhiteSpace(external))
        {
            BaseAddress = external.TrimEnd('/');
        }
        else
        {
            _app = FakeGatewayHost.Build([], "http://127.0.0.1:0");
            await _app.StartAsync();

            var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
                ?? throw new InvalidOperationException("Kestrel reported no server addresses.");
            BaseAddress = addresses.Addresses.First().TrimEnd('/');
        }

        Control = new HttpClient { BaseAddress = new Uri(BaseAddress) };
        await ResetAsync();
    }

    public async Task DisposeAsync()
    {
        Control.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>Restores the deterministic seed. Call from a test that mutates state.</summary>
    public async Task ResetAsync()
    {
        using var response = await Control.PostAsync("/_control/reset", content: null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Arms a fault scenario against the gateway.</summary>
    public async Task ArmFaultAsync(object directive)
    {
        using var response = await Control.PostAsJsonAsync("/_control/fault", directive);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>How many requests actually reached <paramref name="path"/>.</summary>
    public async Task<int> RequestCountAsync(string path)
    {
        var payload = await Control.GetFromJsonAsync<RequestCount>($"/_control/requests?path={Uri.EscapeDataString(path)}");
        return payload?.Count ?? 0;
    }

    /// <summary>Which hubs have seen an access token on a handshake ("user", "market").</summary>
    public async Task<IReadOnlyList<string>> HubTokensSeenAsync()
    {
        var payload = await Control.GetFromJsonAsync<ControlState>("/_control/state");
        return payload?.HubTokensSeen ?? [];
    }

    /// <summary>Subscription calls the hubs received, as "hub:method:argument".</summary>
    public async Task<IReadOnlyList<string>> HubSubscriptionsAsync()
    {
        var payload = await Control.GetFromJsonAsync<ControlState>("/_control/state");
        return payload?.HubSubscriptions ?? [];
    }

    /// <summary>Live SignalR connection ids on <paramref name="hub"/> ("market" or "user").</summary>
    public async Task<IReadOnlyList<string>> HubConnectionIdsAsync(string hub)
    {
        var payload = await Control.GetFromJsonAsync<ControlState>("/_control/state");
        if (payload?.HubConnections is null)
        {
            return [];
        }

        return string.Equals(hub, "user", StringComparison.OrdinalIgnoreCase)
            ? payload.HubConnections.User ?? []
            : payload.HubConnections.Market ?? [];
    }

    /// <summary>Aborts every live connection on the named hub so automatic reconnect gets a new connection id.</summary>
    public async Task<int> AbortHubAsync(string hub)
    {
        using var response = await Control.PostAsync($"/_control/abort/{hub}", content: null);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AbortResult>();
        return payload?.Aborted ?? 0;
    }

    /// <summary>Posts to a <c>/_control/emit/*</c> route.</summary>
    public async Task EmitAsync(string route, object payload)
    {
        using var response = await Control.PostAsJsonAsync($"/_control/emit/{route}", payload);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// A service provider with the client registered against the fake gateway, using placeholder credentials —
    /// the point of this tier is that no real credential exists anywhere.
    /// </summary>
    public ServiceProvider BuildClient(Action<Dictionary<string, string?>>? configure = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ProjectX:ApiKey"] = "fake-gateway-user",
            ["ProjectX:ApiSecret"] = "fake-gateway-secret",
            ["ProjectX:BaseUrl"] = BaseAddress,
            ["ProjectX:WebSocketUserHubUrl"] = $"{BaseAddress}/hubs/user",
            ["ProjectX:WebSocketMarketHubUrl"] = $"{BaseAddress}/hubs/market",
            ["ProjectX:WebSocket:UserHubUrl"] = $"{BaseAddress}/hubs/user",
            ["ProjectX:WebSocket:MarketHubUrl"] = $"{BaseAddress}/hubs/market",
        };

        configure?.Invoke(settings);

        // The environment variables must not leak in from a developer's shell and repoint the client at the
        // real venue — this tier must be incapable of reaching production.
        foreach (var name in new[] { "PROJECTX_API_KEY", "PROJECTX_API_SECRET", "ProjectX__ApiKey", "ProjectX__ApiSecret" })
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddProjectXApiClient(configuration)
            .BuildServiceProvider();
    }

    /// <summary>A seeded contract id that exists in the fake gateway.</summary>
    public const string KnownContractId = "CON.F.US.ENQ.Z25";

    /// <summary>A seeded, tradable account id.</summary>
    public const int KnownAccountId = 1;

    private sealed record RequestCount(string Path, int Count);

    private sealed record AbortResult(string Hub, int Aborted);

    private sealed record ControlState(
        IReadOnlyList<string> HubTokensSeen,
        IReadOnlyList<string> HubSubscriptions,
        HubConnectionsState? HubConnections);

    private sealed record HubConnectionsState(IReadOnlyList<string>? Market, IReadOnlyList<string>? User);
}

/// <summary>Shares one gateway across a test class.</summary>
[CollectionDefinition(Name)]
public sealed class FakeGatewayCollection : ICollectionFixture<FakeGatewayFixture>
{
    public const string Name = "fake-gateway";
}
