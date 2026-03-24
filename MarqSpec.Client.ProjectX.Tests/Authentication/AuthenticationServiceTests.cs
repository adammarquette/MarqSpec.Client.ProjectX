using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.Exceptions;
using System.Net;
using System.Text.Json;

namespace MarqSpec.Client.ProjectX.Tests.Authentication;

public class AuthenticationServiceTests
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ProjectXOptions _options;

    public AuthenticationServiceTests()
    {
        _logger = A.Fake<ILogger<AuthenticationService>>();
        _options = new ProjectXOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUrl = "https://api.test.com"
        };
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            token = "test-token",
            success = true,
            errorCode = 0,
            errorMessage = (string?)null
        });
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var token = await service.GetAccessTokenAsync();

        // Assert
        token.Should().Be("test-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenIsCached_ReturnsCachedToken()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            token = "test-token",
            success = true,
            errorCode = 0,
            errorMessage = (string?)null
        }, () => callCount++);
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var token1 = await service.GetAccessTokenAsync();
        var token2 = await service.GetAccessTokenAsync();

        // Assert
        token1.Should().Be(token2);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithInvalidCredentials_ThrowsAuthenticationException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.Unauthorized, new { message = "Invalid credentials" });
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var act = async () => await service.GetAccessTokenAsync();

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Authentication failed*");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithNetworkError_ThrowsAuthenticationException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var act = async () => await service.GetAccessTokenAsync();

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Network error*");
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new ProjectXOptions { ApiKey = "", ApiSecret = "" };
        var httpClient = new HttpClient();

        // Act
        var act = () => new AuthenticationService(_logger, Options.Create(invalidOptions), httpClient);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is required*");
    }

    [Fact]
    public async Task RefreshTokenAsync_UpdatesToken()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            token = "new-token",
            success = true,
            errorCode = 0,
            errorMessage = (string?)null
        });
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        await service.RefreshTokenAsync();
        var token = await service.GetAccessTokenAsync();

        // Assert
        token.Should().Be("new-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ThreadSafe_HandlesMultipleConcurrentRequests()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            token = "test-token",
            success = true,
            errorCode = 0,
            errorMessage = (string?)null
        }, () => Interlocked.Increment(ref callCount));
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => service.GetAccessTokenAsync()).ToArray();
        var tokens = await Task.WhenAll(tasks);

        // Assert
        tokens.Should().AllBe("test-token");
        callCount.Should().Be(1, "token should only be fetched once despite concurrent requests");
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object? _responseContent;
        private readonly Exception? _exception;
        private readonly Action? _onSend;

        public MockHttpMessageHandler(HttpStatusCode statusCode, object responseContent, Action? onSend = null)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
            _onSend = onSend;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onSend?.Invoke();

            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode);
            if (_responseContent != null)
            {
                response.Content = new StringContent(JsonSerializer.Serialize(_responseContent));
            }
            return Task.FromResult(response);
        }
    }
}
