using System.Net;
using FluentAssertions;
using MarqSpec.Client.ProjectX.DependencyInjection;

namespace MarqSpec.Client.ProjectX.Tests.Resilience;

/// <summary>
/// The order-placement route is non-idempotent, so the gateway retry pipeline must <b>never</b> retry it: a lost
/// acknowledgement or a post-acceptance <c>5xx</c> would place a second live order (gh#629). These pin the real
/// <see cref="ServiceCollectionExtensions.ShouldRetryOutcome"/> the pipeline is wired to — a placement fault is
/// surfaced to the caller (which classifies it as indeterminate and leaves the intent for reconciliation),
/// while an idempotent read still gets the standard transient-fault retry. A sized partial close is the same
/// class of write and is pinned alongside it (gh#98).
/// </summary>
public class PlaceOrderRetryTests
{
    private static HttpRequestMessage Request(string path) =>
        new(HttpMethod.Post, new Uri($"https://gateway.example.com{path}"));

    private static readonly HttpRequestMessage _place = Request("/api/Order/place");
    private static readonly HttpRequestMessage _search = Request("/api/Order/search");
    private static readonly HttpRequestMessage _partialClose = Request("/api/Position/partialCloseContract");

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnTransportFault_DoesNotRetry()
    {
        // The dangerous case: a transport fault after the gateway may already have booked the order.
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_place, new HttpRequestException("connection reset"), response: null)
            .Should().BeFalse("retrying a non-idempotent place after a lost ack would double-place (gh#629)");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnServerError_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_place, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeFalse("a 5xx may follow an accepted place — a retry would double-place");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPlaceOnTooManyRequests_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_place, exception: null, new HttpResponseMessage(HttpStatusCode.TooManyRequests))
            .Should().BeFalse("even a 429 is not safe to auto-retry for a non-idempotent place");
    }

    // gh#98: a sized partial close accumulates across replays exactly as a placement does -- reduce-by-1
    // retried three times takes up to four contracts off, and can overshoot past flat into an opposing position.
    [Fact]
    public void ShouldRetryOutcome_ForPartialCloseOnTransportFault_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_partialClose, new HttpRequestException("connection reset"), response: null)
            .Should().BeFalse("retrying a partial close after a lost ack would reduce the position twice (gh#98)");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPartialCloseOnServerError_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_partialClose, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeFalse("a 5xx may follow an accepted partial close -- a retry would reduce twice (gh#98)");
    }

    [Fact]
    public void ShouldRetryOutcome_ForPartialCloseOnTooManyRequests_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_partialClose, exception: null, new HttpResponseMessage(HttpStatusCode.TooManyRequests))
            .Should().BeFalse("even a 429 is not safe to auto-retry for a non-idempotent partial close (gh#98)");
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnTransportFault_Retries()
    {
        // A read/search is idempotent — the standard transient-fault retry must still apply.
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_search, new HttpRequestException("connection reset"), response: null)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnServerError_Retries()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_search, exception: null, new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnTooManyRequests_Retries()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_search, exception: null, new HttpResponseMessage(HttpStatusCode.TooManyRequests))
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnSuccess_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_search, exception: null, new HttpResponseMessage(HttpStatusCode.OK))
            .Should().BeFalse("a successful response is not a transient fault");
    }

    [Fact]
    public void ShouldRetryOutcome_ForSearchOnBadRequest_DoesNotRetry()
    {
        ServiceCollectionExtensions
            .ShouldRetryOutcome(_search, exception: null, new HttpResponseMessage(HttpStatusCode.BadRequest))
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
    [InlineData("/api/Position/partialCloseContract", true)]
    [InlineData("/API/POSITION/PARTIALCLOSECONTRACT", true)]
    [InlineData("/api/Order/search", false)]
    [InlineData("/api/Order/searchOpen", false)]
    [InlineData("/api/Order/cancel", false)]
    [InlineData("/api/Order/modify", false)]
    [InlineData("/api/Position/closeContract", false)]
    [InlineData("/api/Position/searchOpen", false)]
    [InlineData("/api/Status/ping", false)]
    public void IsNonIdempotentWrite_IdentifiesOnlyThePlaceAndPartialCloseRoutes(string path, bool expected)
    {
        ServiceCollectionExtensions.IsNonIdempotentWrite(Request(path)).Should().Be(expected);
    }

    [Fact]
    public void IsNonIdempotentWrite_ForNullRequest_IsFalse()
    {
        ServiceCollectionExtensions.IsNonIdempotentWrite(null).Should().BeFalse();
    }
}
