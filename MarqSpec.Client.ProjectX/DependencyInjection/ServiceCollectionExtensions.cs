using System.Net;
using System.Text.Json;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Refit;

namespace MarqSpec.Client.ProjectX.DependencyInjection;

/// <summary>
/// Extension methods for setting up ProjectX API client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Serializer settings for the gateway.
    /// </summary>
    /// <remarks>
    /// Refit's default writes enums as camelCase <b>strings</b> — an aggregate-bar unit went out as
    /// <c>"unit":"minute"</c>. The gateway's schema types every enum as an <b>integer</b>
    /// (<c>2 = Minute</c>) and rejects a string outright, so <c>retrieveBars</c> failed with
    /// <c>400 "The JSON value could not be converted to AggregateBarUnit"</c> for every request.
    /// <para>
    /// <see cref="JsonSerializerDefaults.Web"/> without a string-enum converter writes and reads enums
    /// numerically, which matches the schema in both directions — verified against the gateway's swagger:
    /// every enum it defines is integer-typed, and none is a string.
    /// </para>
    /// </remarks>
    private static readonly RefitSettings _gatewaySettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)),
    };

    /// <summary>
    /// Adds ProjectX API client services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration containing ProjectX settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProjectXApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure REST API options
        services.Configure<ProjectXOptions>(options =>
        {
            configuration.GetSection(ProjectXOptions.SectionName).Bind(options);

            options.ApiKey = ResolveCredential("PROJECTX_API_KEY", "ProjectX__ApiKey", options.ApiKey);
            options.ApiSecret = ResolveCredential("PROJECTX_API_SECRET", "ProjectX__ApiSecret", options.ApiSecret);
        });

        // Configure WebSocket options.
        //
        // The hub URLs have two documented spellings: ProjectX:WebSocketUserHubUrl (which the library README's
        // configuration table lists) and ProjectX:WebSocket:UserHubUrl (which is what the client actually read).
        // Only the second worked, so a consumer following the README to repoint at a simulation venue edited a
        // dead key, saw no error, and stayed on the production default -- configured-looking and not configured
        // (gh#69). Both now resolve. The nested, more specific form wins when both are set, so no existing
        // deployment changes behaviour.
        services.Configure<WebSocketOptions>(options =>
        {
            var outer = configuration.GetSection(ProjectXOptions.SectionName);

            var userHubUrl = outer["WebSocketUserHubUrl"];
            if (!string.IsNullOrWhiteSpace(userHubUrl))
            {
                options.UserHubUrl = userHubUrl;
            }

            var marketHubUrl = outer["WebSocketMarketHubUrl"];
            if (!string.IsNullOrWhiteSpace(marketHubUrl))
            {
                options.MarketHubUrl = marketHubUrl;
            }

            configuration.GetSection("ProjectX:WebSocket").Bind(options);
        });

        // Register the authentication service as a SINGLETON.
        //
        // The lifetime is the token cache. AuthenticationService holds _accessToken / _tokenExpiration in
        // instance fields behind an instance SemaphoreSlim, so a transient registration -- which is what
        // AddHttpClient<TClient, TImplementation> gives you -- hands every consumer an empty cache and its own
        // lock. ProjectXApiClient and the Refit pipeline's AuthenticationHandler then log in separately, on
        // every scope, against a rate-limited endpoint; the double-check inside GetAccessTokenAsync dedupes
        // nothing because concurrent callers wait on different semaphores; and LogoutAsync clears a cache the
        // transport never reads (gh#55).
        //
        // The typed client is registered on the CONCRETE type so the constructor keeps taking an HttpClient --
        // it is public surface, and the unit suite builds the service directly. The interface is then bound to
        // that one instance.
        services.AddHttpClient<AuthenticationService>()
            // A singleton holding an HttpClient is captive: its handler chain is fixed for the life of the
            // container, so handler rotation cannot refresh DNS for it. PooledConnectionLifetime is the
            // documented remedy -- connections retire on a timer and the next one re-resolves. Without it a
            // long-running host pins the gateway's IP until restart.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            })
            // Rotation is pointless for a captive client -- the captured HttpClient keeps the chain it was
            // built with either way -- and letting the factory expire handlers only leaves dead entries behind.
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        services.AddSingleton<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());

        // Register Refit client with retry policy
        services.AddRefitGeneratedClient<IProjectXRestApi>(_gatewaySettings)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ProjectXOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler(sp =>
            {
                return new AuthenticationHandler(sp.GetRequiredService<IAuthenticationService>());
            })
            .AddStandardResilienceHandler(options =>
            {
                // From configuration, not hardcoded. ProjectX:RetryOptions was bound and documented from the
                // beginning while the pipeline used literals, so tuning it did nothing (gh#69). The defaults on
                // RetryOptions are the literals that used to be here, so an unconfigured consumer is unaffected.
                var retry = ReadRetryOptions(configuration);

                options.Retry.MaxRetryAttempts = retry.MaxRetries;
                options.Retry.Delay = retry.InitialDelay;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.MaxDelay = retry.MaxDelay;
                options.Retry.ShouldHandle = args => new ValueTask<bool>(
                    ShouldRetryOutcome(
                        args.Context.GetRequestMessage(),
                        args.Outcome.Exception,
                        args.Outcome.Result));
                options.Retry.DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests } response
                        && response.Headers.RetryAfter is { } retryAfter)
                    {
                        TimeSpan? delay = retryAfter.Delta
                            ?? (retryAfter.Date.HasValue
                                ? retryAfter.Date.Value - DateTimeOffset.UtcNow
                                : null);

                        if (delay.HasValue && delay.Value > TimeSpan.Zero)
                        {
                            return new ValueTask<TimeSpan?>(delay);
                        }
                    }

                    // Fall back to the default exponential backoff
                    return new ValueTask<TimeSpan?>((TimeSpan?)null);
                };
            });

        // Register API clients
        services.AddScoped<IProjectXApiClient, ProjectXApiClient>();
        services.AddSingleton<IProjectXWebSocketClient, ProjectXWebSocketClient>();

        return services;
    }

    /// <summary>
    /// Reads <c>ProjectX:RetryOptions</c> for the resilience pipeline.
    /// </summary>
    /// <remarks>
    /// Read directly rather than through <see cref="IOptions{TOptions}"/> because
    /// <see cref="Microsoft.Extensions.Http.Resilience"/> wants its values while the pipeline is being
    /// registered, before a provider exists to resolve options from. Unset keys fall through to
    /// <see cref="RetryOptions"/>'s own defaults, which are the literals this used to hardcode.
    /// </remarks>
    private static RetryOptions ReadRetryOptions(IConfiguration configuration)
    {
        var options = new RetryOptions();
        configuration.GetSection($"{ProjectXOptions.SectionName}:{nameof(ProjectXOptions.RetryOptions)}").Bind(options);
        return options;
    }

    /// <summary>
    /// Resolves a credential from the environment, falling back to whatever configuration bound.
    /// </summary>
    /// <param name="legacyName">The flat form, e.g. <c>PROJECTX_API_KEY</c>.</param>
    /// <param name="sectionScopedName">The ASP.NET double-underscore form, e.g. <c>ProjectX__ApiKey</c>.</param>
    /// <param name="boundValue">The value <see cref="IConfiguration"/> already bound, used when neither is set.</param>
    /// <remarks>
    /// Both conventions are read directly rather than relying on the configuration pipeline, because
    /// <see cref="AddProjectXApiClient"/> accepts an arbitrary <see cref="IConfiguration"/> — one built without
    /// an environment-variable provider never sees either form. That was a live gap: <c>release.yml</c> sets
    /// <c>ProjectX__ApiKey</c> / <c>ProjectX__ApiSecret</c>, and only the flat names were read, so the
    /// workflow's credentials never reached the options (R-1.2).
    /// <para>
    /// The flat form wins when both are set, so adding the second convention cannot change what an existing
    /// deployment resolves to.
    /// </para>
    /// </remarks>
    private static string ResolveCredential(string legacyName, string sectionScopedName, string boundValue)
    {
        var legacy = Environment.GetEnvironmentVariable(legacyName);
        if (!string.IsNullOrEmpty(legacy))
        {
            return legacy;
        }

        var sectionScoped = Environment.GetEnvironmentVariable(sectionScopedName);
        return string.IsNullOrEmpty(sectionScoped) ? boundValue : sectionScoped;
    }

    /// <summary>
    /// The gateway pipeline's retry predicate, factored out of the registration lambda so it can be unit-tested
    /// against the real request and outcome shapes.
    /// </summary>
    /// <remarks>
    /// Placing an order is <b>non-idempotent</b>: if the first attempt reached the gateway and booked the order
    /// but the acknowledgement was lost to a transport fault, or a <c>5xx</c> came back <i>after</i> the order
    /// was accepted, a retry would place a <b>second live order</b>. So a placement is <b>never</b> retried
    /// (gh#629) — its fault surfaces to the caller, which classifies it as an <i>indeterminate</i> venue outcome
    /// and leaves the durable intent (<c>Taking</c> / <c>Firing</c>) for reconciliation rather than releasing a
    /// maybe-live order. Every other route here is a read/search or an idempotent cancel, so the standard
    /// transient-fault retry (transport fault, <c>429</c>, <c>5xx</c>) still applies. If the request cannot be
    /// identified — a defensive null — it is treated as "not a placement" and the standard retry still covers
    /// it, so this guard is never <i>less</i> resilient than the pipeline was before it.
    /// </remarks>
    internal static bool ShouldRetryOutcome(
        HttpRequestMessage? request,
        Exception? exception,
        HttpResponseMessage? response)
    {
        if (IsOrderPlacement(request))
        {
            return false;
        }

        return exception is HttpRequestException
            || response?.StatusCode == HttpStatusCode.TooManyRequests
            || response?.StatusCode >= HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Whether <paramref name="request"/> is the non-idempotent order-placement call
    /// (<c>POST /api/Order/place</c>) that must never be auto-retried (gh#629). A null or route-less request is
    /// treated as "not a placement", so this guard only ever <i>removes</i> retries from a confirmed placement
    /// and never adds double-place risk beyond the pipeline's prior behaviour.
    /// </summary>
    internal static bool IsOrderPlacement(HttpRequestMessage? request) =>
        request?.RequestUri is { } uri
        && uri.AbsolutePath.EndsWith("/Order/place", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// HTTP message handler that adds authentication to requests.
    /// </summary>
    private class AuthenticationHandler : DelegatingHandler
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationHandler(IAuthenticationService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _authService.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
