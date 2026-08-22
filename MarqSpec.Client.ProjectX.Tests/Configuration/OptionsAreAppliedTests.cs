using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.DependencyInjection;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarqSpec.Client.ProjectX.Tests.Configuration;

/// <summary>
/// Proves configuration is <i>applied</i>, not merely bound.
/// </summary>
/// <remarks>
/// Eleven options were bound from configuration, listed in the library README, and read by nothing (gh#69).
/// Binding tests passed the whole time, because binding was never the broken part. Every test here asserts
/// against the object that consumes the value -- the resilience pipeline, the hub connection -- rather than
/// against the options instance.
/// </remarks>
public class OptionsAreAppliedTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        settings.TryAdd("ProjectX:ApiKey", "test-api-key");
        settings.TryAdd("ProjectX:ApiSecret", "test-api-secret");
        settings.TryAdd("ProjectX:BaseUrl", "https://api.test.invalid");

        return new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
            .AddProjectXApiClient(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
    }

    // NOTE: ProjectX:RetryOptions is proven in the integration tier, not here. The resilience options are
    // registered under the name Refit generates for the client, which is an internal detail of Refit -- a unit
    // test that guessed it would silently assert against a DEFAULT options instance and pass regardless. It is
    // counted at the gateway instead: see ResilienceIntegrationTests.

    #region Hub URLs -- both documented spellings

    [Fact]
    public void WebSocketOptions_ShouldResolveHubUrls_FromTheOuterSpellingTheReadmeDocuments()
    {
        // The failure this prevents: a consumer repoints at a simulation venue using the keys the README
        // lists, gets no error, and stays connected to the production TopstepX default.
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ProjectX:WebSocketUserHubUrl"] = "https://sim.example.invalid/hubs/user",
            ["ProjectX:WebSocketMarketHubUrl"] = "https://sim.example.invalid/hubs/market",
        });

        var options = provider.GetRequiredService<IOptions<WebSocketOptions>>().Value;

        options.UserHubUrl.Should().Be("https://sim.example.invalid/hubs/user");
        options.MarketHubUrl.Should().Be("https://sim.example.invalid/hubs/market");
    }

    [Fact]
    public void WebSocketOptions_ShouldPreferTheNestedSpelling_WhenBothAreSet()
    {
        // The more specific key wins, so adding the fallback cannot change what an existing deployment resolves.
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ProjectX:WebSocketUserHubUrl"] = "https://outer.example.invalid/hubs/user",
            ["ProjectX:WebSocket:UserHubUrl"] = "https://nested.example.invalid/hubs/user",
        });

        provider.GetRequiredService<IOptions<WebSocketOptions>>().Value
            .UserHubUrl.Should().Be("https://nested.example.invalid/hubs/user");
    }

    [Fact]
    public void WebSocketOptions_ShouldKeepTheProductionDefault_WhenNeitherSpellingIsSet()
    {
        using var provider = BuildProvider([]);

        provider.GetRequiredService<IOptions<WebSocketOptions>>().Value
            .UserHubUrl.Should().Be("https://rtc.topstepx.com/hubs/user");
    }

    #endregion

    #region Hub connection settings

    private static ProjectXWebSocketClient ClientWith(WebSocketOptions options) =>
        new(A.Fake<IAuthenticationService>(),
            Options.Create(options),
            NullLoggerFactory.Instance,
            new NullLogger<ProjectXWebSocketClient>());

    [Fact]
    public async Task BuildHubConnection_ShouldApplyTheConfiguredTimeouts()
    {
        var client = ClientWith(new WebSocketOptions
        {
            HandshakeTimeoutSeconds = 42,
            KeepAliveIntervalSeconds = 11,
            ServerTimeoutSeconds = 77,
        });

        await using var connection = client.BuildHubConnection("https://hub.example.invalid/hubs/user");

        connection.HandshakeTimeout.Should().Be(TimeSpan.FromSeconds(42));
        connection.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(11));
        connection.ServerTimeout.Should().Be(TimeSpan.FromSeconds(77));
    }

    [Fact]
    public async Task BuildHubConnection_ShouldApplyTheDocumentedDefaults_WhenNothingIsConfigured()
    {
        var client = ClientWith(new WebSocketOptions());

        await using var connection = client.BuildHubConnection("https://hub.example.invalid/hubs/user");

        connection.HandshakeTimeout.Should().Be(TimeSpan.FromSeconds(15));
        connection.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(15));
        connection.ServerTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region AutoReconnect

    // Returning null is how SignalR is told to stop. Previously WithAutomaticReconnect was applied
    // unconditionally and the flag only gated a log line, so AutoReconnect: false reconnected anyway.

    [Fact]
    public void ReconnectPolicy_ShouldStopRetrying_WhenAutoReconnectIsDisabled()
    {
        var policy = new ProjectXWebSocketClient.ReconnectPolicy(new WebSocketOptions { AutoReconnect = false });

        policy.NextRetryDelay(new RetryContext { PreviousRetryCount = 0 }).Should().BeNull();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]   // capped at MaxReconnectDelaySeconds
    [InlineData(9, 5)]
    public void ReconnectPolicy_ShouldBackOffExponentiallyAndCap_WhenAutoReconnectIsEnabled(long attempt, int expectedSeconds)
    {
        var policy = new ProjectXWebSocketClient.ReconnectPolicy(new WebSocketOptions
        {
            AutoReconnect = true,
            InitialReconnectDelaySeconds = 1,
            MaxReconnectDelaySeconds = 5,
        });

        policy.NextRetryDelay(new RetryContext { PreviousRetryCount = attempt })
            .Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    #endregion
}
