using FluentAssertions;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.DependencyInjection;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarqSpec.Client.ProjectX.Tests.DependencyInjection;

/// <summary>
/// Lifetime contracts for <see cref="ServiceCollectionExtensions.AddProjectXApiClient"/>.
/// </summary>
/// <remarks>
/// These are not registration trivia. <see cref="AuthenticationService"/> caches the bearer token in instance
/// fields behind an instance <see cref="SemaphoreSlim"/>, so the lifetime <i>is</i> the cache: register it
/// transient and every consumer logs in separately, the semaphore dedupes nothing, and a logout clears a cache
/// the transport never reads (gh#55).
/// </remarks>
public class ServiceRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProjectX:ApiKey"] = "test-api-key",
                ["ProjectX:ApiSecret"] = "test-api-secret",
                ["ProjectX:BaseUrl"] = "https://api.test.invalid",
            })
            .Build();

        // Deliberately does NOT clear PROJECTX_* / ProjectX__* from the environment. These tests assert
        // lifetimes and reference identity, never a credential value, so a leaked variable cannot change the
        // outcome -- and EnvironmentCredentialBindingTests sets those same process-wide variables. xUnit runs
        // test classes in parallel, so clearing them here made that class fail intermittently.
        return new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
            .AddProjectXApiClient(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public void AddProjectXApiClient_ShouldRegisterAuthenticationServiceAsSingleton_WhenCalled()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddProjectXApiClient(new ConfigurationBuilder().Build());

        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(IAuthenticationService)).Subject;

        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void GetRequiredService_ShouldReturnTheSameAuthenticationService_WhenResolvedTwice()
    {
        using var provider = BuildProvider();

        var first = provider.GetRequiredService<IAuthenticationService>();
        var second = provider.GetRequiredService<IAuthenticationService>();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetRequiredService_ShouldReturnTheSameAuthenticationService_WhenResolvedFromDifferentScopes()
    {
        using var provider = BuildProvider();

        var fromRoot = provider.GetRequiredService<IAuthenticationService>();

        using var scopeOne = provider.CreateScope();
        using var scopeTwo = provider.CreateScope();

        var fromScopeOne = scopeOne.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var fromScopeTwo = scopeTwo.ServiceProvider.GetRequiredService<IAuthenticationService>();

        // One token cache for the whole container, or the cache is not a cache.
        fromScopeOne.Should().BeSameAs(fromRoot);
        fromScopeTwo.Should().BeSameAs(fromRoot);
    }

    // await using: the hub client is IAsyncDisposable-only, and ServiceProvider.Dispose() throws rather than
    // dispose one synchronously.
    [Fact]
    public async Task ProjectXApiClientAndWebSocketClient_ShouldShareOneAuthenticationService_WhenBothResolved()
    {
        await using var provider = BuildProvider();

        // The singleton hub client captures whatever it is given for the life of the container; if that is a
        // different instance from the one the REST path uses, the two hold divergent tokens.
        provider.GetRequiredService<IProjectXWebSocketClient>();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IProjectXApiClient>();

        var resolvedInScope = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        resolvedInScope.Should().BeSameAs(provider.GetRequiredService<IAuthenticationService>());
    }
}
