namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// User-hub subscriptions this client will restore after an automatic reconnect (R-5.3, gh#87).
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
