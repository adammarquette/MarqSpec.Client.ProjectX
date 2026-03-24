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
    /// <param name="live">Whether to search in live or simulation contracts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The contract if found, otherwise null.</returns>
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
    /// Gets a specific order by ID.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order if found, otherwise null.</returns>
    Task<Order?> GetOrderAsync(int accountId, long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders within a specified time range.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="startTime">The start time for the search range.</param>
    /// <param name="endTime">The end time for the search range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of orders matching the criteria.</returns>
    Task<IEnumerable<Order>> GetOrdersAsync(int accountId, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open orders for an account.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of open orders.</returns>
    Task<IEnumerable<Order>> GetOpenOrdersAsync(int accountId, CancellationToken cancellationToken = default);
}
