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

public class TradingService
{
    private readonly IProjectXApiClient _apiClient;
    private readonly ILogger<TradingService> _logger;

    public TradingService(IProjectXApiClient apiClient, ILogger<TradingService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task GetMarketDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current price
            var price = await _apiClient.GetCurrentPriceAsync(symbol, cancellationToken);
            _logger.LogInformation("Current price for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}",
                symbol, price.Bid, price.Ask, price.Last);

            // Get order book
            var orderBook = await _apiClient.GetOrderBookAsync(symbol, depth: 10, cancellationToken);
            _logger.LogInformation("Order book for {Symbol}: {BidCount} bids, {AskCount} asks",
                symbol, orderBook.Bids.Count(), orderBook.Asks.Count());

            // Get recent trades
            var trades = await _apiClient.GetRecentTradesAsync(symbol, limit: 50, cancellationToken);
            _logger.LogInformation("Retrieved {Count} recent trades for {Symbol}",
                trades.Count(), symbol);
        }
        catch (ProjectXApiException ex)
        {
            _logger.LogError(ex, "API error occurred: {Message}", ex.Message);
            if (ex.StatusCode.HasValue)
            {
                _logger.LogError("Status code: {StatusCode}", ex.StatusCode);
            }
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
        }
    }
}
```

## API Reference

### IProjectXApiClient

#### GetCurrentPriceAsync
Gets the current price information for a symbol.

```csharp
Task<Price> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default);
```

**Returns**: `Price` object containing Bid, Ask, Last, Volume, and Timestamp

#### GetOrderBookAsync
Gets the order book for a symbol with specified depth.

```csharp
Task<OrderBook> GetOrderBookAsync(string symbol, int depth = 10, CancellationToken cancellationToken = default);
```

**Parameters**:
- `symbol`: The trading symbol
- `depth`: Number of price levels to retrieve (default: 10)

**Returns**: `OrderBook` object containing Bids, Asks, and Timestamp

#### GetRecentTradesAsync
Gets recent trades for a symbol.

```csharp
Task<IEnumerable<Trade>> GetRecentTradesAsync(string symbol, int limit = 100, CancellationToken cancellationToken = default);
```

**Parameters**:
- `symbol`: The trading symbol
- `limit`: Maximum number of trades to retrieve (default: 100)

**Returns**: Collection of `Trade` objects

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

## Requirements

- .NET 10.0 or later
- Valid ProjectX API credentials

## License

Proprietary - Marquette Speculations

## Support

For issues and questions, please contact the development team.
