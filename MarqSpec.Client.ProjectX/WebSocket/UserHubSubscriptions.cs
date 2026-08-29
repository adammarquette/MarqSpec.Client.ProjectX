namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Recorded user-hub subscribe intent this client will try to restore after
/// connect or automatic reconnect (R-5.3, gh#87). Not live server state.
/// </summary>
public sealed class UserHubSubscriptions
{
    /// <summary>
    /// Whether <see cref="IProjectXWebSocketClient.SubscribeToAccountUpdatesAsync"/> is recorded.
    /// </summary>
    public bool Accounts { get; init; }

    /// <summary>
    /// Account ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToOrderUpdatesAsync"/>.
    /// </summary>
    public IReadOnlySet<int> OrderAccountIds { get; init; } = new HashSet<int>();

    /// <summary>
    /// Account ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToPositionUpdatesAsync"/>.
    /// </summary>
    public IReadOnlySet<int> PositionAccountIds { get; init; } = new HashSet<int>();

    /// <summary>
    /// Account ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToTradeNotificationsAsync"/>.
    /// </summary>
    public IReadOnlySet<int> TradeAccountIds { get; init; } = new HashSet<int>();
}
