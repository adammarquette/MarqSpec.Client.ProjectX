using MarqSpec.Client.ProjectX.Api.Models;
using Refit;

namespace MarqSpec.Client.ProjectX.Api.Rest;

/// <summary>
/// Defines the REST API interface for ProjectX using Refit.
/// </summary>
/// <remarks>
/// Real-time market data (live prices, order books, market trades) is provided via WebSocket.
/// REST API provides historical data, contract information, and order management.
/// </remarks>
public interface IProjectXRestApi
{
    /// <summary>
    /// Searches for contracts based on search criteria.
    /// </summary>
    /// <param name="request">The search criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results containing matching contracts.</returns>
    [Post("/api/Contract/search")]
    Task<SearchContractResponse> SearchContractsAsync([Body] SearchContractRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical price bars for a contract.
    /// </summary>
    /// <param name="request">The request specifying contract, time range, and bar interval.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The historical price bars.</returns>
    [Post("/api/History/retrieveBars")]
    Task<RetrieveBarResponse> RetrieveBarsAsync([Body] RetrieveBarRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="request">The order placement request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order placement response.</returns>
    [Post("/api/Order/place")]
    Task<PlaceOrderResponse> PlaceOrderAsync([Body] PlaceOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies an existing order.
    /// </summary>
    /// <param name="request">The order modification request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order modification response.</returns>
    [Post("/api/Order/modify")]
    Task<ModifyOrderResponse> ModifyOrderAsync([Body] ModifyOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="request">The order cancellation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order cancellation response.</returns>
    [Post("/api/Order/cancel")]
    Task<CancelOrderResponse> CancelOrderAsync([Body] CancelOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for orders based on the provided criteria.
    /// </summary>
    /// <param name="request">The search criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results.</returns>
    [Post("/api/Order/search")]
    Task<SearchOrderResponse> SearchOrdersAsync([Body] SearchOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open orders for an account.
    /// </summary>
    /// <param name="accountId">The account ID to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results containing open orders.</returns>
    [Get("/api/Order/searchOpen")]
    Task<SearchOrderResponse> GetOpenOrdersAsync([Query] int accountId, CancellationToken cancellationToken = default);
}
