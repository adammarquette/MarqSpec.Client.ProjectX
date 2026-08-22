using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX;

/// <summary>
/// Provides methods to interact with the ProjectX API.
/// </summary>
/// <remarks>
/// Note: Real-time market data (live prices, order books, market trades) is provided via WebSocket streams.
/// See IProjectXWebSocketClient for real-time market data subscriptions.
/// This interface provides historical data and contract queries via REST.
/// </remarks>
public interface IProjectXApiClient
{
    /// <summary>
    /// Gets trading accounts for the authenticated user.
    /// </summary>
    /// <param name="onlyActiveAccounts">When <see langword="true"/>, returns only active accounts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of trading accounts matching the criteria.</returns>
    Task<IEnumerable<TradingAccount>> GetAccountsAsync(bool onlyActiveAccounts = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for contracts based on search criteria.
    /// </summary>
    /// <param name="searchText">Optional search text to filter contracts.</param>
    /// <param name="live">Whether to search live or simulation contracts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of contracts matching the criteria.</returns>
    Task<IEnumerable<Contract>> SearchContractsAsync(string? searchText = null, bool live = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific contract by its ID.
    /// </summary>
    /// <param name="contractId">The contract ID to retrieve.</param>
    /// <param name="live">
    /// <b>Ignored.</b> The gateway's <c>SearchContractByIdRequest</c> schema defines only <c>contractId</c>,
    /// so a by-ID lookup cannot be scoped to live or simulation. Passing <see langword="false"/> does not
    /// restrict the result.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The contract if found, otherwise null.</returns>
    /// <remarks>
    /// <see cref="SearchContractsAsync"/> and <see cref="GetAvailableContractsAsync"/> take a <c>live</c> that
    /// <i>is</i> honoured — their request schemas define the field. This one never did.
    /// </remarks>
    [Obsolete("The live flag is ignored: the gateway's SearchContractByIdRequest schema has no such "
          + "field, so a contract lookup by ID cannot be scoped to live or simulation. Use "
          + "GetContractByIdAsync(contractId, cancellationToken), which is what this already calls "
          + "(gh#76).")]
    Task<Contract?> GetContractAsync(string contractId, bool live = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical price bars for a contract.
    /// </summary>
    /// <param name="contractId">The contract ID to retrieve bars for.</param>
    /// <param name="startTime">The start time for the bar retrieval.</param>
    /// <param name="endTime">The end time for the bar retrieval.</param>
    /// <param name="unit">The time unit for bar aggregation.</param>
    /// <param name="unitNumber">The number of units per bar (e.g., 5 for 5-minute bars).</param>
    /// <param name="limit">The maximum number of bars to return.</param>
    /// <param name="live">Whether to retrieve live or simulation data.</param>
    /// <param name="includePartialBar">Whether to include partial (incomplete) bars.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The historical price bars.</returns>
    Task<IEnumerable<AggregateBar>> GetHistoricalBarsAsync(
        string contractId,
        DateTime startTime,
        DateTime endTime,
        AggregateBarUnit unit,
        int unitNumber = 1,
        int limit = 1000,
        bool live = true,
        bool includePartialBar = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="request">The order placement request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order placement response.</returns>
    Task<PlaceOrderResponse> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies an existing order.
    /// </summary>
    /// <param name="request">The order modification request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order modification response.</returns>
    Task<ModifyOrderResponse> ModifyOrderAsync(ModifyOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="orderId">The order ID to cancel.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order cancellation response.</returns>
    Task<CancelOrderResponse> CancelOrderAsync(int accountId, long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by ID, searching the supplied window.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="startTime">
    /// The start of the window to search. <b>Required by the gateway</b> — see the remarks.
    /// </param>
    /// <param name="endTime">The end of the window, or null for "up to now".</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order if found within the window, otherwise null.</returns>
    /// <remarks>
    /// There is no by-ID route on the gateway, so this searches <c>/api/Order/search</c> and filters. That
    /// endpoint's schema marks <c>startTimestamp</c> required, and <b>an order outside the window is returned
    /// as absent</b> — indistinguishable from "no such order". Choose a window that certainly contains the
    /// order; when reconciling a placement, that means one starting before the placement was attempted.
    /// </remarks>
    Task<Order?> GetOrderAsync(int accountId, long orderId, DateTime startTime, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by ID.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order if found, otherwise null.</returns>
    [Obsolete(
        "A search window is required. This overload sent no startTimestamp, which the gateway's " +
        "/api/Order/search schema marks required; the gateway then applied a window of its own and an order " +
        "outside it came back as null -- a live order reported as not found. Use " +
        "GetOrderAsync(accountId, orderId, startTime, endTime, cancellationToken) with a window that " +
        "certainly contains the order (gh#57).",
        error: true)]
    Task<Order?> GetOrderAsync(int accountId, long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders within a specified time range.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="startTime">
    /// The start of the search range. <b>Required</b> — the gateway's <c>/api/Order/search</c> schema marks
    /// <c>startTimestamp</c> required, and passing null throws <see cref="ArgumentException"/> rather than
    /// letting the gateway silently choose a window.
    /// </param>
    /// <param name="endTime">The end time for the search range, or null for "up to now".</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of orders matching the criteria.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="startTime"/> is null, or <paramref name="accountId"/> is not greater than zero.
    /// </exception>
    /// <remarks>
    /// <paramref name="startTime"/> stays nullable so the signature is unchanged, but null is now rejected: an
    /// order outside whatever window the gateway picks is returned as absent, and a caller cannot tell that
    /// apart from "no such order" (gh#57).
    /// </remarks>
    Task<IEnumerable<Order>> GetOrdersAsync(int accountId, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific contract by its ID using a direct lookup.
    /// </summary>
    /// <param name="contractId">The exact contract ID to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The contract if found, otherwise <see langword="null"/>.</returns>
    Task<Contract?> GetContractByIdAsync(string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently available contracts.
    /// </summary>
    /// <param name="live">Whether to list live or simulation contracts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of available contracts.</returns>
    Task<IEnumerable<Contract>> GetAvailableContractsAsync(bool live = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open orders for an account.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of open orders.</returns>
    Task<IEnumerable<Order>> GetOpenOrdersAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open positions for an account.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of open positions.</returns>
    Task<IEnumerable<Position>> GetOpenPositionsAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the full position for a contract on an account.
    /// </summary>
    /// <param name="accountId">The account ID that holds the position.</param>
    /// <param name="contractId">The contract ID of the position to close.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The close position response.</returns>
    Task<ClosePositionResponse> ClosePositionAsync(int accountId, string contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially closes a position for a contract on an account.
    /// </summary>
    /// <param name="accountId">The account ID that holds the position.</param>
    /// <param name="contractId">The contract ID of the position to partially close.</param>
    /// <param name="size">The number of contracts to close.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The partial close position response.</returns>
    Task<PartialClosePositionResponse> PartialClosePositionAsync(int accountId, string contractId, int size, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for half-turn trade executions on an account within an optional time range.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of trade executions matching the criteria.</returns>
    Task<IEnumerable<HalfTrade>> GetTradesAsync(int accountId, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the API is responsive.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the API returned a pong response.</returns>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}
