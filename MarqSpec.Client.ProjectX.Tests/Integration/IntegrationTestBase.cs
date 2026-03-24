using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.DependencyInjection;

namespace MarqSpec.Client.ProjectX.Tests.Integration;

/// <summary>
/// Base class for integration tests that require actual API connectivity.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected IProjectXApiClient Client { get; }
    protected ProjectXOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether integration tests should be skipped.
    /// Checks both environment variables and appsettings.integration.json for credentials.
    /// </summary>
    public static string? SkipReason
    {
        get
        {
            // Check environment variables first
            var envApiKey = Environment.GetEnvironmentVariable("PROJECTX_API_KEY");
            var envApiSecret = Environment.GetEnvironmentVariable("PROJECTX_API_SECRET");

            if (!string.IsNullOrEmpty(envApiKey) && !string.IsNullOrEmpty(envApiSecret))
            {
                return null; // Credentials found in environment variables
            }

            // Check appsettings.integration.json
            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.integration.json"), optional: true)
                    .Build();

                var apiKey = configuration["ProjectX:ApiKey"];
                var apiSecret = configuration["ProjectX:ApiSecret"];

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
                {
                    return null; // Credentials found in appsettings
                }
            }
            catch
            {
                // If we can't read appsettings, continue to error message
            }

            return "Integration tests require PROJECTX_API_KEY and PROJECTX_API_SECRET to be set in either:\n" +
                   "  1. Environment variables, or\n" +
                   "  2. appsettings.integration.json (ProjectX:ApiKey and ProjectX:ApiSecret)";
        }
    }

    protected IntegrationTestBase()
    {
        // Build configuration - environment variables override appsettings
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.integration.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI container
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add ProjectX API client
        services.AddProjectXApiClient(configuration);

        ServiceProvider = services.BuildServiceProvider();
        Client = ServiceProvider.GetRequiredService<IProjectXApiClient>();

        var optionsSnapshot = ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProjectXOptions>>();
        Options = optionsSnapshot.Value;
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
