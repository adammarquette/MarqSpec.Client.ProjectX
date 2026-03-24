using Microsoft.Extensions.Logging;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Refit;

namespace MarqSpec.Client.ProjectX;

/// <summary>
/// Implementation of the ProjectX API client.
/// </summary>
/// <remarks>
/// This client provides REST API access for order management, historical data, and contract queries.
/// For real-time market data (live prices, order books, market trades), use IProjectXWebSocketClient.
/// </remarks>
public class ProjectXApiClient : IProjectXApiClient
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiClient"/> class.
    /// </summary>
    /// <param name="restApi">The REST API interface.</param>
    /// <param name="authService">The authentication service.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectXApiClient(
        IProjectXRestApi restApi,
        IAuthenticationService authService,
        ILogger<ProjectXApiClient> logger)
    {
        _restApi = restApi;
        _authService = authService;
        _logger = logger;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        await _authService.GetAccessTokenAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contract>> SearchContractsAsync(string? searchText = null, bool live = true, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching contracts with text: {SearchText}, Live: {Live}", searchText, live);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new SearchContractRequest { SearchText = searchText, Live = live };
            var response = await _restApi.SearchContractsAsync(request, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to search contracts: {response.ErrorMessage}", response.ErrorCode);
            }

            _logger.LogInformation("Successfully retrieved {Count} contracts", response.Contracts.Count);
            return response.Contracts;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while searching contracts");
            throw new ProjectXApiException($"Failed to search contracts: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while searching contracts");
            throw new ProjectXApiException("Network error occurred while searching contracts.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching contracts");
            throw new ProjectXApiException("Unexpected error occurred while searching contracts.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Contract?> GetContractAsync(string contractId, bool live = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        try
        {
            _logger.LogDebug("Retrieving contract: {ContractId}, Live: {Live}", contractId, live);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new SearchContractRequest { SearchText = contractId, Live = live };
            var response = await _restApi.SearchContractsAsync(request, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to retrieve contract: {response.ErrorMessage}", response.ErrorCode);
            }

            var contract = response.Contracts.FirstOrDefault(c => c.Id == contractId);

            if (contract != null)
            {
                _logger.LogInformation("Successfully retrieved contract: {ContractId}", contractId);
            }
            else
            {
                _logger.LogDebug("Contract not found: {ContractId}", contractId);
            }

            return contract;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while retrieving contract: {ContractId}", contractId);
            throw new ProjectXApiException($"Failed to retrieve contract: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while retrieving contract: {ContractId}", contractId);
            throw new ProjectXApiException("Network error occurred while retrieving contract.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving contract: {ContractId}", contractId);
            throw new ProjectXApiException("Unexpected error occurred while retrieving contract.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AggregateBar>> GetHistoricalBarsAsync(
        string contractId,
        DateTime startTime,
        DateTime endTime,
        AggregateBarUnit unit,
        int unitNumber = 1,
        int limit = 1000,
        bool live = true,
        bool includePartialBar = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        if (unitNumber <= 0)
        {
            throw new ArgumentException("Unit number must be greater than zero.", nameof(unitNumber));
        }

        if (limit <= 0)
        {
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time must be before end time.", nameof(startTime));
        }

        if (startTime > DateTime.UtcNow)
        {
            throw new ArgumentException("Start time cannot be in the future.", nameof(startTime));
        }

        if (endTime > DateTime.UtcNow)
        {
            throw new ArgumentException("End time cannot be in the future.", nameof(endTime));
        }

        try
        {
            _logger.LogDebug("Retrieving historical bars for contract: {ContractId}, StartTime: {StartTime}, EndTime: {EndTime}, Unit: {Unit}, UnitNumber: {UnitNumber}",
                contractId, startTime, endTime, unit, unitNumber);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new RetrieveBarRequest
            {
                ContractId = contractId,
                StartTime = startTime,
                EndTime = endTime,
                Unit = unit,
                UnitNumber = unitNumber,
                Limit = limit,
                Live = live,
                IncludePartialBar = includePartialBar
            };

            var response = await _restApi.RetrieveBarsAsync(request, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to retrieve historical bars: {response.ErrorMessage}", response.ErrorCode);
            }

            _logger.LogInformation("Successfully retrieved {Count} historical bars for contract: {ContractId}",
                response.Bars.Count, contractId);

            return response.Bars;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while retrieving historical bars for contract: {ContractId}", contractId);
            throw new ProjectXApiException($"Failed to retrieve historical bars: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while retrieving historical bars for contract: {ContractId}", contractId);
            throw new ProjectXApiException("Network error occurred while retrieving historical bars.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving historical bars for contract: {ContractId}", contractId);
            throw new ProjectXApiException("Unexpected error occurred while retrieving historical bars.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PlaceOrderResponse> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.AccountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ContractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(request));
        }

        if (request.Size <= 0)
        {
            throw new ArgumentException("Order size must be greater than zero.", nameof(request));
        }

        try
        {
            _logger.LogDebug("Placing order for contract: {ContractId}, Account: {AccountId}, Type: {Type}, Side: {Side}, Size: {Size}", 
                request.ContractId, request.AccountId, request.Type, request.Side, request.Size);

            await EnsureAuthenticatedAsync(cancellationToken);
            var response = await _restApi.PlaceOrderAsync(request, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("Successfully placed order. Order ID: {OrderId}", response.OrderId);
            }
            else
            {
                _logger.LogWarning("Order placement failed. Error Code: {ErrorCode}, Message: {ErrorMessage}", 
                    response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while placing order for contract: {ContractId}", request.ContractId);
            throw new ProjectXApiException($"Failed to place order: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while placing order for contract: {ContractId}", request.ContractId);
            throw new ProjectXApiException("Network error occurred while placing order.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Unexpected error occurred while placing order for contract: {ContractId}", request.ContractId);
            throw new ProjectXApiException("Unexpected error occurred while placing order.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<ModifyOrderResponse> ModifyOrderAsync(ModifyOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.AccountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(request));
        }

        if (request.OrderId <= 0)
        {
            throw new ArgumentException("Order ID must be greater than zero.", nameof(request));
        }

        try
        {
            _logger.LogDebug("Modifying order: {OrderId}, Account: {AccountId}", request.OrderId, request.AccountId);

            await EnsureAuthenticatedAsync(cancellationToken);
            var response = await _restApi.ModifyOrderAsync(request, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("Successfully modified order: {OrderId}", request.OrderId);
            }
            else
            {
                _logger.LogWarning("Order modification failed. Error Code: {ErrorCode}, Message: {ErrorMessage}", 
                    response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while modifying order: {OrderId}", request.OrderId);
            throw new ProjectXApiException($"Failed to modify order: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while modifying order: {OrderId}", request.OrderId);
            throw new ProjectXApiException("Network error occurred while modifying order.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Unexpected error occurred while modifying order: {OrderId}", request.OrderId);
            throw new ProjectXApiException("Unexpected error occurred while modifying order.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CancelOrderResponse> CancelOrderAsync(int accountId, long orderId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        if (orderId <= 0)
        {
            throw new ArgumentException("Order ID must be greater than zero.", nameof(orderId));
        }

        try
        {
            _logger.LogDebug("Canceling order: {OrderId}, Account: {AccountId}", orderId, accountId);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new CancelOrderRequest { AccountId = accountId, OrderId = orderId };
            var response = await _restApi.CancelOrderAsync(request, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("Successfully canceled order: {OrderId}", orderId);
            }
            else
            {
                _logger.LogWarning("Order cancellation failed. Error Code: {ErrorCode}, Message: {ErrorMessage}", 
                    response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while canceling order: {OrderId}", orderId);
            throw new ProjectXApiException($"Failed to cancel order: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while canceling order: {OrderId}", orderId);
            throw new ProjectXApiException("Network error occurred while canceling order.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while canceling order: {OrderId}", orderId);
            throw new ProjectXApiException("Unexpected error occurred while canceling order.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Order?> GetOrderAsync(int accountId, long orderId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        try
        {
            _logger.LogDebug("Retrieving order: {OrderId}, Account: {AccountId}", orderId, accountId);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new SearchOrderRequest { AccountId = accountId };
            var response = await _restApi.SearchOrdersAsync(request, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to retrieve order: {response.ErrorMessage}", response.ErrorCode);
            }

            var order = response.Orders.FirstOrDefault(o => o.Id == orderId);

            if (order != null)
            {
                _logger.LogInformation("Successfully retrieved order: {OrderId}", orderId);
            }
            else
            {
                _logger.LogDebug("Order not found: {OrderId}", orderId);
            }

            return order;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while retrieving order: {OrderId}", orderId);
            throw new ProjectXApiException($"Failed to retrieve order: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while retrieving order: {OrderId}", orderId);
            throw new ProjectXApiException("Network error occurred while retrieving order.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving order: {OrderId}", orderId);
            throw new ProjectXApiException("Unexpected error occurred while retrieving order.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetOrdersAsync(int accountId, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        try
        {
            _logger.LogDebug("Retrieving orders for account: {AccountId}, StartTime: {StartTime}, EndTime: {EndTime}", 
                accountId, startTime, endTime);

            await EnsureAuthenticatedAsync(cancellationToken);
            var request = new SearchOrderRequest 
            { 
                AccountId = accountId, 
                StartTime = startTime, 
                EndTime = endTime 
            };
            var response = await _restApi.SearchOrdersAsync(request, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to retrieve orders: {response.ErrorMessage}", response.ErrorCode);
            }

            _logger.LogInformation("Successfully retrieved {Count} orders for account: {AccountId}", 
                response.Orders.Count, accountId);

            return response.Orders;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while retrieving orders for account: {AccountId}", accountId);
            throw new ProjectXApiException($"Failed to retrieve orders: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while retrieving orders for account: {AccountId}", accountId);
            throw new ProjectXApiException("Network error occurred while retrieving orders.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving orders for account: {AccountId}", accountId);
            throw new ProjectXApiException("Unexpected error occurred while retrieving orders.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetOpenOrdersAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        try
        {
            _logger.LogDebug("Retrieving open orders for account: {AccountId}", accountId);

            await EnsureAuthenticatedAsync(cancellationToken);
            var response = await _restApi.GetOpenOrdersAsync(accountId, cancellationToken);

            if (!response.Success)
            {
                throw new ProjectXApiException($"Failed to retrieve open orders: {response.ErrorMessage}", response.ErrorCode);
            }

            _logger.LogInformation("Successfully retrieved {Count} open orders for account: {AccountId}", 
                response.Orders.Count, accountId);

            return response.Orders;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error occurred while retrieving open orders for account: {AccountId}", accountId);
            throw new ProjectXApiException($"Failed to retrieve open orders: {ex.Message}", (int)ex.StatusCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while retrieving open orders for account: {AccountId}", accountId);
            throw new ProjectXApiException("Network error occurred while retrieving open orders.", ex);
        }
        catch (Exception ex) when (ex is not ProjectXApiException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving open orders for account: {AccountId}", accountId);
            throw new ProjectXApiException("Unexpected error occurred while retrieving open orders.", ex);
        }
    }
}
