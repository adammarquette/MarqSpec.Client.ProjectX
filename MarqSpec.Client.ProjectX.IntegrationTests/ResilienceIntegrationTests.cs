using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MarqSpec.Client.ProjectX.IntegrationTests;

/// <summary>
/// The resilience pipeline as it actually behaves over the wire, counted at the gateway.
/// </summary>
/// <remarks>
/// The unit tests cover <c>ShouldRetryOutcome</c> as a predicate. These cover what the predicate cannot prove
/// on its own: how many requests the composed pipeline really sends. A predicate can be perfectly correct while
/// the pipeline is wired so that it is never consulted.
/// <para>
/// The count comes from the <i>server</i>, not from the client's account of itself, so the guard holds by
/// construction.
/// </para>
/// </remarks>
[Collection(FakeGatewayCollection.Name)]
[Trait("Category", "Integration")]
public class ResilienceIntegrationTests : IAsyncLifetime
{
    private readonly FakeGatewayFixture _gateway;
    private ServiceProvider _services = null!;
    private IProjectXApiClient _client = null!;

    public ResilienceIntegrationTests(FakeGatewayFixture gateway) => _gateway = gateway;

    public async Task InitializeAsync()
    {
        await _gateway.ResetAsync();
        _services = _gateway.BuildClient();
        _client = _services.GetRequiredService<IProjectXApiClient>();
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    private static PlaceOrderRequest MarketOrder() => new()
    {
        AccountId = FakeGatewayFixture.KnownAccountId,
        ContractId = FakeGatewayFixture.KnownContractId,
        Type = OrderType.Market,
        Side = OrderSide.Bid,
        Size = 1,
    };

    /// <summary>
    /// The one with money behind it (ADR-0002, R-3.3).
    /// </summary>
    /// <remarks>
    /// A 5xx on a placement is ambiguous: the venue may have booked the order and lost the acknowledgement. A
    /// retry would place a SECOND live order.
    /// </remarks>
    [Fact]
    public async Task PlaceOrder_ShouldReachTheGatewayExactlyOnce_WhenItFailsWith500()
    {
        await _gateway.ArmFaultAsync(new { pathSuffix = "/api/Order/place", status = 500, remaining = 5 });

        var act = async () => await _client.PlaceOrderAsync(MarketOrder());

        await act.Should().ThrowAsync<Exception>("the fault must surface to the caller, not be retried away");

        var attempts = await _gateway.RequestCountAsync("/api/Order/place");

        attempts.Should().Be(1,
            "placing an order is not idempotent — a retry after a lost acknowledgement is a second live order");
    }

    [Fact]
    public async Task PlaceOrder_ShouldNotRetry_EvenOn429()
    {
        await _gateway.ArmFaultAsync(new { pathSuffix = "/api/Order/place", status = 429, remaining = 5, retryAfterSeconds = 1 });

        var act = async () => await _client.PlaceOrderAsync(MarketOrder());

        await act.Should().ThrowAsync<Exception>();

        var attempts = await _gateway.RequestCountAsync("/api/Order/place");

        attempts.Should().Be(1, "even a rate-limit response is not safe to auto-retry for a placement");
    }

    /// <summary>
    /// A window wide enough to contain everything the gateway seeds. The order search requires one — see
    /// gh#57 — so these retry tests must supply it; they are about the pipeline, not about the window.
    /// </summary>
    private static DateTime SearchWindowStart => DateTime.UtcNow.AddDays(-30);

    /// <summary>The other side of the boundary: an idempotent read must still be retried.</summary>
    [Fact]
    public async Task GetOrders_ShouldRetryAndSucceed_WhenTheFirstAttemptsFail()
    {
        await _gateway.ArmFaultAsync(new { pathSuffix = "/api/Order/search", status = 500, remaining = 2 });

        var orders = await _client.GetOrdersAsync(FakeGatewayFixture.KnownAccountId, SearchWindowStart);

        orders.Should().NotBeNull();

        var attempts = await _gateway.RequestCountAsync("/api/Order/search");

        attempts.Should().Be(3, "two injected failures plus the successful third attempt");
    }

    [Fact]
    public async Task GetOrders_ShouldHonourRetryAfter_WhenExpressedAsAnHttpDate()
    {
        // The HTTP-date form is the encoding that is easy to get wrong, because it has to be converted against
        // UtcNow rather than read as a duration.
        await _gateway.ArmFaultAsync(new
        {
            pathSuffix = "/api/Order/search",
            status = 429,
            remaining = 1,
            retryAfterSeconds = 1,
            retryAfterAsHttpDate = true,
        });

        var orders = await _client.GetOrdersAsync(FakeGatewayFixture.KnownAccountId, SearchWindowStart);

        orders.Should().NotBeNull();
        (await _gateway.RequestCountAsync("/api/Order/search")).Should().Be(2);
    }

    [Fact]
    public async Task GetOrders_ShouldGiveUpAfterThreeRetries_WhenTheGatewayNeverRecovers()
    {
        await _gateway.ArmFaultAsync(new { pathSuffix = "/api/Order/search", status = 500, remaining = 99 });

        var act = async () => await _client.GetOrdersAsync(FakeGatewayFixture.KnownAccountId, SearchWindowStart);

        await act.Should().ThrowAsync<Exception>();

        var attempts = await _gateway.RequestCountAsync("/api/Order/search");

        attempts.Should().Be(4, "the pipeline allows 3 retries on top of the initial attempt");
    }
}
