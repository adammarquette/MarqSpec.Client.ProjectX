using System.Collections.Concurrent;
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

    private const string SubscribeContractQuotes = "SubscribeContractQuotes";
    private const string SubscribeContractMarketDepth = "SubscribeContractMarketDepth";
    private const string SubscribeContractTrades = "SubscribeContractTrades";
    private const string SubscribeAccounts = "SubscribeAccounts";
    private const string SubscribeOrders = "SubscribeOrders";
    private const string SubscribePositions = "SubscribePositions";
    private const string SubscribeTrades = "SubscribeTrades";

    private IHubConnectionAdapter? _marketHub;
    private IHubConnectionAdapter? _userHub;
    private ConnectionState _marketHubState = ConnectionState.Disconnected;
    private ConnectionState _userHubState = ConnectionState.Disconnected;

    private readonly ConcurrentDictionary<HubSubscription, byte> _marketSubscriptions = new();
    private readonly ConcurrentDictionary<HubSubscription, byte> _userSubscriptions = new();

    /// <summary>
    /// Test seam: when set, connect uses this instead of building a real
    /// <see cref="HubConnection"/>. Unit tests must not open a socket (gh#87).
    /// </summary>
    internal Func<string, IHubConnectionAdapter>? HubConnectionFactory { get; set; }

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
    public MarketHubSubscriptions MarketSubscriptions => new()
    {
        PriceContractIds = SnapshotContracts(SubscribeContractQuotes),
        OrderBookContractIds = SnapshotContracts(SubscribeContractMarketDepth),
        TradeContractIds = SnapshotContracts(SubscribeContractTrades)
    };

    /// <inheritdoc/>
    public UserHubSubscriptions UserSubscriptions => new()
    {
        Accounts = _userSubscriptions.ContainsKey(new HubSubscription(SubscribeAccounts, null)),
        OrderAccountIds = SnapshotAccounts(SubscribeOrders),
        PositionAccountIds = SnapshotAccounts(SubscribePositions),
        TradeAccountIds = SnapshotAccounts(SubscribeTrades)
    };

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

            // Proves credentials resolve before a connection is attempted; the connection itself gets its
            // token from AccessTokenProvider, which is re-invoked on every reconnect.
            await _authService.GetAccessTokenAsync(cancellationToken);
            if (_marketHub != null)
            {
                await _marketHub.DisposeAsync();
                _marketHub = null;
            }

            _marketHub = CreateHubConnection(_options.MarketHubUrl);
            ConfigureMarketHubHandlers(_marketHub);

            await _marketHub.StartAsync(cancellationToken);
            await RestoreSubscriptionsAsync(_marketHub, _marketSubscriptions, "Market", cancellationToken);
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

            // Proves credentials resolve before a connection is attempted; the connection itself gets its
            // token from AccessTokenProvider, which is re-invoked on every reconnect.
            await _authService.GetAccessTokenAsync(cancellationToken);
            if (_userHub != null)
            {
                await _userHub.DisposeAsync();
                _userHub = null;
            }

            _userHub = CreateHubConnection(_options.UserHubUrl);
            ConfigureUserHubHandlers(_userHub);

            await _userHub.StartAsync(cancellationToken);
            await RestoreSubscriptionsAsync(_userHub, _userSubscriptions, "User", cancellationToken);
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
            await _marketHub!.InvokeAsync(SubscribeContractQuotes, [contractId], cancellationToken);
            Record(_marketSubscriptions, SubscribeContractQuotes, contractId);
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
            await _marketHub!.InvokeAsync("UnsubscribeContractQuotes", [contractId], cancellationToken);
            Forget(_marketSubscriptions, SubscribeContractQuotes, contractId);
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
            await _marketHub!.InvokeAsync(SubscribeContractMarketDepth, [contractId], cancellationToken);
            Record(_marketSubscriptions, SubscribeContractMarketDepth, contractId);
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
            await _marketHub!.InvokeAsync("UnsubscribeContractMarketDepth", [contractId], cancellationToken);
            Forget(_marketSubscriptions, SubscribeContractMarketDepth, contractId);
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
            await _marketHub!.InvokeAsync(SubscribeContractTrades, [contractId], cancellationToken);
            Record(_marketSubscriptions, SubscribeContractTrades, contractId);
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
            await _marketHub!.InvokeAsync("UnsubscribeContractTrades", [contractId], cancellationToken);
            Forget(_marketSubscriptions, SubscribeContractTrades, contractId);
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
            await _userHub!.InvokeAsync(SubscribeOrders, [accountId], cancellationToken);
            Record(_userSubscriptions, SubscribeOrders, accountId);
            _logger.LogInformation("Successfully subscribed to order updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to order updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "SubscribeOrders", [accountId], ex);
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
            await _userHub!.InvokeAsync("UnsubscribeOrders", [accountId], cancellationToken);
            Forget(_userSubscriptions, SubscribeOrders, accountId);
            _logger.LogInformation("Successfully unsubscribed from order updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from order updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "UnsubscribeOrders", [accountId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SubscribeToAccountUpdatesAsync(CancellationToken cancellationToken = default)
    {
        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to account updates");
            await _userHub!.InvokeAsync(SubscribeAccounts, [], cancellationToken);
            Record(_userSubscriptions, SubscribeAccounts, null);
            _logger.LogInformation("Successfully subscribed to account updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to account updates");
            RaiseMessageSendFailed("User", "SubscribeAccounts", [], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromAccountUpdatesAsync(CancellationToken cancellationToken = default)
    {
        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from account updates");
            await _userHub!.InvokeAsync("UnsubscribeAccounts", [], cancellationToken);
            Forget(_userSubscriptions, SubscribeAccounts, null);
            _logger.LogInformation("Successfully unsubscribed from account updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from account updates");
            RaiseMessageSendFailed("User", "UnsubscribeAccounts", [], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SubscribeToPositionUpdatesAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to position updates for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync(SubscribePositions, [accountId], cancellationToken);
            Record(_userSubscriptions, SubscribePositions, accountId);
            _logger.LogInformation("Successfully subscribed to position updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to position updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "SubscribePositions", [accountId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromPositionUpdatesAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from position updates for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync("UnsubscribePositions", [accountId], cancellationToken);
            Forget(_userSubscriptions, SubscribePositions, accountId);
            _logger.LogInformation("Successfully unsubscribed from position updates for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from position updates for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "UnsubscribePositions", [accountId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SubscribeToTradeNotificationsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Subscribing to trade notifications for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync(SubscribeTrades, [accountId], cancellationToken);
            Record(_userSubscriptions, SubscribeTrades, accountId);
            _logger.LogInformation("Successfully subscribed to trade notifications for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to trade notifications for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "SubscribeTrades", [accountId], ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromTradeNotificationsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            throw new ArgumentException("Account ID must be greater than zero.", nameof(accountId));
        }

        EnsureUserHubConnected();

        try
        {
            _logger.LogDebug("Unsubscribing from trade notifications for account: {AccountId}", accountId);
            await _userHub!.InvokeAsync("UnsubscribeTrades", [accountId], cancellationToken);
            Forget(_userSubscriptions, SubscribeTrades, accountId);
            _logger.LogInformation("Successfully unsubscribed from trade notifications for account: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from trade notifications for account: {AccountId}", accountId);
            RaiseMessageSendFailed("User", "UnsubscribeTrades", [accountId], ex);
            throw;
        }
    }

    #endregion

    #region User Data Events

    /// <inheritdoc/>
    public event EventHandler<AccountUpdate>? AccountUpdateReceived;

    /// <inheritdoc/>
    public event EventHandler<OrderUpdate>? OrderUpdateReceived;

    /// <inheritdoc/>
    public event EventHandler<PositionUpdate>? PositionUpdateReceived;

    /// <inheritdoc/>
    public event EventHandler<TradeNotification>? TradeNotificationReceived;

    #endregion

    #region Error Reporting

    /// <inheritdoc/>
    public event EventHandler<WebSocketMessageFailedEventArgs>? MessageSendFailed;

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds a hub connection with every <see cref="WebSocketOptions"/> setting applied.
    /// </summary>
    /// <remarks>
    /// Internal rather than private so the unit suite can assert that the options actually reach the
    /// connection. <c>HandshakeTimeoutSeconds</c>, <c>KeepAliveIntervalSeconds</c>, <c>ServerTimeoutSeconds</c>
    /// and <c>MaxBufferSize</c> were all bound, documented and never read (gh#69); a test that only checks
    /// binding would not have caught that.
    /// </remarks>
    internal HubConnection BuildHubConnection(string hubUrl)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // A delegate, not a captured token: SignalR calls this again on every reconnect, so the
                // connection cannot come back up holding a token that expired while it was down.
                options.AccessTokenProvider = async () => await _authService.GetAccessTokenAsync();

                // Applies to both directions of the transport. Bound and documented since 1.0.x; never read
                // until gh#69.
                options.TransportMaxBufferSize = _options.MaxBufferSize;
                options.ApplicationMaxBufferSize = _options.MaxBufferSize;
            })
            .WithAutomaticReconnect(new ReconnectPolicy(_options))
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new LoggerFactoryProvider(_loggerFactory));
            })
            .Build();

        // Set on the connection rather than the builder: the builder-level extension methods for these
        // arrived after net8.0, and both target frameworks are first-class (ADR-0005).
        connection.HandshakeTimeout = TimeSpan.FromSeconds(_options.HandshakeTimeoutSeconds);
        connection.KeepAliveInterval = TimeSpan.FromSeconds(_options.KeepAliveIntervalSeconds);
        connection.ServerTimeout = TimeSpan.FromSeconds(_options.ServerTimeoutSeconds);

        return connection;
    }

    /// <summary>
    /// Builds the hub adapter, or the test fake when <see cref="HubConnectionFactory"/> is set.
    /// </summary>
    internal IHubConnectionAdapter CreateHubConnection(string hubUrl)
    {
        if (HubConnectionFactory is not null)
        {
            return HubConnectionFactory(hubUrl);
        }

        return new SignalRHubConnectionAdapter(BuildHubConnection(hubUrl));
    }

    /// <summary>
    /// Restores market-hub subscriptions after SignalR automatic reconnect (gh#87).
    /// </summary>
    internal async Task HandleMarketHubReconnectedAsync(string? connectionId)
    {
        await _marketHubLock.WaitAsync();
        try
        {
            _logger.LogInformation("Market hub reconnected with connection ID: {ConnectionId}", connectionId);

            try
            {
                await RestoreSubscriptionsAsync(_marketHub, _marketSubscriptions, "Market", CancellationToken.None);
                UpdateMarketHubState(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore market hub subscriptions after reconnect");
                UpdateMarketHubState(ConnectionState.Failed, ex);
            }
        }
        finally
        {
            _marketHubLock.Release();
        }
    }

    /// <summary>
    /// Restores user-hub subscriptions after SignalR automatic reconnect (gh#87).
    /// </summary>
    internal async Task HandleUserHubReconnectedAsync(string? connectionId)
    {
        await _userHubLock.WaitAsync();
        try
        {
            _logger.LogInformation("User hub reconnected with connection ID: {ConnectionId}", connectionId);

            try
            {
                await RestoreSubscriptionsAsync(_userHub, _userSubscriptions, "User", CancellationToken.None);
                UpdateUserHubState(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore user hub subscriptions after reconnect");
                UpdateUserHubState(ConnectionState.Failed, ex);
            }
        }
        finally
        {
            _userHubLock.Release();
        }
    }

    /// <summary>
    /// Applies the hub <paramref name="contractId"/> to a quote and raises
    /// <see cref="PriceUpdateReceived"/>. Visible to unit tests (gh#86).
    /// </summary>
    internal void HandleGatewayQuote(string contractId, PriceUpdate? update)
    {
        if (update is null)
        {
            return;
        }

        update.ContractId = contractId;
        _logger.LogTrace("Received price update for contract {ContractId} symbol {Symbol}", contractId, update.Symbol);
        PriceUpdateReceived?.Invoke(this, update);
    }

    /// <summary>
    /// Applies the hub <paramref name="contractId"/> to each depth row and raises
    /// <see cref="OrderBookUpdateReceived"/>. Visible to unit tests (gh#86).
    /// </summary>
    internal void HandleGatewayDepth(string contractId, OrderBookUpdate[]? updates)
    {
        foreach (var update in updates ?? [])
        {
            if (update is null)
            {
                continue;
            }

            update.ContractId = contractId;
            _logger.LogTrace(
                "Received DOM update for contract {ContractId} type {Type} at price {Price}",
                contractId,
                update.Type,
                update.Price);
            OrderBookUpdateReceived?.Invoke(this, update);
        }
    }

    /// <summary>
    /// Applies the hub <paramref name="contractId"/> to each trade and raises
    /// <see cref="TradeUpdateReceived"/>. Visible to unit tests (gh#86).
    /// </summary>
    internal void HandleGatewayTrade(string contractId, TradeUpdate[]? updates)
    {
        foreach (var update in updates ?? [])
        {
            if (update is null)
            {
                continue;
            }

            update.ContractId = contractId;
            _logger.LogTrace(
                "Received trade update for contract {ContractId} symbol {SymbolId}",
                contractId,
                update.SymbolId);
            TradeUpdateReceived?.Invoke(this, update);
        }
    }

    private void ConfigureMarketHubHandlers(IHubConnectionAdapter connection)
    {
        // Price/Quote updates - server sends (contractId, data)
        connection.On<string, PriceUpdate>("GatewayQuote", HandleGatewayQuote);

        // Order book/Depth updates - server sends (contractId, data[])
        connection.On<string, OrderBookUpdate[]>("GatewayDepth", HandleGatewayDepth);

        // Trade updates - server sends (contractId, data[])
        connection.On<string, TradeUpdate[]>("GatewayTrade", HandleGatewayTrade);

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

        connection.Reconnected += HandleMarketHubReconnectedAsync;
    }

    private void ConfigureUserHubHandlers(IHubConnectionAdapter connection)
    {
        // Account updates
        connection.On<AccountUpdate>("GatewayUserAccount", update =>
        {
            _logger.LogTrace("Received account update for account: {AccountId}", update.Id);
            AccountUpdateReceived?.Invoke(this, update);
        });

        // Order updates
        connection.On<OrderUpdate>("GatewayUserOrder", update =>
        {
            _logger.LogTrace("Received order update for order: {OrderId}, Account: {AccountId}",
                update.Id, update.AccountId);
            OrderUpdateReceived?.Invoke(this, update);
        });

        // Position updates
        connection.On<PositionUpdate>("GatewayUserPosition", update =>
        {
            _logger.LogTrace("Received position update for account: {AccountId}, Contract: {ContractId}",
                update.AccountId, update.ContractId);
            PositionUpdateReceived?.Invoke(this, update);
        });

        // Trade notifications
        connection.On<TradeNotification>("GatewayUserTrade", update =>
        {
            _logger.LogTrace("Received trade notification for account: {AccountId}, Order: {OrderId}",
                update.AccountId, update.OrderId);
            TradeNotificationReceived?.Invoke(this, update);
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

        connection.Reconnected += HandleUserHubReconnectedAsync;
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

    private async Task RestoreSubscriptionsAsync(
        IHubConnectionAdapter? hub,
        ConcurrentDictionary<HubSubscription, byte> subscriptions,
        string hubName,
        CancellationToken cancellationToken)
    {
        if (hub is null)
        {
            return;
        }

        Exception? firstFailure = null;
        foreach (var subscription in subscriptions.Keys)
        {
            var args = Args(subscription.Argument);
            try
            {
                await hub.InvokeAsync(subscription.MethodName, args, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to restore {Hub} subscription {Method} after reconnect",
                    hubName,
                    subscription.MethodName);
                RaiseMessageSendFailed(hubName, ToReportedMethodName(subscription.MethodName), args, ex);
                firstFailure ??= ex;
            }
        }

        if (firstFailure is not null)
        {
            throw firstFailure;
        }
    }

    private IReadOnlySet<string> SnapshotContracts(string methodName) =>
        _marketSubscriptions.Keys
            .Where(subscription => subscription.MethodName == methodName)
            .Select(subscription => (string)subscription.Argument!)
            .ToHashSet();

    private IReadOnlySet<int> SnapshotAccounts(string methodName) =>
        _userSubscriptions.Keys
            .Where(subscription => subscription.MethodName == methodName)
            .Select(subscription => (int)subscription.Argument!)
            .ToHashSet();

    private static void Record(ConcurrentDictionary<HubSubscription, byte> set, string methodName, object? argument) =>
        set[new HubSubscription(methodName, argument)] = 0;

    private static void Forget(ConcurrentDictionary<HubSubscription, byte> set, string methodName, object? argument) =>
        set.TryRemove(new HubSubscription(methodName, argument), out _);

    private static object?[] Args(object? argument) => argument is null ? [] : [argument];

    private static string ToReportedMethodName(string hubMethod) => hubMethod switch
    {
        SubscribeContractQuotes => "SubscribeToPrices",
        SubscribeContractMarketDepth => "SubscribeToDepth",
        SubscribeContractTrades => "SubscribeToTrades",
        SubscribeAccounts => "SubscribeAccounts",
        SubscribeOrders => "SubscribeOrders",
        SubscribePositions => "SubscribePositions",
        SubscribeTrades => "SubscribeTrades",
        _ => hubMethod
    };

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

    /// <summary>
    /// Reconnect backoff, and the switch that turns reconnection off.
    /// </summary>
    /// <remarks>
    /// Returning <see langword="null"/> tells SignalR to stop retrying, which is how
    /// <see cref="WebSocketOptions.AutoReconnect"/> is honoured. Previously
    /// <c>WithAutomaticReconnect</c> was applied unconditionally and the flag only decided whether a log line
    /// was written, so setting it <see langword="false"/> reconnected anyway (gh#69).
    /// </remarks>
    private readonly record struct HubSubscription(string MethodName, object? Argument);

    internal sealed class ReconnectPolicy : IRetryPolicy
    {
        private readonly WebSocketOptions _options;

        public ReconnectPolicy(WebSocketOptions options)
        {
            _options = options;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (!_options.AutoReconnect)
            {
                return null;
            }

            // Progressive backoff, capped (R-4.3).
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
