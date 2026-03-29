# ProjectX API Client

A .NET client library for the ProjectX REST API, providing easy access to market data and trading operations.

## Features

✅ **User Story 1: Authentication**
- API key and secret authentication via environment variables or configuration files
- Automatic JWT token management with refresh
- Secure credential handling (never logged or exposed)
- Clear error messages for authentication failures

✅ **User Story 2: Market Data Queries**
- Get current prices for symbols
- Retrieve order book depth
- Query recent trades
- Proper error handling with meaningful messages
- All responses deserialized into strongly-typed C# models

✅ **User Story 3: Order Management**
- Place, modify, and cancel orders
- Query open orders and order history
- Manage positions (close, partial close)
- Query trade executions
- Bracket orders with stop-loss and take-profit

✅ **User Story 4: Real-Time Streaming Data**
- WebSocket streaming via two SignalR hubs (Market Hub + User Hub)
- Subscribe to real-time price updates, order book depth, and trade executions
- Subscribe to real-time order status updates per account
- Automatic reconnection with exponential backoff (1s initial, 5s max)
- Connection status monitoring via events
- Thread-safe, high-throughput (1000+ events/second)

## Installation

Add the package reference to your project:

```bash
dotnet add package MarqSpec.Client.ProjectX
```

## Configuration

### Option 1: Environment Variables (Recommended for Production)

Set the following environment variables:

```bash
PROJECTX_API_KEY=your-api-key
PROJECTX_API_SECRET=your-api-secret
```

### Option 2: appsettings.json

```json
{
  "ProjectX": {
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "BaseUrl": "https://api.topstepx.com",
    "RetryOptions": {
      "MaxRetries": 3,
      "InitialDelay": "00:00:01",
      "MaxDelay": "00:00:30"
    }
  }
}
```

> **Note**: Environment variables take precedence over appsettings.json

## Quick Start

### 1. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using MarqSpec.Client.ProjectX.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add ProjectX API client
builder.Services.AddProjectXApiClient(builder.Configuration);

var app = builder.Build();
```

### 2. Use the Client

```csharp
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.Api.Models;

public class TradingService
{
    private readonly IProjectXApiClient _apiClient;
    private readonly ILogger<TradingService> _logger;

    public TradingService(IProjectXApiClient apiClient, ILogger<TradingService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get trading accounts
            var accounts = await _apiClient.GetAccountsAsync(onlyActiveAccounts: true, cancellationToken);
            var account = accounts.First();
            _logger.LogInformation("Account {Id}: {Name} (Balance: {Balance})",
                account.Id, account.Name, account.Balance);

            // Search for live contracts
            var contracts = await _apiClient.SearchContractsAsync("NQ", live: true, cancellationToken);
            var contract = contracts.First();
            _logger.LogInformation("Contract {Id}: {Name} (Tick: {Size}/{Value})",
                contract.Id, contract.Name, contract.TickSize, contract.TickValue);

            // Place a limit order
            var orderResponse = await _apiClient.PlaceOrderAsync(new PlaceOrderRequest
            {
                AccountId = account.Id,
                ContractId = contract.Id,
                Type = OrderType.Limit,
                Side = OrderSide.Bid,
                Size = 1,
                LimitPrice = 18000m
            }, cancellationToken);
            _logger.LogInformation("Order placed: {OrderId}", orderResponse.OrderId);

            // Query open orders
            var openOrders = await _apiClient.GetOpenOrdersAsync(account.Id, cancellationToken);
            _logger.LogInformation("Open orders: {Count}", openOrders.Count());

            // Query open positions
            var positions = await _apiClient.GetOpenPositionsAsync(account.Id, cancellationToken);
            _logger.LogInformation("Open positions: {Count}", positions.Count());
        }
        catch (ProjectXApiException ex)
        {
            _logger.LogError(ex, "API error: {Message}", ex.Message);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
        }
    }
}
```

## Real-Time Streaming

The client provides real-time market data and order updates via two SignalR WebSocket hubs:

- **Market Hub** — price quotes, order book depth, and trade executions
- **User Hub** — order status updates for your accounts

Updates are delivered through C# events using the Observer pattern.

### Streaming Market Data

```csharp
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.WebSocket;

