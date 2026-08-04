using FluentAssertions;
using MarqSpec.Client.ProjectX.DependencyInjection;
using System.Net;

namespace MarqSpec.Client.ProjectX.Tests.Resilience;

/// <summary>
/// The order-placement route is non-idempotent, so the gateway retry pipeline must <b>never</b> retry it: a lost
/// acknowledgement or a post-acceptance <c>5xx</c> would place a second live order (gh#629). These pin the real
/// <see cref="ServiceCollectionExtensions.ShouldRetryOutcome"/> the pipeline is wired to — a placement fault is
/// surfaced to the caller (which classifies it as indeterminate and leaves the intent for reconciliation),
/// while an idempotent read still gets the standard transient-fault retry.
/// </summary>
public class PlaceOrderRetryTests
{
    private static HttpRequestMessage Request(string path) =>
        new(HttpMethod.Post, new Uri($"https://gateway.example.com{path}"));

    private static readonly HttpRequestMessage Place = Request("/api/Order/place");
    private static readonly HttpRequestMessage Search = Request("/api/Order/search");

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnTransportFault_DoesNotRetry()
    {
        // The dangerous case: a transport fault after the gateway may already have booked the order.
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Place, new HttpRequestException("connection reset"), response: null)
            .Should().BeFalse("retrying a non-idempotent place after a lost ack would double-place (gh#629)");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnServerError_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Place, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeFalse("a 5xx may follow an accepted place — a retry would double-place");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnTooManyRequests_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Place, exception: null, new HttpResponseMessage(HttpStatusCode.TooManyRequests))
            .Should().BeFalse("even a 429 is not safe to auto-retry for a non-idempotent place");
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnTransportFault_Retries()
    {
        // A read/search is idempotent — the standard transient-fault retry must still apply.
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Search, new HttpRequestException("connection reset"), response: null)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnServerError_Retries()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Search, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnTooManyRequests_Retries()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Search, exception: null, new HttpResponseMessage(HttpStatusCode.TooManyRequests))
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnSuccess_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Search, exception: null, new HttpResponseMessage(HttpStatusCode.OK))
            .Should().BeFalse("a successful response is not a transient fault");
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnBadRequest_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(Search, exception: null, new HttpResponseMessage(HttpStatusCode.BadRequest))
            .Should().BeFalse("a 4xx is a caller error, not a transient fault");
    }

    [Fact]
    public void ShouldRetryOutcome_ForUnidentifiedRequestOnServerError_Retries()
    {
        // Defensive: if the request can't be read we cannot confirm a placement, so the standard retry still
        // applies — this guard is never *less* resilient than the pipeline was before it (gh#629).
        ServiceCollectionExtensions
            .ShouldRetryOutcome(request: null, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/Order/place", true)]
    [InlineData("/api/Order/search", false)]
    [InlineData("/api/Order/searchOpen", false)]
    [InlineData("/api/Order/cancel", false)]
    [InlineData("/api/Order/modify", false)]
    [InlineData("/api/Position/closeContract", false)]
    [InlineData("/api/Status/ping", false)]
    public void IsOrderPlacement_IdentifiesOnlyThePlaceRoute(string path, bool expected)
    {
        ServiceCollectionExtensions.IsOrderPlacement(Request(path)).Should().Be(expected);
    }

    [Fact]
    public void IsOrderPlacement_ForNullRequest_IsFalse()
    {
        ServiceCollectionExtensions.IsOrderPlacement(null).Should().BeFalse();
    }
}
