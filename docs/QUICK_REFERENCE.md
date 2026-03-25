# ProjectX API Client - Quick Reference

## Installation

```bash
# Add project reference in your solution
dotnet add reference ../MarqSpec.Client.ProjectX/MarqSpec.Client.ProjectX.csproj
```

## Configuration

### Environment Variables (Production - Recommended)
```bash
# Windows PowerShell
$env:PROJECTX_API_KEY = "your-api-key"
$env:PROJECTX_API_SECRET = "your-api-secret"

# Linux/Mac
export PROJECTX_API_KEY="your-api-key"
export PROJECTX_API_SECRET="your-api-secret"
```

### appsettings.json (Development)
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

## Setup in Program.cs

```csharp
using MarqSpec.Client.ProjectX.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add ProjectX API Client
builder.Services.AddProjectXApiClient(builder.Configuration);

// Optional: Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();
```

## Basic Usage Examples

### 1. Get Current Price
```csharp
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.Exceptions;

public class MarketDataService
{
    private readonly IProjectXApiClient _client;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        IProjectXApiClient client,
        ILogger<MarketDataService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<decimal> GetLastPriceAsync(string symbol)
    {
        try
        {
            var price = await _client.GetCurrentPriceAsync(symbol);
            return price.Last;
        }
        catch (ProjectXApiException ex)
        {
            _logger.LogError(ex, "Failed to get price for {Symbol}", symbol);
            throw;
        }
    }
}
```

### 2. Get Order Book
```csharp
public async Task DisplayOrderBookAsync(string symbol, int depth = 10)
{
    try
    {
        var orderBook = await _client.GetOrderBookAsync(symbol, depth);
        
        Console.WriteLine($"Order Book for {orderBook.Symbol} at {orderBook.Timestamp}");
        Console.WriteLine("\nBids:");
        foreach (var bid in orderBook.Bids.Take(5))
        {
            Console.WriteLine($"  {bid.Price:N2} x {bid.Quantity}");
        }
        
        Console.WriteLine("\nAsks:");
        foreach (var ask in orderBook.Asks.Take(5))
        {
            Console.WriteLine($"  {ask.Price:N2} x {ask.Quantity}");
        }
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Invalid parameters");
    }
    catch (ProjectXApiException ex)
    {
        _logger.LogError(ex, "API error");
    }
}
```

### 3. Get Recent Trades
```csharp
public async Task<IEnumerable<decimal>> GetRecentPricesAsync(string symbol, int count = 50)
{
    var trades = await _client.GetRecentTradesAsync(symbol, count);
    return trades.Select(t => t.Price).ToList();
}
```

### 4. With Cancellation Token
```csharp
public async Task GetMarketDataWithTimeoutAsync(string symbol)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    
    try
    {
        var price = await _client.GetCurrentPriceAsync(symbol, cts.Token);
        Console.WriteLine($"Price: {price.Last}");
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Request timed out after 5 seconds");
    }
}
```

## Error Handling Patterns

### Pattern 1: Specific Error Handling
```csharp
try
{
    var price = await _client.GetCurrentPriceAsync("ES");
}
catch (AuthenticationException ex)
{
    // Handle authentication errors
    _logger.LogError(ex, "Authentication failed. Check API credentials.");
}
catch (ProjectXApiException ex) when (ex.StatusCode == 404)
{
    // Handle not found errors
    _logger.LogWarning("Symbol not found");
}
catch (ProjectXApiException ex) when (ex.StatusCode == 429)
{
    // Rate limit exceeded (should be rare due to automatic retry)
    _logger.LogWarning("Rate limit exceeded");
}
catch (ProjectXApiException ex)
{
    // Handle other API errors
    _logger.LogError(ex, "API error occurred: {StatusCode}", ex.StatusCode);
}
```

### Pattern 2: Retry Pattern (Manual)
```csharp
public async Task<Price?> GetPriceWithManualRetryAsync(string symbol, int maxRetries = 3)
{
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await _client.GetCurrentPriceAsync(symbol);
        }
        catch (ProjectXApiException ex) when (attempt < maxRetries)
        {
            _logger.LogWarning("Attempt {Attempt} failed, retrying...", attempt + 1);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    return null;
}
```

## Model Reference

### Price Model
```csharp
public class Price
{
    public string Symbol { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Last { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### OrderBook Model
```csharp
public class OrderBook
{
    public string Symbol { get; set; }
    public IEnumerable<OrderBookLevel> Bids { get; set; }
    public IEnumerable<OrderBookLevel> Asks { get; set; }
    public DateTime Timestamp { get; set; }
}

public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}
```

### Trade Model
```csharp
public class Trade
{
    public string TradeId { get; set; }
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Side { get; set; }  // "Buy" or "Sell"
    public DateTime Timestamp { get; set; }
}
```

## Testing

### Unit Test Example
```csharp
using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;

public class MyServiceTests
{
    [Fact]
    public async Task GetPrice_ReturnsValidPrice()
    {
        // Arrange
        var mockRestApi = A.Fake<IProjectXRestApi>();
        var mockAuth = A.Fake<IAuthenticationService>();
        var mockLogger = A.Fake<ILogger<ProjectXApiClient>>();
        
        var expectedPrice = new Price { Symbol = "ES", Last = 4500.25m };
        A.CallTo(() => mockRestApi.GetPriceAsync("ES", A<CancellationToken>._))
            .Returns(expectedPrice);
        
        var client = new ProjectXApiClient(mockRestApi, mockAuth, mockLogger);
        
        // Act
        var result = await client.GetCurrentPriceAsync("ES");
        
        // Assert
        result.Should().BeEquivalentTo(expectedPrice);
    }
}
```

## Common Issues

### Issue: "API key is required"
**Solution**: Set PROJECTX_API_KEY environment variable or add to appsettings.json

### Issue: Authentication fails
**Solution**: 
1. Verify API key and secret are correct
2. Check network connectivity
3. Ensure API endpoint is accessible

### Issue: Rate limiting errors
**Solution**: The client automatically retries with exponential backoff. If you still see errors, reduce request frequency.

## Performance Tips

1. **Reuse IProjectXApiClient**: Register as scoped or singleton in DI
2. **Use CancellationTokens**: Always pass cancellation tokens for long-running operations
3. **Batch Requests**: When querying multiple symbols, use Task.WhenAll
   ```csharp
   var symbols = new[] { "ES", "NQ", "RTY" };
   var tasks = symbols.Select(s => _client.GetCurrentPriceAsync(s));
   var prices = await Task.WhenAll(tasks);
   ```
4. **Monitor Logs**: Enable logging to track performance and errors

## Additional Resources

- Project Repository: [Internal]
- API Documentation: https://api.topstepx.com/swagger/v1/swagger.json
- Support: Contact development team
