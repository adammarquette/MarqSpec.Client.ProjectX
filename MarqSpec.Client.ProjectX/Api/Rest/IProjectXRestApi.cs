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
    /// Searches for trading accounts based on the provided criteria.
    /// </summary>
    /// <param name="request">The search criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results containing matching accounts.</returns>
    [Post("/api/Account/search")]
    Task<SearchAccountResponse> SearchAccountsAsync([Body] SearchAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for contracts based on search criteria.
    /// </summary>
    /// <param name="request">The search criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results containing matching contracts.</returns>
    [Post("/api/Contract/search")]
    Task<SearchContractResponse> SearchContractsAsync([Body] SearchContractRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for a contract by its unique ID.
    /// </summary>
    /// <param name="request">The request containing the contract ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response containing the matching contract if found.</returns>
    [Post("/api/Contract/searchById")]
    Task<SearchContractByIdResponse> SearchContractByIdAsync([Body] SearchContractByIdRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently available contracts.
    /// </summary>
    /// <param name="request">The request specifying live or simulation contracts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response containing the available contracts.</returns>
    [Post("/api/Contract/available")]
    Task<ListAvailableContractResponse> GetAvailableContractsAsync([Body] ListAvailableContractRequest request, CancellationToken cancellationToken = default);

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
    /// <param name="request">The request containing the account ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The search results containing open orders.</returns>
    [Post("/api/Order/searchOpen")]
    Task<SearchOrderResponse> SearchOpenOrdersAsync([Body] SearchOpenOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for open positions on an account.
    /// </summary>
    /// <param name="request">The request containing the account ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response containing the open positions.</returns>
    [Post("/api/Position/searchOpen")]
    Task<SearchPositionResponse> SearchOpenPositionsAsync([Body] SearchPositionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the full position for a contract on an account.
    /// </summary>
    /// <param name="request">The request containing the account ID and contract ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response indicating success or failure.</returns>
    [Post("/api/Position/closeContract")]
    Task<ClosePositionResponse> CloseContractPositionAsync([Body] CloseContractPositionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially closes a position for a contract on an account.
    /// </summary>
    /// <param name="request">The request containing the account ID, contract ID, and size to close.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response indicating success or failure.</returns>
    [Post("/api/Position/partialCloseContract")]
    Task<PartialClosePositionResponse> PartialCloseContractPositionAsync([Body] PartialCloseContractPositionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for half-turn trade executions on an account.
    /// </summary>
    /// <param name="request">The search criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response containing the matching trades.</returns>
    [Post("/api/Trade/search")]
    Task<SearchTradeResponse> SearchTradesAsync([Body] SearchTradeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the API is responsive.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The string <c>"pong"</c> if the API is available.</returns>
    [Get("/api/Status/ping")]
    Task<string> PingAsync(CancellationToken cancellationToken = default);
}

