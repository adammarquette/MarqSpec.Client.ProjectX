using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.WebSocket;
using Refit;
using System.Net;

namespace MarqSpec.Client.ProjectX.DependencyInjection;

/// <summary>
/// Extension methods for setting up ProjectX API client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
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

            // Override with environment variables if present
            var apiKey = Environment.GetEnvironmentVariable("PROJECTX_API_KEY");
            var apiSecret = Environment.GetEnvironmentVariable("PROJECTX_API_SECRET");

            if (!string.IsNullOrEmpty(apiKey))
            {
                options.ApiKey = apiKey;
            }

            if (!string.IsNullOrEmpty(apiSecret))
            {
                options.ApiSecret = apiSecret;
            }
        });

        // Configure WebSocket options
        services.Configure<WebSocketOptions>(options =>
        {
            configuration.GetSection("ProjectX:WebSocket").Bind(options);
        });

        // Register authentication service
        services.AddHttpClient<IAuthenticationService, AuthenticationService>();

        // Register Refit client with retry policy
        services.AddRefitClient<IProjectXRestApi>()
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
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.MaxDelay = TimeSpan.FromSeconds(30);
                options.Retry.ShouldHandle = args => new ValueTask<bool>(
                    args.Outcome.Exception is HttpRequestException ||
                    (args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests) ||
                    (args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError));
            });

        // Register API clients
        services.AddScoped<IProjectXApiClient, ProjectXApiClient>();
        services.AddSingleton<IProjectXWebSocketClient, ProjectXWebSocketClient>();

        return services;
    }

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
