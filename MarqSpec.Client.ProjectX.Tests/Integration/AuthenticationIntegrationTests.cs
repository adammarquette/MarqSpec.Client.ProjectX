using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;

namespace MarqSpec.Client.ProjectX.Tests.Integration;

/// <summary>
/// Integration tests for authentication functionality.
/// </summary>
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var token = await authService.GetAccessTokenAsync();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex(@"^[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+$",
            "token should be a valid JWT format");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetAccessTokenAsync_CalledMultipleTimes_CachesToken()
    {
        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var token1 = await authService.GetAccessTokenAsync();
        var token2 = await authService.GetAccessTokenAsync();
        var token3 = await authService.GetAccessTokenAsync();

        // Assert
        token1.Should().Be(token2);
        token2.Should().Be(token3);
        token1.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task RefreshTokenAsync_UpdatesToken()
    {
        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();
        var initialToken = await authService.GetAccessTokenAsync();

        // Act
        await authService.RefreshTokenAsync();
        var refreshedToken = await authService.GetAccessTokenAsync();

        // Assert
        initialToken.Should().NotBeNullOrEmpty();
        refreshedToken.Should().NotBeNullOrEmpty();
        // Token may or may not change depending on server implementation
        // Just verify we can get a token after refresh
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task ConcurrentAuthentication_HandlesMultipleRequests()
    {
        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act - Multiple concurrent auth requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => authService.GetAccessTokenAsync())
            .ToArray();

        var tokens = await Task.WhenAll(tasks);

        // Assert
        tokens.Should().AllSatisfy(token => token.Should().NotBeNullOrEmpty());
        tokens.Distinct().Should().HaveCount(1, "all concurrent requests should get the same cached token");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetAccessTokenAsync_WithInvalidCredentials_ThrowsAuthenticationException()
    {
        // Note: This test requires manually setting invalid credentials
        // PROJECTX_API_KEY=invalid_key
        // PROJECTX_API_SECRET=invalid_secret
        
        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var act = async () => await authService.GetAccessTokenAsync();

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Authentication failed*");
    }
}