public class MarketDataService : IAsyncDisposable
{
    private readonly IProjectXWebSocketClient _wsClient;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(IProjectXWebSocketClient wsClient, ILogger<MarketDataService> logger)
    {
        _wsClient = wsClient;
        _logger = logger;
    }

    public async Task StartStreamingAsync(string contractId, CancellationToken cancellationToken = default)
    {
        // Monitor connection status changes
        _wsClient.ConnectionStatusChanged += (sender, change) =>
        {
            _logger.LogInformation("Market Hub: {Previous} → {Current}",
                change.PreviousState, change.CurrentState);

            if (change.ErrorMessage is not null)
                _logger.LogWarning("Connection error: {Error}", change.ErrorMessage);
        };

        // Register event handlers
        _wsClient.PriceUpdateReceived += (sender, update) =>
        {
            _logger.LogInformation("{Contract} Last={Last} Bid={Bid}x{BidSz} Ask={Ask}x{AskSz} Vol={Vol}",
                update.ContractId, update.LastPrice,
                update.BidPrice, update.BidSize,
                update.AskPrice, update.AskSize,
                update.Volume);
        };

        _wsClient.OrderBookUpdateReceived += (sender, update) =>
        {
            _logger.LogInformation("{Contract} Book: {Bids} bids, {Asks} asks (seq {Seq})",
                update.ContractId, update.Bids.Count, update.Asks.Count, update.SequenceNumber);
        };

        _wsClient.TradeUpdateReceived += (sender, update) =>
        {
            _logger.LogInformation("{Contract} Trade: {Price} x {Qty} ({Side})",
                update.ContractId, update.Price, update.Quantity, update.Side);
        };

        // Connect and subscribe
        await _wsClient.ConnectMarketHubAsync(cancellationToken);
        await _wsClient.SubscribeToPriceUpdatesAsync(contractId, cancellationToken);
        await _wsClient.SubscribeToOrderBookUpdatesAsync(contractId, cancellationToken);
        await _wsClient.SubscribeToTradeUpdatesAsync(contractId, cancellationToken);
    }

    public async Task StopStreamingAsync(string contractId, CancellationToken cancellationToken = default)
    {
        await _wsClient.UnsubscribeFromPriceUpdatesAsync(contractId, cancellationToken);
        await _wsClient.UnsubscribeFromOrderBookUpdatesAsync(contractId, cancellationToken);
        await _wsClient.UnsubscribeFromTradeUpdatesAsync(contractId, cancellationToken);
        await _wsClient.DisconnectMarketHubAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _wsClient.DisposeAsync();
    }
}
```

### Streaming Order Updates

```csharp
// Connect to the User Hub for order status updates
await wsClient.ConnectUserHubAsync(cancellationToken);

wsClient.OrderUpdateReceived += (sender, update) =>
{
    logger.LogInformation("Order {Id} on account {Acct}: {Status} — {Side} {Size} @ {Price}",
        update.OrderId, update.AccountId, update.Status,
        update.Side, update.Size, update.AverageFillPrice);

    if (update.RejectionReason is not null)
        logger.LogWarning("Rejected: {Reason}", update.RejectionReason);
};

await wsClient.SubscribeToOrderUpdatesAsync(accountId, cancellationToken);

// Later: clean up
await wsClient.UnsubscribeFromOrderUpdatesAsync(accountId, cancellationToken);
await wsClient.DisconnectUserHubAsync(cancellationToken);
```

### Connection States

The `ConnectionState` enum tracks the lifecycle of each hub connection:

| State | Description |
|-------|-------------|
| `Disconnected` | Not connected |
| `Connecting` | Connection attempt in progress |
| `Connected` | Active and receiving data |
| `Reconnecting` | Automatically reconnecting after a disconnection |
| `Failed` | Connection failed (check `ErrorMessage` on the event) |

Monitor state transitions via the `ConnectionStatusChanged` event, which provides a `ConnectionStatusChange` object containing `PreviousState`, `CurrentState`, `Timestamp`, `ErrorMessage`, and `Exception`.

### Auto-Reconnection

Automatic reconnection is enabled by default. When a connection drops, the client uses exponential backoff starting at 1 second up to a maximum of 5 seconds. During reconnection, the `ConnectionStatusChanged` event fires with `ConnectionState.Reconnecting`. Configure reconnection behavior in `appsettings.json`:

```json
{
  "ProjectX": {
    "WebSocket": {
      "AutoReconnect": true,
      "InitialReconnectDelaySeconds": 1,
      "MaxReconnectDelaySeconds": 5
    }
  }
}
```

## API Reference

### IProjectXApiClient

#### Accounts

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetAccountsAsync` | `bool onlyActiveAccounts = true` | `IEnumerable<TradingAccount>` | Get trading accounts |

