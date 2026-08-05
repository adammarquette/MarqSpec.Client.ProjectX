using System.Net;
using System.Text.Json;
using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    public async Task GetAccessTokenAsync_WhenServerReturnsErrorCodeWithoutMessage_SurfacesErrorCode()
    {
        // Arrange: server returns HTTP 200 but success=false with a populated
        // errorCode and a null errorMessage (the documented ProjectX behaviour).
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, new
        {
            token = (string?)null,
            success = false,
            errorCode = 3, // LoginErrorCode.InvalidCredentials
            errorMessage = (string?)null
        });
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var act = async () => await service.GetAccessTokenAsync();

        // Assert: the real reason must be surfaced, not collapsed to "Unknown error".
        var assertion = await act.Should().ThrowAsync<AuthenticationException>();
        assertion.Which.Message.Should().Contain("InvalidCredentials");
        assertion.Which.Message.Should().NotContain("Unknown error");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithEmptyResponseBody_ThrowsWithEmptyResponseMessage()
    {
        // Arrange: HTTP 200 with a genuinely empty body. Deserialize() throws JsonException on "" before
        // it can ever return null, so this has to be diagnosed before deserialization is attempted -- a
        // regression here previously fell through to "could not be parsed" instead.
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, responseBody: string.Empty);
        var httpClient = new HttpClient(mockHandler);
        var service = new AuthenticationService(_logger, Options.Create(_options), httpClient);

        // Act
        var act = async () => await service.GetAccessTokenAsync();

        // Assert
        var assertion = await act.Should().ThrowAsync<AuthenticationException>();
        assertion.Which.Message.Should().Contain("empty response");
        assertion.Which.Message.Should().NotContain("could not be parsed");
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
        private readonly string? _rawResponseBody;
        private readonly Exception? _exception;
        private readonly Action? _onSend;

        public MockHttpMessageHandler(HttpStatusCode statusCode, object responseContent, Action? onSend = null)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
            _onSend = onSend;
        }

        /// <summary>Serves <paramref name="responseBody"/> verbatim (e.g. empty) rather than JSON-serializing it.</summary>
        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseBody, Action? onSend = null)
        {
            _statusCode = statusCode;
            _rawResponseBody = responseBody;
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
            if (_rawResponseBody != null)
            {
                response.Content = new StringContent(_rawResponseBody);
            }
            else if (_responseContent != null)
            {
                response.Content = new StringContent(JsonSerializer.Serialize(_responseContent));
            }
            return Task.FromResult(response);
        }
    }
}
