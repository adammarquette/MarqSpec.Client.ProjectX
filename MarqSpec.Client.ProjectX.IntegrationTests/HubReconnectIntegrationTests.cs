using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.IntegrationTests;

/// <summary>
/// Delivery after a real SignalR connection-id change (gh#92, R-5.3 / R-5.5 / R-5.6).
/// </summary>
/// <remarks>
/// gh#87's unit tests raise <c>Reconnected</c> on a fake connection. That cannot prove the venue dropped
/// group membership. These tests abort the socket at FakeGateway, wait for <see cref="ConnectionState.Connected"/>,
/// and then ask the gateway — not the client — whether a new subscribe arrived and whether a post-reconnect
/// emit is delivered.
/// </remarks>
[Collection(FakeGatewayCollection.Name)]
[Trait("Category", "Integration")]
public class HubReconnectIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _reconnectTimeout = TimeSpan.FromSeconds(15);

    private readonly FakeGatewayFixture _gateway;
    private ServiceProvider _services = null!;
    private IProjectXWebSocketClient _hub = null!;

    public HubReconnectIntegrationTests(FakeGatewayFixture gateway) => _gateway = gateway;

    public async Task InitializeAsync()
    {
        await _gateway.ResetAsync();
        _services = _gateway.BuildClient();
        _hub = _services.GetRequiredService<IProjectXWebSocketClient>();
    }

    public async Task DisposeAsync()
    {
        await _hub.DisposeAsync();
        await _services.DisposeAsync();
    }

    /// <summary>
    /// R-5.3 / R-5.6: after a forced market-hub drop, a subscribed quote stream delivers again — not
    /// merely <see cref="ConnectionState.Connected"/>.
    /// </summary>
    [Fact]
    public async Task SubscribeToPriceUpdates_ShouldDeliverAQuoteAfterAForcedMarketHubReconnect()
    {
        var first = new TaskCompletionSource<PriceUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<PriceUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.PriceUpdateReceived += (_, update) =>
        {
            if (!first.Task.IsCompleted)
            {
                first.TrySetResult(update);
                return;
            }

            second.TrySetResult(update);
        };

        await _hub.ConnectMarketHubAsync();
        await _hub.SubscribeToPriceUpdatesAsync(FakeGatewayFixture.KnownContractId);

        var connectionIdsBefore = await _gateway.HubConnectionIdsAsync("market");
        var subscribeCountBefore = await SubscribeCountAsync(_marketQuoteSubscribeKey);

        await _gateway.EmitAsync("quote", QuotePayload(21_502.50m));
        (await WaitFor(first.Task)).LastPrice.Should().Be(21_502.50m);

        var reconnected = WaitForNextConnected();
        var aborted = await _gateway.AbortHubAsync("market");
        aborted.Should().BeGreaterThan(0, "the fake must actually drop the market socket");
        await WaitFor(reconnected, _reconnectTimeout);

        _hub.MarketHubState.Should().Be(ConnectionState.Connected);

        var connectionIdsAfter = await _gateway.HubConnectionIdsAsync("market");
        connectionIdsAfter.Should().NotBeEmpty("the client must have a live market connection after reconnect");
        connectionIdsAfter.Should().NotIntersectWith(
            connectionIdsBefore,
            "abort must produce a new connection id — the path subscriptions do not survive");

        var subscribeCountAfter = await SubscribeCountAsync(_marketQuoteSubscribeKey);
        subscribeCountAfter.Should().BeGreaterThan(
            subscribeCountBefore,
            "a new SubscribeContractQuotes must reach the fake; Connected without that invoke is the gh#87 lie");

        await _gateway.EmitAsync("quote", QuotePayload(21_510.00m));
        (await WaitFor(second.Task)).LastPrice.Should().Be(21_510.00m);
    }

    /// <summary>
    /// The same proof on the user hub (orders). gh#87 named both hubs.
    /// </summary>
    [Fact]
    public async Task SubscribeToOrderUpdates_ShouldDeliverAnOrderAfterAForcedUserHubReconnect()
    {
        var first = new TaskCompletionSource<OrderUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<OrderUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.OrderUpdateReceived += (_, update) =>
        {
            if (!first.Task.IsCompleted)
            {
                first.TrySetResult(update);
                return;
            }

            second.TrySetResult(update);
        };

        await _hub.ConnectUserHubAsync();
        await _hub.SubscribeToOrderUpdatesAsync(FakeGatewayFixture.KnownAccountId);

        var connectionIdsBefore = await _gateway.HubConnectionIdsAsync("user");
        var subscribeCountBefore = await SubscribeCountAsync(_userOrderSubscribeKey);

        await _gateway.EmitAsync("order", OrderPayload(4242));
        (await WaitFor(first.Task)).Id.Should().Be(4242);

        var reconnected = WaitForNextConnected();
        var aborted = await _gateway.AbortHubAsync("user");
        aborted.Should().BeGreaterThan(0, "the fake must actually drop the user socket");
        await WaitFor(reconnected, _reconnectTimeout);

        _hub.UserHubState.Should().Be(ConnectionState.Connected);

        var connectionIdsAfter = await _gateway.HubConnectionIdsAsync("user");
        connectionIdsAfter.Should().NotBeEmpty();
        connectionIdsAfter.Should().NotIntersectWith(connectionIdsBefore);

        var subscribeCountAfter = await SubscribeCountAsync(_userOrderSubscribeKey);
        subscribeCountAfter.Should().BeGreaterThan(
            subscribeCountBefore,
            "a new SubscribeOrders must reach the fake; Connected without that invoke is the gh#87 lie");

        await _gateway.EmitAsync("order", OrderPayload(4243));
        (await WaitFor(second.Task)).Id.Should().Be(4243);
    }

    /// <summary>
    /// The gh#87 lie, constructed at the gateway: a connection that reports Connected after abort
    /// without sending a new subscribe. The live-tape guard used by the tests above must fail.
    /// </summary>
    /// <remarks>
    /// This does not use <see cref="IProjectXWebSocketClient"/> — it is a bare SignalR client with
    /// automatic reconnect and no restore. That is the only way to put the lie on the wire without
    /// editing <c>MarqSpec.Client.ProjectX/</c>. The assertion is the same gateway-side count the
    /// library tests require to grow.
    /// </remarks>
    [Fact]
    public async Task LiveTapeGuard_ShouldFail_WhenReconnectReportsConnectedWithoutANewSubscribe()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl($"{_gateway.BaseAddress}/hubs/market")
            .WithAutomaticReconnect()
            .Build();

        var received = new TaskCompletionSource<decimal>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string, PriceUpdate>("GatewayQuote", (_, update) => received.TrySetResult(update.LastPrice));

        var reconnected = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Reconnected += id =>
        {
            reconnected.TrySetResult(id);
            return Task.CompletedTask;
        };

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeContractQuotes", FakeGatewayFixture.KnownContractId);

        var subscribeCountBefore = await SubscribeCountAsync(_marketQuoteSubscribeKey);
        subscribeCountBefore.Should().BeGreaterThan(0);

        await _gateway.EmitAsync("quote", QuotePayload(21_500.00m));
        (await WaitFor(received.Task)).Should().Be(21_500.00m);

        var aborted = await _gateway.AbortHubAsync("market");
        aborted.Should().BeGreaterThan(0, "the fake must actually drop the market socket");
        await WaitFor(reconnected.Task, _reconnectTimeout);

        connection.State.Should().Be(HubConnectionState.Connected, "the lie starts from a live socket");

        var subscribeCountAfter = await SubscribeCountAsync(_marketQuoteSubscribeKey);

        Action liveTapeGuard = () => subscribeCountAfter.Should().BeGreaterThan(
            subscribeCountBefore,
            "the library tests treat a new subscribe at the gateway as the proof that tape is live");

        liveTapeGuard.Should().Throw<Exception>(
            "this is the gh#87 lie: Connected after a new connection id, no SubscribeContractQuotes on the wire");

        var afterReconnect = new TaskCompletionSource<decimal>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string, PriceUpdate>("GatewayQuote", (_, update) => afterReconnect.TrySetResult(update.LastPrice));

        await _gateway.EmitAsync("quote", QuotePayload(21_599.00m));

        var completed = await Task.WhenAny(afterReconnect.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().NotBe(
            afterReconnect.Task,
            "group membership died with the old connection id — emit must not reach a reconnect that never re-subscribed");
    }

    private static readonly string _marketQuoteSubscribeKey =
        $"market:SubscribeContractQuotes:{FakeGatewayFixture.KnownContractId}";

    private static readonly string _userOrderSubscribeKey =
        $"user:SubscribeOrders:{FakeGatewayFixture.KnownAccountId}";

    private async Task<int> SubscribeCountAsync(string recorded)
    {
        var subscriptions = await _gateway.HubSubscriptionsAsync();
        return subscriptions.Count(s => s == recorded);
    }

    private Task<ConnectionStatusChange> WaitForNextConnected()
    {
        var arrived = new TaskCompletionSource<ConnectionStatusChange>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChange(object? _, ConnectionStatusChange change)
        {
            if (change.CurrentState != ConnectionState.Connected)
            {
                return;
            }

            _hub.ConnectionStatusChanged -= OnChange;
            arrived.TrySetResult(change);
        }

        _hub.ConnectionStatusChanged += OnChange;
        return arrived.Task;
    }

    private static object QuotePayload(decimal lastPrice) => new
    {
        contractId = FakeGatewayFixture.KnownContractId,
        update = new
        {
            symbol = FakeGatewayFixture.KnownContractId,
            lastPrice,
            bestBid = lastPrice - 0.25m,
            bestAsk = lastPrice + 0.25m,
            timestamp = DateTime.UtcNow,
        },
    };

    private static object OrderPayload(long id) => new
    {
        id,
        accountId = FakeGatewayFixture.KnownAccountId,
        contractId = FakeGatewayFixture.KnownContractId,
        symbolId = "F.US.ENQ",
        creationTimestamp = DateTime.UtcNow,
        status = (int)OrderStatus.Filled,
        type = (int)OrderType.Market,
        side = (int)OrderSide.Bid,
        size = 3,
        fillVolume = 3,
        filledPrice = 21_501.75m,
    };

    private static async Task<T> WaitFor<T>(Task<T> task, TimeSpan? timeout = null)
    {
        var limit = timeout ?? _receiveTimeout;
        var completed = await Task.WhenAny(task, Task.Delay(limit));
        if (completed != task)
        {
            throw new TimeoutException(
                $"Nothing arrived within {limit.TotalSeconds:0}s. For reconnect tests that usually means the "
                + "hub never reported Connected, or the post-reconnect emit was not delivered.");
        }

        return await task;
    }
}
