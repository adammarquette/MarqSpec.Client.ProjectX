using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.Exceptions;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Authentication;

/// <summary>
/// Provides authentication services for the ProjectX API using JWT tokens.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ProjectXOptions _options;
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private DateTime _tokenExpiration = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The ProjectX configuration options.</param>
    /// <param name="httpClient">The HTTP client for authentication requests.</param>
    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IOptions<ProjectXOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        
        _options.Validate();
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check if token is still valid (with 1 minute buffer)
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            await RefreshTokenInternalAsync(cancellationToken);
            return _accessToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task RefreshTokenInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Requesting new access token");

            var request = new AuthenticationRequest
            {
                Username = _options.ApiKey,
                ApiKey = _options.ApiSecret
            };

            var response = await _httpClient.PostAsJsonAsync("/api/Auth/loginKey", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed with status code {StatusCode}. Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new AuthenticationException($"Authentication failed with status code {response.StatusCode}. Please verify your API credentials.");
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);

            if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
            {
                var errorMsg = authResponse?.ErrorMessage ?? "Unknown error";
                throw new AuthenticationException($"Authentication failed: {errorMsg}");
            }

            _accessToken = authResponse.Token;
            // JWT tokens typically expire in 1 hour, set expiration to 55 minutes to be safe
            _tokenExpiration = DateTime.UtcNow.AddMinutes(55);

            _logger.LogInformation("Successfully authenticated and obtained access token");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred during authentication");
            throw new AuthenticationException("Network error occurred during authentication. Please check your connection.", ex);
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during authentication");
            throw new AuthenticationException("Unexpected error occurred during authentication.", ex);
        }
    }

    private class AuthenticationRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("apikey")]
        public string ApiKey { get; set; } = string.Empty;
    }

    private class AuthenticationResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}
