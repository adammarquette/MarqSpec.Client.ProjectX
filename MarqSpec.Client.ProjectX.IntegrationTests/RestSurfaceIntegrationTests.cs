using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.IntegrationTests;

/// <summary>
/// The REST surface end to end against the fake gateway — real HTTP, real auth handshake, real serialization.
/// No credentials required (R-11.2).
/// </summary>
[Collection(FakeGatewayCollection.Name)]
[Trait("Category", "Integration")]
public class RestSurfaceIntegrationTests : IAsyncLifetime
{
    private readonly FakeGatewayFixture _gateway;
    private ServiceProvider _services = null!;
    private IProjectXApiClient _client = null!;

    public RestSurfaceIntegrationTests(FakeGatewayFixture gateway) => _gateway = gateway;

    public async Task InitializeAsync()
    {
        await _gateway.ResetAsync();
        _services = _gateway.BuildClient();
        _client = _services.GetRequiredService<IProjectXApiClient>();
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task GetAccountsAsync_ShouldReturnOnlyTradableAccounts_WhenActiveOnlyRequested()
    {
        var accounts = (await _client.GetAccountsAsync(onlyActiveAccounts: true)).ToList();

        accounts.Should().NotBeEmpty();
        accounts.Should().OnlyContain(a => a.CanTrade && a.IsVisible,
            "the gateway seeds an archived account that the active-only filter must exclude");
        accounts.Should().NotContain(a => a.Name == "ARCHIVED");
    }

    [Fact]
    public async Task SearchContractsAsync_ShouldRoundTripThroughRealHttp()
    {
        var contracts = (await _client.SearchContractsAsync("ENQ", live: false)).ToList();

        contracts.Should().NotBeEmpty();
        contracts.Should().OnlyContain(c => c.Name.Contains("ENQ") || c.Description.Contains("ENQ"));
        contracts.Should().Contain(c => c.TickSize == 0.25m,
            "tick size must survive as a decimal — a float here would be a defect (R-12.3)");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_ShouldSucceed_ProvingEnumsTravelAsIntegers()
    {
        // The regression this pins: Refit's default wrote the unit as the STRING "minute" and the gateway
        // rejected every request with 400. Reaching a 200 at all proves the integer encoding survives.
        var bars = (await _client.GetHistoricalBarsAsync(
            FakeGatewayFixture.KnownContractId,
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow,
            AggregateBarUnit.Minute,
            unitNumber: 5,
            limit: 10)).ToList();

        bars.Should().NotBeEmpty();
        bars.Should().OnlyContain(b => b.High >= b.Low);
        bars.Should().BeInAscendingOrder(b => b.Timestamp);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldBeVisibleToASubsequentSearch()
    {
        var placed = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 2,
            LimitPrice = 21_000.25m,
            CustomTag = "integration",
        });

        placed.Success.Should().BeTrue();
        placed.OrderId.Should().NotBeNull();

        var open = (await _client.GetOpenOrdersAsync(FakeGatewayFixture.KnownAccountId)).ToList();

        open.Should().ContainSingle(o => o.Id == placed.OrderId!.Value)
            .Which.Should().Match<Order>(o =>
                o.Size == 2 && o.LimitPrice == 21_000.25m && o.CustomTag == "integration");
    }

    [Fact]
    public async Task PlaceMarketOrder_ShouldOpenAPositionAndRecordAHalfTrade()
    {
        await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Market,
            Side = OrderSide.Bid,
            Size = 3,
        });

        var positions = (await _client.GetOpenPositionsAsync(FakeGatewayFixture.KnownAccountId)).ToList();
        var trades = (await _client.GetTradesAsync(FakeGatewayFixture.KnownAccountId)).ToList();

