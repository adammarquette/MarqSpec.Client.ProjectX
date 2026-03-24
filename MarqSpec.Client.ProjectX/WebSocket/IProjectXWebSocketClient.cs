using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Interface for the ProjectX WebSocket client that provides real-time market data and order updates.
/// </summary>
/// <remarks>
/// This client manages WebSocket connections to the ProjectX real-time API, supporting both market data
/// (prices, order books, trades) and user data (order updates) streams. The client implements the Observer
/// pattern for easy subscription to real-time updates and includes automatic reconnection logic.
/// </remarks>
public interface IProjectXWebSocketClient : IAsyncDisposable
{
    #region Connection Management

    /// <summary>
    /// Gets the current connection state of the market hub.
    /// </summary>
    ConnectionState MarketHubState { get; }

    /// <summary>
    /// Gets the current connection state of the user hub.
    /// </summary>
    ConnectionState UserHubState { get; }

    /// <summary>
    /// Occurs when the connection status changes for either hub.
    /// </summary>
    event EventHandler<ConnectionStatusChange>? ConnectionStatusChanged;

    /// <summary>
    /// Connects to the market data hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectMarketHubAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the user data hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectUserHubAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the market data hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectMarketHubAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the user data hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectUserHubAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Market Data Subscriptions

    /// <summary>
    /// Subscribes to real-time price updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToPriceUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from real-time price updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to unsubscribe from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromPriceUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to real-time order book updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToOrderBookUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from real-time order book updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to unsubscribe from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromOrderBookUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to real-time trade updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToTradeUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from real-time trade updates for the specified contract.
    /// </summary>
    /// <param name="contractId">The contract identifier to unsubscribe from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromTradeUpdatesAsync(string contractId, CancellationToken cancellationToken = default);

    #endregion

    #region Market Data Events

    /// <summary>
    /// Occurs when a price update is received.
    /// </summary>
    event EventHandler<PriceUpdate>? PriceUpdateReceived;

    /// <summary>
    /// Occurs when an order book update is received.
    /// </summary>
    event EventHandler<OrderBookUpdate>? OrderBookUpdateReceived;

    /// <summary>
    /// Occurs when a trade update is received.
    /// </summary>
    event EventHandler<TradeUpdate>? TradeUpdateReceived;

    #endregion

    #region User Data Subscriptions

    /// <summary>
    /// Subscribes to real-time order updates for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToOrderUpdatesAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from real-time order updates for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier to unsubscribe from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromOrderUpdatesAsync(int accountId, CancellationToken cancellationToken = default);

    #endregion

    #region User Data Events

    /// <summary>
    /// Occurs when an order update is received.
    /// </summary>
    event EventHandler<OrderUpdate>? OrderUpdateReceived;

    #endregion
}
