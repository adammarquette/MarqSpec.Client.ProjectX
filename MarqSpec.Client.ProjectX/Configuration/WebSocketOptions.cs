namespace MarqSpec.Client.ProjectX.Configuration;

/// <summary>
/// Configuration options for the ProjectX WebSocket client.
/// </summary>
public class WebSocketOptions
{
    /// <summary>
    /// Gets or sets the market hub URL for real-time market data.
    /// </summary>
    /// <value>Default: https://rtc.topstepx.com/hubs/market</value>
    public string MarketHubUrl { get; set; } = "https://rtc.topstepx.com/hubs/market";

    /// <summary>
    /// Gets or sets the user hub URL for real-time order updates.
    /// </summary>
    /// <value>Default: https://rtc.topstepx.com/hubs/user</value>
    public string UserHubUrl { get; set; } = "https://rtc.topstepx.com/hubs/user";

    /// <summary>
    /// Gets or sets the automatic reconnection enabled flag.
    /// </summary>
    /// <value>Default: true</value>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum reconnection delay in seconds.
    /// </summary>
    /// <value>Default: 5 seconds (per PRD requirement)</value>
    public int MaxReconnectDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the initial reconnection delay in seconds.
    /// </summary>
    /// <value>Default: 1 second</value>
    public int InitialReconnectDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets the handshake timeout in seconds.
    /// </summary>
    /// <value>Default: 15 seconds</value>
    public int HandshakeTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the keep-alive interval in seconds.
    /// </summary>
    /// <value>Default: 15 seconds</value>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the server timeout in seconds.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public int ServerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use message pack protocol instead of JSON.
    /// </summary>
    /// <value>Default: false (uses JSON per PRD recommendation)</value>
    public bool UseMessagePack { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum buffer size for incoming messages in bytes.
    /// </summary>
    /// <value>Default: 1 MB</value>
    public long MaxBufferSize { get; set; } = 1024 * 1024;
}
