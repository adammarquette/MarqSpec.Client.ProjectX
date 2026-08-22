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

    #region Bearer token on the session routes (gh#56)

    // /api/Auth/validate and /api/Auth/logout are authenticated routes -- FakeGateway's GatewayMiddleware
    // enforces the bearer on every /api path except /Auth/loginKey, and so does the real venue. The handler
    // that attaches Authorization is registered on the Refit client only, never on the HttpClient this service
    // owns, so the service has to set the header itself from the token it already holds.

    [Fact]
    public async Task ValidateSessionAsync_ShouldSendBearerToken_WhenATokenIsHeld()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler();
        handler.RespondTo("/api/Auth/loginKey", new { token = "test-token", success = true, errorCode = 0 });
        handler.RespondTo("/api/Auth/validate", new { success = true, errorCode = 0, newToken = (string?)null });
        var service = new AuthenticationService(_logger, Options.Create(_options), new HttpClient(handler));
        await service.GetAccessTokenAsync();

        // Act
        var valid = await service.ValidateSessionAsync();

        // Assert
        valid.Should().BeTrue();
        var validate = handler.RequestFor("/api/Auth/validate");
        validate.Headers.Authorization.Should().NotBeNull();
        validate.Headers.Authorization!.Scheme.Should().Be("Bearer");
        validate.Headers.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldSendTheRenewedToken_WhenTheServerIssuesOne()
    {
        // Arrange: a renewal must replace the cached token, or the next call authenticates with a dead one.
        var handler = new RecordingHttpMessageHandler();
        handler.RespondTo("/api/Auth/loginKey", new { token = "first-token", success = true, errorCode = 0 });
        handler.RespondTo("/api/Auth/validate", new { success = true, errorCode = 0, newToken = "renewed-token" });
        handler.RespondTo("/api/Auth/logout", new { success = true, errorCode = 0 });
        var service = new AuthenticationService(_logger, Options.Create(_options), new HttpClient(handler));
        await service.GetAccessTokenAsync();
        await service.ValidateSessionAsync();

        // Act
        await service.LogoutAsync();

        // Assert
        handler.RequestFor("/api/Auth/logout").Headers.Authorization!.Parameter.Should().Be("renewed-token");
    }

    [Fact]
    public async Task LogoutAsync_ShouldSendBearerToken_WhenATokenIsHeld()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler();
        handler.RespondTo("/api/Auth/loginKey", new { token = "test-token", success = true, errorCode = 0 });
        handler.RespondTo("/api/Auth/logout", new { success = true, errorCode = 0 });
        var service = new AuthenticationService(_logger, Options.Create(_options), new HttpClient(handler));
        await service.GetAccessTokenAsync();

        // Act
        await service.LogoutAsync();

        // Assert
        var logout = handler.RequestFor("/api/Auth/logout");
        logout.Headers.Authorization.Should().NotBeNull();
        logout.Headers.Authorization!.Scheme.Should().Be("Bearer");
        logout.Headers.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldNotSendBearerToken_WhenAcquiringTheToken()
    {
        // Arrange: loginKey is how a token is obtained, so it must not require one. Attaching the auth handler
        // to this client instead of setting the header per-route would make this request authenticate itself.
        var handler = new RecordingHttpMessageHandler();
        handler.RespondTo("/api/Auth/loginKey", new { token = "test-token", success = true, errorCode = 0 });
        var service = new AuthenticationService(_logger, Options.Create(_options), new HttpClient(handler));

        // Act
        await service.GetAccessTokenAsync();

        // Assert
        handler.RequestFor("/api/Auth/loginKey").Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldReturnFalseWithoutCallingTheGateway_WhenNoTokenIsHeld()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler();
        var service = new AuthenticationService(_logger, Options.Create(_options), new HttpClient(handler));

        // Act
        var valid = await service.ValidateSessionAsync();

        // Assert
        valid.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    #endregion

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

    /// <summary>
    /// Serves a canned JSON body per request path and keeps every outbound <see cref="HttpRequestMessage"/>, so
    /// a test can assert on headers the service set rather than only on what it returned.
    /// </summary>
    private class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, object> _responses = new(StringComparer.OrdinalIgnoreCase);

        public List<HttpRequestMessage> Requests { get; } = [];

        public void RespondTo(string path, object body) => _responses[path] = body;

        public HttpRequestMessage RequestFor(string path) =>
            Requests.SingleOrDefault(r => string.Equals(r.RequestUri?.AbsolutePath, path, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No request to '{path}'. Saw: {string.Join(", ", Requests.Select(r => r.RequestUri?.AbsolutePath))}");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (!_responses.TryGetValue(path, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(body)),
            });
        }
    }
}