#### Contracts

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SearchContractsAsync` | `string? searchText, bool live = true` | `IEnumerable<Contract>` | Search contracts by text |
| `GetContractAsync` | `string contractId, bool live = true` | `Contract?` | Get a contract by ID |
| `GetContractByIdAsync` | `string contractId` | `Contract?` | Direct contract lookup by ID |
| `GetAvailableContractsAsync` | `bool live = true` | `IEnumerable<Contract>` | List all available contracts |

#### Historical Data

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetHistoricalBarsAsync` | `string contractId, DateTime startTime, DateTime endTime, AggregateBarUnit unit, int unitNumber = 1, int limit = 1000, bool live = true, bool includePartialBar = false` | `IEnumerable<AggregateBar>` | Retrieve historical OHLCV bars |

#### Orders

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `PlaceOrderAsync` | `PlaceOrderRequest request` | `PlaceOrderResponse` | Place a new order |
| `ModifyOrderAsync` | `ModifyOrderRequest request` | `ModifyOrderResponse` | Modify an existing order |
| `CancelOrderAsync` | `int accountId, long orderId` | `CancelOrderResponse` | Cancel an existing order |
| `GetOrderAsync` | `int accountId, long orderId` | `Order?` | Get a specific order |
| `GetOrdersAsync` | `int accountId, DateTime? startTime, DateTime? endTime` | `IEnumerable<Order>` | Get orders in a time range |
| `GetOpenOrdersAsync` | `int accountId` | `IEnumerable<Order>` | Get all open/working orders |

#### Positions

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetOpenPositionsAsync` | `int accountId` | `IEnumerable<Position>` | Get all open positions |
| `ClosePositionAsync` | `int accountId, string contractId` | `ClosePositionResponse` | Close a full position |
| `PartialClosePositionAsync` | `int accountId, string contractId, int size` | `PartialClosePositionResponse` | Partially close a position |

#### Trades

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetTradesAsync` | `int accountId, DateTime? startTime, DateTime? endTime` | `IEnumerable<HalfTrade>` | Get trade executions |

#### Utility

| Method | Returns | Description |
|--------|---------|-------------|
| `PingAsync` | `bool` | Check if the API is responsive |

> **Note**: All methods accept an optional `CancellationToken` parameter (omitted from tables for brevity).

### IProjectXWebSocketClient

#### Connection Management

| Method | Description |
|--------|-------------|
| `ConnectMarketHubAsync(CancellationToken)` | Connect to the market data hub |
| `DisconnectMarketHubAsync(CancellationToken)` | Disconnect from the market data hub |
| `ConnectUserHubAsync(CancellationToken)` | Connect to the user data hub |
| `DisconnectUserHubAsync(CancellationToken)` | Disconnect from the user data hub |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MarketHubState` | `ConnectionState` | Current connection state of the market hub |
| `UserHubState` | `ConnectionState` | Current connection state of the user hub |

#### Market Data Subscriptions

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToPriceUpdatesAsync` | `string contractId, CancellationToken` | Subscribe to real-time price quotes |
| `UnsubscribeFromPriceUpdatesAsync` | `string contractId, CancellationToken` | Unsubscribe from price quotes |
| `SubscribeToOrderBookUpdatesAsync` | `string contractId, CancellationToken` | Subscribe to order book depth updates |
| `UnsubscribeFromOrderBookUpdatesAsync` | `string contractId, CancellationToken` | Unsubscribe from order book updates |
| `SubscribeToTradeUpdatesAsync` | `string contractId, CancellationToken` | Subscribe to trade execution updates |
| `UnsubscribeFromTradeUpdatesAsync` | `string contractId, CancellationToken` | Unsubscribe from trade updates |

