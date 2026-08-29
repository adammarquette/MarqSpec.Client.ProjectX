namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Market-hub subscriptions this client will restore after an automatic reconnect (R-5.3, gh#87).
/// </summary>
public sealed class MarketHubSubscriptions
{
    /// <summary>
    /// Contract ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToPriceUpdatesAsync"/>.
    /// </summary>
    public IReadOnlySet<string> PriceContractIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Contract ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToOrderBookUpdatesAsync"/>.
    /// </summary>
    public IReadOnlySet<string> OrderBookContractIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Contract ids subscribed via <see cref="IProjectXWebSocketClient.SubscribeToTradeUpdatesAsync"/>.
    /// </summary>
    public IReadOnlySet<string> TradeContractIds { get; init; } = new HashSet<string>();
}
