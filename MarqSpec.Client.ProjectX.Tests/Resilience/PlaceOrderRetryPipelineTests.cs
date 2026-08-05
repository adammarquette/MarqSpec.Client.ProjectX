using System.Collections.Concurrent;
using System.Net;
using FluentAssertions;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace MarqSpec.Client.ProjectX.Tests.Resilience;

/// <summary>
/// Proves the safety property end-to-end through a real <c>Microsoft.Extensions.Http.Resilience</c> standard
/// resilience pipeline: the retry strategy reads the in-flight request via
/// <c>ResilienceContext.GetRequestMessage()</c>, so
/// <see cref="ServiceCollectionExtensions.ShouldRetryOutcome"/> can exclude a placement (gh#629). Without this,
/// a green predicate-only unit test could still hide an <i>inert</i> guard: if the request were null at runtime
/// the placement would fall through to the standard retry and double-place. Here the placement is driven through
/// the actual pipeline and asserted to be sent exactly once — on a 5xx <b>and</b> on a transport fault (the
/// lost-ack case) — while a search still retries, so the placement assertion is not vacuously passing.
/// </summary>
public class PlaceOrderRetryPipelineTests
{
    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly Exception? _throwOnSend;

        public CountingHandler(Exception? throwOnSend = null) => _throwOnSend = throwOnSend;

        public ConcurrentDictionary<string, int> Attempts { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts.AddOrUpdate(request.RequestUri!.AbsolutePath, 1, (_, n) => n + 1);

            if (_throwOnSend is not null)
            {
                throw _throwOnSend;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private static (IHttpClientFactory Factory, CountingHandler Handler) BuildPipeline(Exception? throwOnSend = null)
    {
        var handler = new CountingHandler(throwOnSend);
        var services = new ServiceCollection();
        services.AddHttpClient("gw", c => c.BaseAddress = new Uri("https://gateway.test"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                // The same retry decision production wires (ShouldRetryOutcome reading the request off the
                // resilience context); the delay is zeroed so the test is fast — the delay itself is covered
                // separately by RetryAfterTests.
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.Zero;
                options.Retry.BackoffType = DelayBackoffType.Constant;
                options.Retry.UseJitter = false;
                options.Retry.ShouldHandle = args => new ValueTask<bool>(
                    ServiceCollectionExtensions.ShouldRetryOutcome(
                        args.Context.GetRequestMessage(),
                        args.Outcome.Exception,
                        args.Outcome.Result));
            });

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IHttpClientFactory>(), handler);
    }

    [Fact]
    public async Task PlacePost_OnServerError_IsSentExactlyOnce()
    {
        var (factory, handler) = BuildPipeline();
        var client = factory.CreateClient("gw");

        await client.PostAsync("/api/Order/place", content: null);

        handler.Attempts["/api/Order/place"].Should().Be(1,
            "a place is non-idempotent — the pipeline must not retry it even on a 5xx (gh#629)");
    }

    [Fact]
    public async Task PlacePost_OnTransportFault_IsSentExactlyOnce()
    {
        // The dangerous case: the first attempt may already have booked the order before the ack was lost.
        var (factory, handler) = BuildPipeline(new HttpRequestException("connection reset"));
        var client = factory.CreateClient("gw");

        var act = async () => await client.PostAsync("/api/Order/place", content: null);

        await act.Should().ThrowAsync<HttpRequestException>();
        handler.Attempts["/api/Order/place"].Should().Be(1,
            "a lost ack after a booked order must never be retried into a second live order (gh#629)");
    }

    [Fact]
    public async Task SearchPost_OnServerError_IsRetried()
    {
        var (factory, handler) = BuildPipeline();
        var client = factory.CreateClient("gw");

        await client.PostAsync("/api/Order/search", content: null);

        // 1 initial + 3 retries — proves the pipeline genuinely retries, so the placement assertions above are
        // not passing merely because retries were globally off.
        handler.Attempts["/api/Order/search"].Should().Be(4);
    }
}
