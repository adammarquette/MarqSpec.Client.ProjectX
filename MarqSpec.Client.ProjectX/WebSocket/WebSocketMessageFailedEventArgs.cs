namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Event arguments for a failed WebSocket message send operation.
/// </summary>
public class WebSocketMessageFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the hub method name that was being invoked (e.g. "SubscribeToPrices").
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets the arguments that were passed to the hub method.
    /// </summary>
    public object?[] Arguments { get; init; } = [];

    /// <summary>
    /// Gets the hub that the message was targeted at ("Market" or "User").
    /// </summary>
    public required string HubName { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the failure occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