#### User Data Subscriptions

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToOrderUpdatesAsync` | `int accountId, CancellationToken` | Subscribe to order status updates |
| `UnsubscribeFromOrderUpdatesAsync` | `int accountId, CancellationToken` | Unsubscribe from order updates |

#### Events

| Event | Payload | Description |
|-------|---------|-------------|
| `ConnectionStatusChanged` | `ConnectionStatusChange` | Fires on any connection state transition |
| `PriceUpdateReceived` | `PriceUpdate` | Fires on each price quote |
| `OrderBookUpdateReceived` | `OrderBookUpdate` | Fires on each order book snapshot |
| `TradeUpdateReceived` | `TradeUpdate` | Fires on each trade execution |
| `OrderUpdateReceived` | `OrderUpdate` | Fires on each order status change |

#### PriceUpdate

| Property | Type | Description |
|----------|------|-------------|
| `ContractId` | `string` | Contract identifier |
| `LastPrice` | `decimal` | Last traded price |
| `BidPrice` | `decimal` | Best bid price |
| `AskPrice` | `decimal` | Best ask price |
| `BidSize` | `int` | Bid quantity |
| `AskSize` | `int` | Ask quantity |
| `Volume` | `long` | Total volume traded |
| `Timestamp` | `DateTime` | Update timestamp |
| `OpenPrice` | `decimal?` | Opening price |
| `HighPrice` | `decimal?` | High price |
| `LowPrice` | `decimal?` | Low price |
| `PreviousClose` | `decimal?` | Previous session close |

#### OrderBookUpdate

| Property | Type | Description |
|----------|------|-------------|
| `ContractId` | `string` | Contract identifier |
| `Bids` | `List<OrderBookLevel>` | Bid price levels (price + quantity) |
| `Asks` | `List<OrderBookLevel>` | Ask price levels (price + quantity) |
| `Timestamp` | `DateTime` | Update timestamp |
| `SequenceNumber` | `long` | Sequence number for ordering |

#### TradeUpdate

| Property | Type | Description |
|----------|------|-------------|
| `ContractId` | `string` | Contract identifier |
| `TradeId` | `long` | Unique trade identifier |
| `Price` | `decimal` | Trade price |
| `Quantity` | `int` | Trade quantity |
| `Side` | `TradeSide` | `Buy` or `Sell` |
| `Timestamp` | `DateTime` | Trade timestamp |
| `IsAggressive` | `bool` | Whether the trade was aggressive (taker) |

#### OrderUpdate

| Property | Type | Description |
|----------|------|-------------|
| `OrderId` | `long` | Order identifier |
| `AccountId` | `int` | Account identifier |
| `ContractId` | `string` | Contract identifier |
| `Status` | `OrderStatus` | Current order status |
| `Type` | `OrderType` | Order type (Market, Limit, Stop, etc.) |
| `Side` | `OrderSide` | Order side (Bid/Ask) |
| `Size` | `int` | Total order size |
| `FilledQuantity` | `int` | Quantity filled so far |
| `RemainingQuantity` | `int` | Quantity remaining |
| `LimitPrice` | `decimal?` | Limit price (if applicable) |
| `StopPrice` | `decimal?` | Stop price (if applicable) |
| `AverageFillPrice` | `decimal?` | Average fill price |
| `Timestamp` | `DateTime` | Update timestamp |
| `Message` | `string?` | Update reason or message |
| `RejectionReason` | `string?` | Rejection reason (if rejected) |

#### ConnectionStatusChange

| Property | Type | Description |
|----------|------|-------------|
| `PreviousState` | `ConnectionState` | Connection state before the change |
| `CurrentState` | `ConnectionState` | Connection state after the change |
| `Timestamp` | `DateTime` | When the state change occurred |
| `ErrorMessage` | `string?` | Error description (if error-related) |
| `Exception` | `Exception?` | Exception that caused the change (if any) |

#### OrderBookLevel

| Property | Type | Description |
|----------|------|-------------|
| `Price` | `decimal` | Price level |
| `Quantity` | `decimal` | Quantity at this price level |
| `OrderCount` | `int` | Number of orders at this level |

### Enums

#### ConnectionState

| Value | Description |
|-------|-------------|
| `Disconnected` | Not connected |
| `Connecting` | Connection attempt in progress |
| `Connected` | Active and receiving data |
| `Reconnecting` | Automatically reconnecting after a disconnection |
| `Failed` | Connection failed |

#### OrderStatus

| Value | Description |
|-------|-------------|
| `Unknown` (0) | Unknown status |
| `Accepted` (1) | Order accepted by the system |
| `Pending` (2) | Order pending execution |
| `Triggered` (3) | Stop order has been triggered |
| `PartiallyFilled` (4) | Order partially filled |
| `Filled` (5) | Order completely filled |
| `Cancelled` (6) | Order cancelled |
| `Rejected` (7) | Order rejected |
| `Expired` (8) | Order expired |

#### OrderType

| Value | Description |
|-------|-------------|
| `Unknown` (0) | Unknown order type |
| `Limit` (1) | Executes at a specific price or better |
| `Market` (2) | Executes immediately at the best available price |
| `StopLimit` (3) | Becomes a limit order when the stop price is reached |
| `Stop` (4) | Becomes a market order when the stop price is reached |
| `TrailingStop` (5) | Stop price trails the market by a specified amount |
| `JoinBid` (6) | Automatically adjusts to join the best bid |
| `JoinAsk` (7) | Automatically adjusts to join the best ask |

#### OrderSide

| Value | Description |
|-------|-------------|
| `Bid` (0) | Buy order |
| `Ask` (1) | Sell order |

#### TradeSide

| Value | Description |
|-------|-------------|
| `Buy` | Buy side trade |
| `Sell` | Sell side trade |

#### PositionType

| Value | Description |
|-------|-------------|
| `Undefined` (0) | Position direction is undefined |
| `Long` (1) | Long (buy) position |
| `Short` (2) | Short (sell) position |

#### AggregateBarUnit

| Value | Description |
|-------|-------------|
| `Unspecified` (0) | Unspecified unit |
| `Second` (1) | Second-based aggregation |
| `Minute` (2) | Minute-based aggregation |
| `Hour` (3) | Hour-based aggregation |
| `Day` (4) | Day-based aggregation |
| `Week` (5) | Week-based aggregation |
| `Month` (6) | Month-based aggregation |

### REST API Models

#### TradingAccount

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Unique account identifier |
| `Name` | `string` | Account name |
| `Balance` | `decimal` | Current account balance |
| `CanTrade` | `bool` | Whether this account is allowed to trade |
| `IsVisible` | `bool` | Whether this account is visible |
| `Simulated` | `bool` | Whether this is a simulated account |

#### Contract

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique contract identifier |
| `Name` | `string` | Contract name |
| `Description` | `string` | Contract description |
| `TickSize` | `decimal` | Minimum price increment |
| `TickValue` | `decimal` | Monetary value of one tick |
| `ActiveContract` | `bool` | Whether this is an active contract |
| `SymbolId` | `string` | Symbol identifier |

#### Order

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Unique order identifier |
| `AccountId` | `int` | Account that owns this order |
| `ContractId` | `string` | Contract identifier |
| `SymbolId` | `string` | Symbol identifier |
| `CreationTimestamp` | `DateTime` | When the order was created |
| `UpdateTimestamp` | `DateTime?` | When the order was last updated |
| `Status` | `OrderStatus` | Current order status |
| `Type` | `OrderType` | Order type |
| `Side` | `OrderSide` | Order side (Bid/Ask) |
| `Size` | `int` | Order quantity |
| `LimitPrice` | `decimal?` | Limit price (for limit orders) |
| `StopPrice` | `decimal?` | Stop price (for stop orders) |
| `FillVolume` | `int` | Number of contracts filled |
| `FilledPrice` | `decimal?` | Average fill price |
| `CustomTag` | `string?` | Custom tag for the order |

#### Position

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Unique position identifier |
| `AccountId` | `int` | Account that owns this position |
| `ContractId` | `string` | Contract identifier |
| `ContractDisplayName` | `string?` | Human-readable contract name |
| `CreationTimestamp` | `DateTime` | When this position was created |
| `Type` | `PositionType` | Position direction (Long/Short) |
| `Size` | `int` | Number of contracts |
| `AveragePrice` | `decimal` | Volume-weighted average entry price |

#### HalfTrade

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `long` | Unique trade identifier |
| `AccountId` | `int` | Account that executed this trade |
| `ContractId` | `string` | Contract identifier |
| `CreationTimestamp` | `DateTime` | When the trade was executed |
| `Price` | `decimal` | Execution price |
| `ProfitAndLoss` | `decimal?` | Realized P&L for this trade leg |
| `Fees` | `decimal` | Fees charged |
| `Side` | `OrderSide` | Trade side (Bid = buy, Ask = sell) |
| `Size` | `int` | Number of contracts traded |
| `Voided` | `bool` | Whether this trade has been voided |
| `OrderId` | `long` | ID of the order that generated this trade |

#### AggregateBar

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | Bar timestamp |
| `Open` | `decimal` | Opening price |
| `High` | `decimal` | Highest price during the period |
| `Low` | `decimal` | Lowest price during the period |
| `Close` | `decimal` | Closing price |
| `Volume` | `long` | Trading volume |

#### PlaceOrderRequest

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `AccountId` | `int` | Yes | Account identifier |
| `ContractId` | `string` | Yes | Contract to trade |
| `Type` | `OrderType` | Yes | Order type |
| `Side` | `OrderSide` | Yes | Order side |
| `Size` | `int` | Yes | Number of contracts |
| `LimitPrice` | `decimal?` | No | Limit price (required for Limit/StopLimit orders) |
| `StopPrice` | `decimal?` | No | Stop price (required for Stop/StopLimit orders) |
| `TrailPrice` | `decimal?` | No | Trail amount (for TrailingStop orders) |
| `CustomTag` | `string?` | No | Custom tag for order tracking |
| `StopLossBracket` | `PlaceOrderBracket?` | No | Stop-loss bracket configuration |
| `TakeProfitBracket` | `PlaceOrderBracket?` | No | Take-profit bracket configuration |

#### PlaceOrderBracket

| Property | Type | Description |
|----------|------|-------------|
| `Ticks` | `int` | Number of ticks from the entry price |
| `Type` | `OrderType` | Bracket order type |

## Error Handling

The client provides two exception types:

### AuthenticationException
Thrown when authentication fails. Common causes:
- Invalid API key or secret
- Network connectivity issues
- API service unavailable

### ProjectXApiException
Thrown when API requests fail. Includes:
- HTTP status code (if available)
- Detailed error message
- Original exception as inner exception

## Features

### Automatic Token Management
- Tokens are cached and automatically refreshed before expiration
- Thread-safe token acquisition
- 1-minute buffer before token expiration

### Retry Policy with Polly
- Automatic retry for transient failures (500+ errors)
- Exponential backoff strategy
- Rate limiting handling (429 responses)
- Configurable retry attempts and delays

### Logging
- Uses `ILogger<T>` from Microsoft.Extensions.Logging
- Compatible with any logging provider (Serilog, NLog, etc.)
- Structured logging with proper log levels
- Credentials are never logged

### Thread Safety
- All methods are thread-safe
- Supports concurrent API calls
- Safe for use in multi-threaded applications

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ApiKey | string | *required* | Your ProjectX API key |
| ApiSecret | string | *required* | Your ProjectX API secret |
| BaseUrl | string | https://api.topstepx.com | Base URL for REST API |
| WebSocketUserHubUrl | string | https://rtc.topstepx.com/hubs/user | User hub WebSocket URL |
| WebSocketMarketHubUrl | string | https://rtc.topstepx.com/hubs/market | Market hub WebSocket URL |
| ValidateSslCertificates | bool | true | Enable SSL certificate validation |
| RetryOptions.MaxRetries | int | 3 | Maximum retry attempts |
| RetryOptions.InitialDelay | TimeSpan | 00:00:01 | Initial delay between retries |
| RetryOptions.MaxDelay | TimeSpan | 00:00:30 | Maximum delay between retries |
| WebSocket.AutoReconnect | bool | true | Enable automatic reconnection |
| WebSocket.InitialReconnectDelaySeconds | int | 1 | Initial reconnect backoff delay |
| WebSocket.MaxReconnectDelaySeconds | int | 5 | Maximum reconnect backoff delay |
| WebSocket.HandshakeTimeoutSeconds | int | 15 | SignalR handshake timeout |
| WebSocket.KeepAliveIntervalSeconds | int | 15 | Keep-alive ping interval |
| WebSocket.ServerTimeoutSeconds | int | 30 | Server timeout before disconnect |
| WebSocket.UseMessagePack | bool | false | Use MessagePack protocol instead of JSON |
| WebSocket.MaxBufferSize | long | 1048576 | Max incoming message buffer in bytes (1 MB) |

## Requirements

- .NET 10.0 or later
- Valid ProjectX API credentials

## License

Proprietary - Marquette Speculations

## Support

For issues and questions, please contact the development team.
