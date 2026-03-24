# Quick Reference: ProjectX WebSocket API

## Hub URLs

```
Market Hub:  https://rtc.topstepx.com/hubs/market
User Hub:    https://rtc.topstepx.com/hubs/user
```

## Market Hub Methods

### Subscribe Methods
```csharp
// Price/Quote updates
await marketHub.InvokeAsync("SubscribeToPrices", new[] { "NQM6", "ESM6" });

// Market Depth/Order Book
await marketHub.InvokeAsync("SubscribeToDepth", new[] { "NQM6" });

// Trade executions
await marketHub.InvokeAsync("SubscribeToTrades", new[] { "NQM6" });
```

### Unsubscribe Methods
```csharp
await marketHub.InvokeAsync("UnsubscribeFromPrices", new[] { "NQM6" });
await marketHub.InvokeAsync("UnsubscribeFromDepth", new[] { "NQM6" });
await marketHub.InvokeAsync("UnsubscribeFromTrades", new[] { "NQM6" });
```

### Event Handlers
```csharp
// Receive price/quote updates
marketHub.On<PriceUpdate>("Quote", update => {
    Console.WriteLine($"Quote: {update.ContractId} Last: {update.LastPrice}");
});

// Receive market depth/order book
marketHub.On<OrderBookUpdate>("Depth", update => {
    Console.WriteLine($"Depth: {update.ContractId} Bids: {update.Bids.Count}");
});

// Receive trades
marketHub.On<TradeUpdate>("Trade", update => {
    Console.WriteLine($"Trade: {update.ContractId} Price: {update.Price}");
});
```

## User Hub Methods

### Subscribe Methods
```csharp
// Order updates for specific accounts
await userHub.InvokeAsync("SubscribeToOrders", new[] { 12345, 67890 });
```

### Unsubscribe Methods
```csharp
await userHub.InvokeAsync("UnsubscribeFromOrders", new[] { 12345 });
```

### Event Handlers
```csharp
// Receive order updates
userHub.On<OrderUpdate>("Order", update => {
    Console.WriteLine($"Order: {update.OrderId} Status: {update.Status}");
});
```

## Connection Pattern

```csharp
// 1. Build connection with auth token
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options =>
    {
        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        options.Headers.Add("Authorization", $"Bearer {token}");
    })
    .WithAutomaticReconnect()
    .Build();

// 2. Register event handlers BEFORE starting
connection.On<PriceUpdate>("Quote", update => { /* handle */ });

// 3. Start the connection
await connection.StartAsync();

// 4. Subscribe to data
await connection.InvokeAsync("SubscribeToPrices", new[] { "NQM6" });

// 5. Clean up when done
await connection.StopAsync();
await connection.DisposeAsync();
```

## Event Names Summary

| Data Type | Hub Method | Event Name | Model |
|-----------|------------|------------|-------|
| Prices/Quotes | SubscribeToPrices | `Quote` | PriceUpdate |
| Market Depth | SubscribeToDepth | `Depth` | OrderBookUpdate |
| Trades | SubscribeToTrades | `Trade` | TradeUpdate |
| Orders | SubscribeToOrders | `Order` | OrderUpdate |

## Common Contract IDs (2026)

```
NQM6  - E-mini NASDAQ June 2026
NQH6  - E-mini NASDAQ March 2026
ESM6  - E-mini S&P 500 June 2026
ESH6  - E-mini S&P 500 March 2026
YMM6  - E-mini Dow June 2026
```

## Authentication

```csharp
// Get token via REST API first
var authService = serviceProvider.GetService<IAuthenticationService>();
var token = await authService.GetAccessTokenAsync();

// Use token in WebSocket connection
.WithUrl(hubUrl, options =>
{
    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
    options.Headers.Add("Authorization", $"Bearer {token}");
})
```

## Auto-Reconnection

```csharp
.WithAutomaticReconnect(new[] {
    TimeSpan.FromSeconds(0),
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(5)
})
```

## Error Handling

```csharp
connection.Closed += async (error) =>
{
    if (error != null)
    {
        Console.WriteLine($"Connection closed: {error.Message}");
    }
};

connection.Reconnecting += (error) =>
{
    Console.WriteLine("Reconnecting...");
    return Task.CompletedTask;
};

connection.Reconnected += (connectionId) =>
{
    Console.WriteLine($"Reconnected: {connectionId}");
    // Re-subscribe to data here
    return Task.CompletedTask;
};
```

## Tips

1. **Arrays not singles**: All subscribe methods take arrays, not single values
2. **Event names are short**: "Quote", "Depth", "Trade", "Order" (not "PriceUpdate", etc.)
3. **Auth token required**: Must authenticate via REST API first
4. **Register handlers first**: Set up event handlers before calling StartAsync()
5. **Re-subscribe on reconnect**: Subscriptions don't persist across reconnections

## Reference

Based on working implementation: [Scd.ProjectX.Client](https://github.com/ShortChangedDegen/Scd.ProjectX.Client)

Official docs: [ProjectX Gateway Documentation](https://gateway.docs.projectx.com/docs/realtime)