        positions.Should().ContainSingle()
            .Which.Should().Match<Position>(p => p.Size == 3 && p.Type == PositionType.Long);
        trades.Should().ContainSingle().Which.Size.Should().Be(3);
    }

    [Fact]
    public async Task ClosePositionAsync_ShouldRemoveTheOpenPosition()
    {
        await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Market,
            Side = OrderSide.Bid,
            Size = 1,
        });

        var closed = await _client.ClosePositionAsync(FakeGatewayFixture.KnownAccountId, FakeGatewayFixture.KnownContractId);

        closed.Success.Should().BeTrue();
        (await _client.GetOpenPositionsAsync(FakeGatewayFixture.KnownAccountId)).Should().BeEmpty();
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldSucceedTwice_BecauseCancelIsIdempotent()
    {
        var placed = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Limit,
            Side = OrderSide.Ask,
            Size = 1,
            LimitPrice = 22_000m,
        });

        var first = await _client.CancelOrderAsync(FakeGatewayFixture.KnownAccountId, placed.OrderId!.Value);
        var second = await _client.CancelOrderAsync(FakeGatewayFixture.KnownAccountId, placed.OrderId!.Value);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue(
            "cancel being safely repeatable is the property ADR-0002 relies on when it excludes only placement");
    }

    [Fact]
    public async Task GetContractByIdAsync_ShouldReturnNull_ForAnUnknownContract()
    {
        var contract = await _client.GetContractByIdAsync("CON.F.US.NOPE.Z99");

        contract.Should().BeNull();
    }

    [Fact]
    public async Task PingAsync_ShouldReportTheGatewayAsReachable()
    {
        var reachable = await _client.PingAsync();

        reachable.Should().BeTrue();
    }

    #region Session routes (gh#56)

    // GatewayMiddleware enforces the bearer on every /api path except /Auth/loginKey, exactly as the venue
    // does. Through v1.0.5 both of these went out with no Authorization header at all, so both answered 401 --
    // and neither had a test in either project to notice.

    [Fact]
    public async Task ValidateSessionAsync_ShouldSucceed_WhenTheGatewayEnforcesTheBearer()
    {
        var auth = _services.GetRequiredService<IAuthenticationService>();
        await auth.GetAccessTokenAsync();

        var valid = await auth.ValidateSessionAsync();

        valid.Should().BeTrue("the request must carry the bearer the service already holds");
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldAdoptTheRenewedToken_WhenTheGatewayIssuesOne()
    {
        var auth = _services.GetRequiredService<IAuthenticationService>();
        var original = await auth.GetAccessTokenAsync();

        await auth.ValidateSessionAsync();
        var current = await auth.GetAccessTokenAsync();

        // The fake reissues on validate, so a client that ignored newToken would keep the original.
        current.Should().NotBe(original);
    }

    [Fact]
    public async Task LogoutAsync_ShouldBeAcceptedByTheGateway_WhenATokenIsHeld()
    {
        var auth = _services.GetRequiredService<IAuthenticationService>();
        await auth.GetAccessTokenAsync();

        var act = async () => await auth.LogoutAsync();

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Order search window (gh#57)

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnTheOrder_WhenTheWindowContainsIt()
    {
        var placed = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Market,
            Side = OrderSide.Bid,
            Size = 1,
        });

        var orders = await _client.GetOrdersAsync(
            FakeGatewayFixture.KnownAccountId, DateTime.UtcNow.AddMinutes(-5));

        orders.Should().Contain(o => o.Id == placed.OrderId);
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldExcludeTheOrder_WhenTheWindowStartsAfterIt()
    {
        // The whole point of gh#57: outside the window an order is simply absent, and absent is
        // indistinguishable from "never existed". That is why the window can never be chosen for the caller.
        await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Market,
            Side = OrderSide.Bid,
            Size = 1,
        });

        var orders = await _client.GetOrdersAsync(
            FakeGatewayFixture.KnownAccountId, DateTime.UtcNow.AddMinutes(5));

        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderAsync_ShouldFindThePlacedOrder_WhenGivenAWindowThatContainsIt()
    {
        var placed = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            AccountId = FakeGatewayFixture.KnownAccountId,
            ContractId = FakeGatewayFixture.KnownContractId,
            Type = OrderType.Market,
            Side = OrderSide.Bid,
            Size = 1,
        });

        placed.OrderId.Should().NotBeNull();

        var order = await _client.GetOrderAsync(
            FakeGatewayFixture.KnownAccountId, placed.OrderId!.Value, DateTime.UtcNow.AddMinutes(-5));

        order.Should().NotBeNull();
        order!.Id.Should().Be(placed.OrderId);
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldFailBeforeTheWire_WhenNoWindowIsSupplied()
    {
        var act = async () => await _client.GetOrdersAsync(FakeGatewayFixture.KnownAccountId);

        await act.Should().ThrowAsync<ArgumentException>();

        // And the gateway would have rejected it anyway -- the fake now enforces the schema's required field,
        // so the client's guard is a second line rather than the only one.
        (await _gateway.RequestCountAsync("/api/Order/search")).Should().Be(0);
    }

    #endregion
}
