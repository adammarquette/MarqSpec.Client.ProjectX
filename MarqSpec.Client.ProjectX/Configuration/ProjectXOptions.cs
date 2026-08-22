namespace MarqSpec.Client.ProjectX.Configuration;

/// <summary>
/// Configuration options for the ProjectX API client.
/// </summary>
public class ProjectXOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "ProjectX";

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API secret for authentication.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the REST API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.topstepx.com";

    /// <summary>
    /// Gets or sets the WebSocket user hub URL.
    /// </summary>
    public string WebSocketUserHubUrl { get; set; } = "https://rtc.topstepx.com/hubs/user";

    /// <summary>
    /// Gets or sets the WebSocket market hub URL.
    /// </summary>
    public string WebSocketMarketHubUrl { get; set; } = "https://rtc.topstepx.com/hubs/market";

    /// <summary>
    /// Gets or sets a value indicating whether SSL certificates should be validated.
    /// </summary>
    /// <remarks>
    /// <b>Has no effect. TLS certificate validation is always on and cannot be turned off.</b>
    /// <para>
    /// This was bound and documented but never read. It was left unwired deliberately rather than
    /// implemented: honouring <see langword="false"/> would add a supported way to disable certificate
    /// validation against a live trading venue, which is a hole this client does not currently have and has no
    /// reason to open. If a self-signed endpoint must be reached, configure trust at the host rather than
    /// asking the client to stop checking (gh#69).
    /// </para>
    /// </remarks>
    [Obsolete("Has no effect: TLS certificate validation is always on. Wiring this would add a way to disable " +
              "certificate validation against a live venue, which is deliberately not offered (gh#69).")]
    public bool ValidateSslCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the retry options.
    /// </summary>
    public RetryOptions RetryOptions { get; set; } = new();

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("API key is required. Configure it via the ProjectX__ApiKey environment variable, the legacy PROJECTX_API_KEY, user secrets, or the ProjectX configuration section.");
        }

        if (string.IsNullOrWhiteSpace(ApiSecret))
        {
            throw new InvalidOperationException("API secret is required. Configure it via the ProjectX__ApiSecret environment variable, the legacy PROJECTX_API_SECRET, user secrets, or the ProjectX configuration section.");
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Base URL is required.");
        }
    }
}
