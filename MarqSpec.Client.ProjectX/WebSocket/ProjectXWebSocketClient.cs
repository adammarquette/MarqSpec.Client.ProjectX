using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Implementation of the ProjectX WebSocket client for real-time market data and order updates.
/// </summary>
public class ProjectXWebSocketClient : IProjectXWebSocketClient
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXWebSocketClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WebSocketOptions _options;

    private HubConnection? _marketHub;
    private HubConnection? _userHub;
    private ConnectionState _marketHubState = ConnectionState.Disconnected;
    private ConnectionState _userHubState = ConnectionState.Disconnected;

    private readonly SemaphoreSlim _marketHubLock = new(1, 1);
    private readonly SemaphoreSlim _userHubLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXWebSocketClient"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="options">The WebSocket configuration options.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectXWebSocketClient(
        IAuthenticationService authService,
        IOptions<WebSocketOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<ProjectXWebSocketClient> logger)
    {
        _authService = authService;
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    #region Connection Management

    /// <inheritdoc/>
    public ConnectionState MarketHubState => _marketHubState;

    /// <inheritdoc/>
    public ConnectionState UserHubState => _userHubState;

    /// <inheritdoc/>
    public event EventHandler<ConnectionStatusChange>? ConnectionStatusChanged;

    /// <inheritdoc/>
    public async Task ConnectMarketHubAsync(CancellationToken cancellationToken = default)
    {
        await _marketHubLock.WaitAsync(cancellationToken);
        try
        {
            if (_marketHub != null && _marketHubState == ConnectionState.Connected)
            {
                _logger.LogDebug("Market hub is already connected");
                return;
            }

            _logger.LogInformation("Connecting to market hub: {Url}", _options.MarketHubUrl);
            UpdateMarketHubState(ConnectionState.Connecting);

            var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
            _marketHub = BuildHubConnection(_options.MarketHubUrl, accessToken);
            ConfigureMarketHubHandlers(_marketHub);

            await _marketHub.StartAsync(cancellationToken);
            UpdateMarketHubState(ConnectionState.Connected);

            _logger.LogInformation("Successfully connected to market hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to market hub");
            UpdateMarketHubState(ConnectionState.Failed, ex);
            throw;
        }
        finally
        {
            _marketHubLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task ConnectUserHubAsync(CancellationToken cancellationToken = default)
    {
        await _userHubLock.WaitAsync(cancellationToken);
        try
        {
            if (_userHub != null && _userHubState == ConnectionState.Connected)
            {
                _logger.LogDebug("User hub is already connected");
                return;
            }

            _logger.LogInformation("Connecting to user hub: {Url}", _options.UserHubUrl);
            UpdateUserHubState(ConnectionState.Connecting);

            var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
            _userHub = BuildHubConnection(_options.UserHubUrl, accessToken);
            ConfigureUserHubHandlers(_userHub);

            await _userHub.StartAsync(cancellationToken);
            UpdateUserHubState(ConnectionState.Connected);

            _logger.LogInformation("Successfully connected to user hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to user hub");
            UpdateUserHubState(ConnectionState.Failed, ex);
            throw;
        }
        finally
        {
            _userHubLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectMarketHubAsync(CancellationToken cancellationToken = default)
    {
        await _marketHubLock.WaitAsync(cancellationToken);
        try
        {
            if (_marketHub == null || _marketHubState == ConnectionState.Disconnected)
            {
                _logger.LogDebug("Market hub is already disconnected");
                return;
            }

            _logger.LogInformation("Disconnecting from market hub");
            await _marketHub.StopAsync(cancellationToken);
            UpdateMarketHubState(ConnectionState.Disconnected);
            _logger.LogInformation("Successfully disconnected from market hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from market hub");
            throw;
        }
        finally
        {
            _marketHubLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectUserHubAsync(CancellationToken cancellationToken = default)
    {
        await _userHubLock.WaitAsync(cancellationToken);
        try
        {
            if (_userHub == null || _userHubState == ConnectionState.Disconnected)
            {
                _logger.LogDebug("User hub is already disconnected");
                return;
            }

            _logger.LogInformation("Disconnecting from user hub");
            await _userHub.StopAsync(cancellationToken);
            UpdateUserHubState(ConnectionState.Disconnected);
            _logger.LogInformation("Successfully disconnected from user hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from user hub");
            throw;
        }
        finally
        {
            _userHubLock.Release();
        }
    }

    #endregion

    #region Market Data Subscriptions

    /// <inheritdoc/>
    public async Task SubscribeToPriceUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to price updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("SubscribeToPrices", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully subscribed to price updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to price updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "SubscribeToPrices", [contractId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromPriceUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from price updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("UnsubscribeFromPrices", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully unsubscribed from price updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from price updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "UnsubscribeFromPrices", [contractId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SubscribeToOrderBookUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to order book updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("SubscribeToDepth", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully subscribed to order book updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to order book updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "SubscribeToDepth", [contractId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromOrderBookUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from order book updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("UnsubscribeFromDepth", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully unsubscribed from order book updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from order book updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "UnsubscribeFromDepth", [contractId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SubscribeToTradeUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to trade updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("SubscribeToTrades", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully subscribed to trade updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to trade updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "SubscribeToTrades", [contractId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromTradeUpdatesAsync(string contractId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Contract ID cannot be null or whitespace.", nameof(contractId));
        }

        EnsureMarketHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from trade updates for contract: {ContractId}", contractId);
            await _marketHub!.InvokeAsync("UnsubscribeFromTrades", new[] { contractId }, cancellationToken);
            _logger.LogInformation("Successfully unsubscribed from trade updates for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from trade updates for contract: {ContractId}", contractId);
            RaiseMessageSendFailed("Market", "UnsubscribeFromTrades", [contractId], ex);
            throw;
        }
    }

    #endregion

    #region Market Data Events

    /// <inheritdoc/>
    public event EventHandler<PriceUpdate>? PriceUpdateReceived;

    /// <inheritdoc/>
    public event EventHandler<OrderBookUpdate>? OrderBookUpdateReceived;

    /// <inheritdoc/>
    public event EventHandler<TradeUpdate>? TradeUpdateReceived;

    #endregion

    #region User Data Subscriptions

    /// <inheritdoc/>
    public async Task SubscribeToOrderUpdatesAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to order updates for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync("SubscribeToOrders", new[] { accountId }, cancellationToken);
            _logger.LogInformation("Successfully subscribed to order updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to order updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "SubscribeToOrders", [accountId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromOrderUpdatesAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from order updates for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync("UnsubscribeFromOrders", new[] { accountId }, cancellationToken);
            _logger.LogInformation("Successfully unsubscribed from order updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from order updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "UnsubscribeFromOrders", [accountId], ex);
            throw;
        }
    }

    #endregion

    #region User Data Events

    /// <inheritdoc/>
    public event EventHandler<OrderUpdate>? OrderUpdateReceived;

    #endregion

    #region Error Reporting

    /// <inheritdoc/>
    public event EventHandler<WebSocketMessageFailedEventArgs>? MessageSendFailed;

    #endregion

    #region Private Helper Methods

    private HubConnection BuildHubConnection(string hubUrl, string accessToken)
    {
        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Use a delegate that fetches a fresh token on each reconnect
                options.AccessTokenProvider = async () => await _authService.GetAccessTokenAsync();
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
            })
            .WithAutomaticReconnect(new ReconnectPolicy(_options))
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new LoggerFactoryProvider(_loggerFactory));
            });

        return builder.Build();
    }

    private void ConfigureMarketHubHandlers(HubConnection connection)
    {
        // Price/Quote updates
        connection.On<PriceUpdate>("Quote", update =>
        {
            _logger.LogTrace("Received price update for contract: {ContractId}", update.ContractId);
            PriceUpdateReceived?.Invoke(this, update);
        });

        // Order book/Depth updates
        connection.On<OrderBookUpdate>("Depth", update =>
        {
            _logger.LogTrace("Received order book update for contract: {ContractId}", update.ContractId);
            OrderBookUpdateReceived?.Invoke(this, update);
        });

        // Trade updates
        connection.On<TradeUpdate>("Trade", update =>
        {
            _logger.LogTrace("Received trade update for contract: {ContractId}", update.ContractId);
            TradeUpdateReceived?.Invoke(this, update);
        });

        // Connection lifecycle
        connection.Closed += async (error) =>
        {
            _logger.LogWarning(error, "Market hub connection closed");
            UpdateMarketHubState(ConnectionState.Disconnected, error);
            
            if (_options.AutoReconnect && error != null)
            {
                _logger.LogInformation("Auto-reconnect enabled, will attempt to reconnect market hub");
            }
        };

        connection.Reconnecting += (error) =>
        {
            _logger.LogWarning(error, "Market hub is reconnecting");
            UpdateMarketHubState(ConnectionState.Reconnecting, error);
            return Task.CompletedTask;
        };

        connection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("Market hub reconnected with connection ID: {ConnectionId}", connectionId);
            UpdateMarketHubState(ConnectionState.Connected);
            return Task.CompletedTask;
        };
    }

    private void ConfigureUserHubHandlers(HubConnection connection)
    {
        // Order updates
        connection.On<OrderUpdate>("Order", update =>
        {
            _logger.LogTrace("Received order update for order: {OrderId}, Account: {AccountId}", 
                update.OrderId, update.AccountId);
            OrderUpdateReceived?.Invoke(this, update);
        });

        // Connection lifecycle
        connection.Closed += async (error) =>
        {
            _logger.LogWarning(error, "User hub connection closed");
            UpdateUserHubState(ConnectionState.Disconnected, error);
            
            if (_options.AutoReconnect && error != null)
            {
                _logger.LogInformation("Auto-reconnect enabled, will attempt to reconnect user hub");
            }
        };

        connection.Reconnecting += (error) =>
        {
            _logger.LogWarning(error, "User hub is reconnecting");
            UpdateUserHubState(ConnectionState.Reconnecting, error);
            return Task.CompletedTask;
        };

        connection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("User hub reconnected with connection ID: {ConnectionId}", connectionId);
            UpdateUserHubState(ConnectionState.Connected);
            return Task.CompletedTask;
        };
    }

    private void UpdateMarketHubState(ConnectionState newState, Exception? exception = null)
    {
        var previousState = _marketHubState;
        _marketHubState = newState;

        var change = new ConnectionStatusChange
        {
            PreviousState = previousState,
            CurrentState = newState,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = exception?.Message,
            Exception = exception
        };

        ConnectionStatusChanged?.Invoke(this, change);
    }

    private void UpdateUserHubState(ConnectionState newState, Exception? exception = null)
    {
        var previousState = _userHubState;
        _userHubState = newState;

        var change = new ConnectionStatusChange
        {
            PreviousState = previousState,
            CurrentState = newState,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = exception?.Message,
            Exception = exception
        };

        ConnectionStatusChanged?.Invoke(this, change);
    }

    private void EnsureMarketHubConnected()
    {
        if (_marketHub == null || _marketHubState != ConnectionState.Connected)
        {
            throw new InvalidOperationException(
                "Market hub is not connected. Call ConnectMarketHubAsync() before subscribing.");
        }
    }

    private void EnsureUserHubConnected()
    {
        if (_userHub == null || _userHubState != ConnectionState.Connected)
        {
            throw new InvalidOperationException(
                "User hub is not connected. Call ConnectUserHubAsync() before subscribing.");
        }
    }

    private void RaiseMessageSendFailed(string hubName, string methodName, object?[] arguments, Exception exception)
    {
        MessageSendFailed?.Invoke(this, new WebSocketMessageFailedEventArgs
        {
            HubName = hubName,
            MethodName = methodName,
            Arguments = arguments,
            Exception = exception
        });
    }

    #endregion

    #region IAsyncDisposable

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_marketHub != null)
        {
            await DisconnectMarketHubAsync();
            await _marketHub.DisposeAsync();
            _marketHub = null;
        }

        if (_userHub != null)
        {
            await DisconnectUserHubAsync();
            await _userHub.DisposeAsync();
            _userHub = null;
        }

        _marketHubLock.Dispose();
        _userHubLock.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Nested Types

    private class ReconnectPolicy : IRetryPolicy
    {
        private readonly WebSocketOptions _options;

        public ReconnectPolicy(WebSocketOptions options)
        {
            _options = options;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Progressive backoff with max delay of 5 seconds (per PRD requirement)
            var delaySeconds = Math.Min(
                _options.InitialReconnectDelaySeconds * Math.Pow(2, retryContext.PreviousRetryCount),
                _options.MaxReconnectDelaySeconds);

            return TimeSpan.FromSeconds(delaySeconds);
        }
    }

    private class LoggerFactoryProvider : ILoggerProvider
    {
        private readonly ILoggerFactory _factory;

        public LoggerFactoryProvider(ILoggerFactory factory) => _factory = factory;

        public ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
        public void Dispose() { }
    }

    #endregion
}
