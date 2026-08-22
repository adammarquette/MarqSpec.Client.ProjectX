using Microsoft.Extensions.Configuration;

namespace MarqSpec.Client.ProjectX.IntegrationTests.Infrastructure;

/// <summary>
/// A fact that runs only when real gateway credentials are present, and says so when they are not.
/// </summary>
/// <remarks>
/// This replaces the previous pattern, where 22 of 43 integration facts carried a hardcoded
/// <c>Skip = "Manual execution only - requires valid API credentials"</c> that ignored the
/// credential-detection logic sitting right beside it. Those tests were skipped whether or not credentials
/// existed, which is coverage-shaped rather than coverage (R-11.5).
/// <para>
/// The condition here reads the same sources the client binds from, so it can genuinely become false — set the
/// credentials and the test runs.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveGatewayFactAttribute : FactAttribute
{
    public LiveGatewayFactAttribute()
    {
        if (LiveGatewayCredentials.Available)
        {
            return;
        }

        Skip = "No live gateway credentials. Set ProjectX__ApiKey and ProjectX__ApiSecret (or use "
            + "`dotnet user-secrets`) to run the live tier. The fake-gateway tier covers this path without them.";
    }
}

/// <summary>Resolves live-gateway credentials from the environment or user secrets.</summary>
public static class LiveGatewayCredentials
{
    private static readonly Lazy<IConfiguration> _configuration = new(() =>
        new ConfigurationBuilder()
            .AddUserSecrets(typeof(LiveGatewayCredentials).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build());

    /// <summary>The API key, or null.</summary>
    public static string? ApiKey =>
        FirstNonEmpty(
            Environment.GetEnvironmentVariable("PROJECTX_API_KEY"),
            Environment.GetEnvironmentVariable("ProjectX__ApiKey"),
            _configuration.Value["ProjectX:ApiKey"]);

    /// <summary>The API secret, or null.</summary>
    public static string? ApiSecret =>
        FirstNonEmpty(
            Environment.GetEnvironmentVariable("PROJECTX_API_SECRET"),
            Environment.GetEnvironmentVariable("ProjectX__ApiSecret"),
            _configuration.Value["ProjectX:ApiSecret"]);

    /// <summary>Whether both are present.</summary>
    public static bool Available => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiSecret);

    private static string? FirstNonEmpty(params string?[] values) =>
        Array.Find(values, value => !string.IsNullOrWhiteSpace(value));
}
