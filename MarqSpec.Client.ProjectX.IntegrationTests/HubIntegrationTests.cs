using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.IntegrationTests;

/// <summary>
/// The SignalR hub client over a real WebSocket transport.
/// </summary>
/// <remarks>
/// This is the surface a request-stubbing tool could not have reached, and the reason ADR-0007 chose a real
/// application over recorded mappings. <c>IProjectXWebSocketClient</c> is this library's most-used type in
/// trading-copilot, and until now nothing exercised it outside a unit test with everything mocked.
/// </remarks>
[Collection(FakeGatewayCollection.Name)]
[Trait("Category", "Integration")]
public class HubIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(15);

    private readonly FakeGatewayFixture _gateway;
    private ServiceProvider _services = null!;
    private IProjectXWebSocketClient _hub = null!;

    public HubIntegrationTests(FakeGatewayFixture gateway) => _gateway = gateway;

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

    [Fact]
    public async Task ConnectUserHubAsync_ShouldSupplyAFreshlyAcquiredBearerToken()
    {
        await _hub.ConnectUserHubAsync();

        // The gateway records the token it saw on the handshake. Its presence proves the client went through
        // AuthenticationService rather than connecting anonymously — the path that broke when a stale captured
        // token was replayed on reconnect.
        var hubs = await _gateway.HubTokensSeenAsync();

        hubs.Should().Contain("user", "the user hub handshake must carry an access token");
    }

    [Fact]
    public async Task SubscribeToOrderUpdates_ShouldReachTheHub_NotJustTheClient()
    {
        await _hub.ConnectUserHubAsync();
        await _hub.SubscribeToOrderUpdatesAsync(FakeGatewayFixture.KnownAccountId);

        var subscriptions = await _gateway.HubSubscriptionsAsync();

        subscriptions.Should().Contain($"user:SubscribeOrders:{FakeGatewayFixture.KnownAccountId}",
            "the subscription has to arrive at the venue — a client that records intent locally and never "
            + "sends it looks identical from the outside until no events arrive");
    }

    [Fact]
    public async Task SubscribeToOrderUpdates_ShouldDeliverAnOrderRaisedByTheVenue()
    {
        var received = new TaskCompletionSource<OrderUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.OrderUpdateReceived += (_, update) => received.TrySetResult(update);

        await _hub.ConnectUserHubAsync();
        await _hub.SubscribeToOrderUpdatesAsync(FakeGatewayFixture.KnownAccountId);

        await _gateway.EmitAsync("order", new
        {
            id = 4242L,
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
        });

        var update = await WaitFor(received.Task);

        update.Id.Should().Be(4242L);
        update.Size.Should().Be(3);
        update.Status.Should().Be(OrderStatus.Filled, "enums arrive as integers on the wire (ADR-0001)");
        update.FilledPrice.Should().Be(21_501.75m);
    }

    [Fact]
    public async Task SubscribeToPositionUpdates_ShouldDeliverAPositionRaisedByTheVenue()
    {
        var received = new TaskCompletionSource<PositionUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.PositionUpdateReceived += (_, update) => received.TrySetResult(update);

        await _hub.ConnectUserHubAsync();
        await _hub.SubscribeToPositionUpdatesAsync(FakeGatewayFixture.KnownAccountId);

        await _gateway.EmitAsync("position", new
        {
            id = 77,
            accountId = FakeGatewayFixture.KnownAccountId,
            contractId = FakeGatewayFixture.KnownContractId,
            creationTimestamp = DateTime.UtcNow,
            type = (int)PositionType.Short,
            size = 5,
            averagePrice = 21_490.50m,
        });

        var update = await WaitFor(received.Task);

        update.Type.Should().Be(PositionType.Short);
        update.Size.Should().Be(5);
        update.AveragePrice.Should().Be(21_490.50m);
    }

    [Fact]
    public async Task SubscribeToPriceUpdates_ShouldDeliverAQuote()
    {
        var received = new TaskCompletionSource<PriceUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.PriceUpdateReceived += (_, update) => received.TrySetResult(update);

        await _hub.ConnectMarketHubAsync();
        await _hub.SubscribeToPriceUpdatesAsync(FakeGatewayFixture.KnownContractId);

        await _gateway.EmitAsync("quote", new
        {
            contractId = FakeGatewayFixture.KnownContractId,
            update = new
            {
                symbol = FakeGatewayFixture.KnownContractId,
                lastPrice = 21_502.50m,
                bestBid = 21_502.25m,
                bestAsk = 21_502.75m,
                timestamp = DateTime.UtcNow,
            },
        });

        var update = await WaitFor(received.Task);

        update.LastPrice.Should().Be(21_502.50m);
        update.BestAsk.Should().BeGreaterThan(update.BestBid);
    }

    /// <summary>
    /// Pins the array-shaped market handlers.
    /// </summary>
    /// <remarks>
    /// <c>GatewayTrade</c> delivers an ARRAY. It was bound as a single object once, so every market trade event
    /// failed to deserialize and was silently dropped — no exception, no log, just nothing arriving. A shape
    /// regression here is invisible without a test that actually receives one.
    /// </remarks>
    [Fact]
    public async Task SubscribeToTradeUpdates_ShouldDeliverAnArrayOfTradeUpdates()
    {
        var received = new TaskCompletionSource<TradeUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.TradeUpdateReceived += (_, update) => received.TrySetResult(update);

        await _hub.ConnectMarketHubAsync();
        await _hub.SubscribeToTradeUpdatesAsync(FakeGatewayFixture.KnownContractId);

        await _gateway.EmitAsync("market-trade", new
        {
            contractId = FakeGatewayFixture.KnownContractId,
            updates = new[]
            {
                new { symbolId = "F.US.ENQ", price = 21_500.00m, timestamp = DateTime.UtcNow, type = 0, volume = 4m },
            },
        });

        var update = await WaitFor(received.Task);

        update.Price.Should().Be(21_500.00m);
        update.Volume.Should().Be(4m);
    }

    [Fact]
    public async Task SubscribeToOrderBookUpdates_ShouldDeliverAnArrayOfDepthUpdates()
    {
        var received = new TaskCompletionSource<OrderBookUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.OrderBookUpdateReceived += (_, update) => received.TrySetResult(update);

        await _hub.ConnectMarketHubAsync();
        await _hub.SubscribeToOrderBookUpdatesAsync(FakeGatewayFixture.KnownContractId);

        await _gateway.EmitAsync("depth", new
        {
            contractId = FakeGatewayFixture.KnownContractId,
            updates = new[]
            {
                new { timestamp = DateTime.UtcNow, type = 1, price = 21_499.75m, volume = 12m, currentVolume = 12 },
            },
        });

        var update = await WaitFor(received.Task);

        update.Price.Should().Be(21_499.75m);
        update.Volume.Should().Be(12m);
    }

    private static async Task<T> WaitFor<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(_receiveTimeout));
        if (completed != task)
        {
            throw new TimeoutException(
                $"No {typeof(T).Name} arrived within {_receiveTimeout.TotalSeconds:0}s. The handler is bound but "
                + "nothing was delivered — check the hub method name and the payload shape.");
        }

        return await task;
    }
}
