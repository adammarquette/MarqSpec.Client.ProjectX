namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Recorded market-hub subscribe intent this client will try to restore after
/// connect or automatic reconnect (R-5.3, gh#87). Not live server state.
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
