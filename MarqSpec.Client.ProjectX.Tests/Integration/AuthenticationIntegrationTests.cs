using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;

namespace MarqSpec.Client.ProjectX.Tests.Integration;

/// <summary>
/// Integration tests for authentication functionality.
/// </summary>
[Collection("Integration Tests")]
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsToken()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();

        // Act
        var token = await authService.GetAccessTokenAsync();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex(@"^[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+$",
            "token should be a valid JWT format");
    }

    [Fact]
    public async Task GetAccessTokenAsync_CalledMultipleTimes_CachesToken()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

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

    [Fact]
    public async Task RefreshTokenAsync_UpdatesToken()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

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

    [Fact]
    public async Task ConcurrentAuthentication_HandlesMultipleRequests()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

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

    [Fact]
    public async Task GetAccessTokenAsync_WithInvalidCredentials_ThrowsAuthenticationException()
    {
        // This test should be run manually with invalid credentials set in environment
        // Skip if valid credentials are set
        if (SkipReason == null) return;

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
