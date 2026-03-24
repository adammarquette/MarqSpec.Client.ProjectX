namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Represents a change in WebSocket connection status.
/// </summary>
public class ConnectionStatusChange
{
    /// <summary>
    /// Gets or sets the previous connection state.
    /// </summary>
    public ConnectionState PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the current connection state.
    /// </summary>
    public ConnectionState CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the state change.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the error message if the state change was due to an error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the state change, if any.
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// Represents the connection state of a WebSocket.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The WebSocket is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The WebSocket is connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// The WebSocket is connected.
    /// </summary>
    Connected,

    /// <summary>
    /// The WebSocket is reconnecting after a disconnection.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The WebSocket connection has failed.
    /// </summary>
    Failed
}
