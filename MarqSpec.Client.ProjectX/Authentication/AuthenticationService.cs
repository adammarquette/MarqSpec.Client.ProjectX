using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Authentication failed with status code {StatusCode}. Body: {Body}",
                    response.StatusCode, responseBody);
                throw new AuthenticationException($"Authentication failed with status code {(int)response.StatusCode} ({response.StatusCode}). Please verify your API credentials.");
            }

            if (string.IsNullOrEmpty(responseBody))
            {
                // Deserialize() throws JsonException on an empty string before it ever gets a chance to
                // return null, so this case has to be caught here rather than via the authResponse == null
                // check below -- that check alone never actually fires for a genuinely empty body.
                _logger.LogError("Authentication response was empty.");
                throw new AuthenticationException("Authentication failed: the server returned an empty response.");
            }

            AuthenticationResponse? authResponse;
            try
            {
                authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication response. Body: {Body}", responseBody);
                throw new AuthenticationException("Authentication failed: the server returned a response that could not be parsed.", ex);
            }

            if (authResponse == null)
            {
                // Reachable for a literal JSON `null` body -- valid JSON, distinct from the empty-body case
                // handled above, and JsonSerializer.Deserialize legitimately returns null for it.
                _logger.LogError("Authentication response deserialized to null. Body: {Body}", responseBody);
                throw new AuthenticationException("Authentication failed: the server returned an empty response.");
            }

            if (!authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
            {
                var reason = DescribeLoginFailure(authResponse);
                _logger.LogError("Authentication rejected by server: {Reason}", reason);
                throw new AuthenticationException($"Authentication failed: {reason}");
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

    /// <summary>
    /// Builds a <c>POST</c> carrying <paramref name="accessToken"/> as a bearer credential.
    /// </summary>
    /// <remarks>
    /// <c>/api/Auth/validate</c> and <c>/api/Auth/logout</c> are authenticated routes; only
    /// <c>/api/Auth/loginKey</c> is not. Through v1.0.5 both went out bare, because the
    /// <c>AuthenticationHandler</c> that attaches the header is registered on the Refit client and never on the
    /// <see cref="HttpClient"/> this service owns. The gateway answered <c>401</c>, so
    /// <see cref="ValidateSessionAsync"/> could never return <see langword="true"/> and
    /// <see cref="LogoutAsync"/> never terminated the server-side session while still reporting success (gh#56).
    /// <para>
    /// The header is set per-request rather than by adding that handler to this client, because
    /// <c>loginKey</c> travels on the same client — a handler would call
    /// <see cref="GetAccessTokenAsync"/> to authenticate the very request that acquires the token.
    /// </para>
    /// </remarks>
    private static HttpRequestMessage CreateAuthenticatedRequest(string route, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }

    /// <summary>
    /// Builds a human-readable description of a failed login response, surfacing the
    /// <c>errorCode</c> returned by the API (the <c>errorMessage</c> field is frequently null).
    /// </summary>
    private static string DescribeLoginFailure(AuthenticationResponse response)
    {
        var code = (LoginErrorCode)response.ErrorCode;
        var codeName = Enum.IsDefined(typeof(LoginErrorCode), code)
            ? code.ToString()
            : $"Unrecognized({response.ErrorCode})";

        var detail = code switch
        {
            LoginErrorCode.UserNotFound => "the user name was not found",
            LoginErrorCode.PasswordVerificationFailed => "password verification failed",
            LoginErrorCode.InvalidCredentials => "the API key or user name is invalid or has been revoked",
            LoginErrorCode.AgreementsNotSigned => "required account agreements have not been signed",
            LoginErrorCode.ApiSubscriptionNotFound => "no active API subscription was found for this account",
            LoginErrorCode.ApiKeyAuthenticationDisabled => "API key authentication is disabled for this account",
            LoginErrorCode.Success => "the server reported success but returned no token",
            _ => "the server rejected the credentials"
        };

        var message = $"{detail} (errorCode {response.ErrorCode}: {codeName})";
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            message += $" - {response.ErrorMessage}";
        }

        return message;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            return false;
        }

        try
        {
            _logger.LogDebug("Validating current session");

            using var request = CreateAuthenticatedRequest("/api/Auth/validate", _accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Session validation returned status {StatusCode}", response.StatusCode);
                return false;
            }

            var validateResponse = await response.Content.ReadFromJsonAsync<ValidateResponse>(cancellationToken);

            if (validateResponse == null)
            {
                return false;
            }

            if (validateResponse.Success && !string.IsNullOrEmpty(validateResponse.NewToken))
            {
                _accessToken = validateResponse.NewToken;
                _tokenExpiration = DateTime.UtcNow.AddMinutes(55);
                _logger.LogInformation("Session validated and token renewed");
            }
            else if (validateResponse.Success)
            {
                _logger.LogDebug("Session is valid");
            }
            else
            {
                _logger.LogWarning("Session is invalid. Error code: {ErrorCode}, Message: {ErrorMessage}",
                    validateResponse.ErrorCode, validateResponse.ErrorMessage);
                _accessToken = null;
                _tokenExpiration = DateTime.MinValue;
            }

            return validateResponse.Success;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred during session validation");
            throw new AuthenticationException("Network error occurred during session validation.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during session validation");
            throw new AuthenticationException("Unexpected error occurred during session validation.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Logging out current session");

            if (!string.IsNullOrEmpty(_accessToken))
            {
                using var request = CreateAuthenticatedRequest("/api/Auth/logout", _accessToken);
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var logoutResponse = await response.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken);
                    if (logoutResponse?.Success == true)
                    {
                        _logger.LogInformation("Successfully logged out");
                    }
                    else
                    {
                        _logger.LogWarning("Logout returned unsuccessful response: {ErrorMessage}", logoutResponse?.ErrorMessage);
                    }
                }
                else
                {
                    _logger.LogWarning("Logout request returned status {StatusCode}", response.StatusCode);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred during logout");
            throw new AuthenticationException("Network error occurred during logout.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during logout");
            throw new AuthenticationException("Unexpected error occurred during logout.", ex);
        }
        finally
        {
            _accessToken = null;
            _tokenExpiration = DateTime.MinValue;
        }
    }

    private class AuthenticationRequest
    {
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("apiKey")]
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

    private class ValidateResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("newToken")]
        public string? NewToken { get; set; }
    }

    private class LogoutResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}

/// <summary>
/// Error codes returned by the ProjectX <c>/api/Auth/loginKey</c> endpoint.
/// Mirrors the <c>LoginErrorCode</c> definition in the API's Swagger contract.
/// </summary>
public enum LoginErrorCode
{
    /// <summary>Authentication succeeded.</summary>
    Success = 0,

    /// <summary>The user name was not found.</summary>
    UserNotFound = 1,

    /// <summary>Password verification failed.</summary>
    PasswordVerificationFailed = 2,

    /// <summary>The supplied credentials are invalid or have been revoked.</summary>
    InvalidCredentials = 3,

    /// <summary>The application was not found.</summary>
    AppNotFound = 4,

    /// <summary>Application verification failed.</summary>
    AppVerificationFailed = 5,

    /// <summary>The device is not valid for this session.</summary>
    InvalidDevice = 6,

    /// <summary>Required account agreements have not been signed.</summary>
    AgreementsNotSigned = 7,

    /// <summary>An unknown error occurred.</summary>
    UnknownError = 8,

    /// <summary>No active API subscription was found for the account.</summary>
    ApiSubscriptionNotFound = 9,

    /// <summary>API key authentication is disabled for the account.</summary>
    ApiKeyAuthenticationDisabled = 10,
}
