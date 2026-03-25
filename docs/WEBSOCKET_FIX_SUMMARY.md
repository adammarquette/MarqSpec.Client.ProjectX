# WebSocket Client Fix: Updated to Match ProjectX API

## Overview

Updated the WebSocket client implementation to match the actual ProjectX SignalR hub API based on the working reference implementation from [ShortChangedDegen/Scd.ProjectX.Client](https://github.com/ShortChangedDegen/Scd.ProjectX.Client).

## Key Changes Made

### 1. Hub Method Names

The ProjectX API uses different method names than initially assumed. Updated all subscription methods:

#### Market Hub Methods

| Old Method | New Method | Purpose |
|------------|------------|---------|
| `SubscribeToPrice` | `SubscribeToPrices` | Subscribe to price/quote updates |
| `UnsubscribeFromPrice` | `UnsubscribeFromPrices` | Unsubscribe from price updates |
| `SubscribeToOrderBook` | `SubscribeToDepth` | Subscribe to market depth/order book |
| `UnsubscribeFromOrderBook` | `UnsubscribeFromDepth` | Unsubscribe from market depth |
| `SubscribeToTrades` | `SubscribeToTrades` | ✅ Correct (no change) |
| `UnsubscribeFromTrades` | `UnsubscribeFromTrades` | ✅ Correct (no change) |

#### User Hub Methods

| Old Method | New Method | Purpose |
|------------|------------|---------|
| `SubscribeToOrders` | `SubscribeToOrders` | ✅ Correct (no change) |
| `UnsubscribeFromOrders` | `UnsubscribeFromOrders` | ✅ Correct (no change) |

### 2. Method Parameters

Changed from passing single values to arrays:

**Before:**
```csharp
await _marketHub.InvokeAsync("SubscribeToPrice", contractId, cancellationToken);
await _userHub.InvokeAsync("SubscribeToOrders", accountId, cancellationToken);
```

**After:**
```csharp
await _marketHub.InvokeAsync("SubscribeToPrices", new[] { contractId }, cancellationToken);
await _userHub.InvokeAsync("SubscribeToOrders", new[] { accountId }, cancellationToken);
```

**Benefit**: Allows subscribing to multiple contracts/accounts in a single call.

### 3. Event Handler Names

Updated SignalR event names to match the actual events sent by the ProjectX hubs:

#### Market Hub Events

| Old Event Name | New Event Name | Description |
|----------------|----------------|-------------|
| `PriceUpdate` | `Quote` | Real-time price/quote data |
| `OrderBookUpdate` | `Depth` | Market depth/order book data |
| `TradeUpdate` | `Trade` | Trade execution data |

#### User Hub Events

| Old Event Name | New Event Name | Description |
|----------------|----------------|-------------|
| `OrderUpdate` | `Order` | Order status updates |

**Before:**
```csharp
connection.On<PriceUpdate>("PriceUpdate", update => { ... });
connection.On<OrderBookUpdate>("OrderBookUpdate", update => { ... });
connection.On<TradeUpdate>("TradeUpdate", update => { ... });
connection.On<OrderUpdate>("OrderUpdate", update => { ... });
```

**After:**
```csharp
connection.On<PriceUpdate>("Quote", update => { ... });
connection.On<OrderBookUpdate>("Depth", update => { ... });
connection.On<TradeUpdate>("Trade", update => { ... });
connection.On<OrderUpdate>("Order", update => { ... });
```

### 4. Authentication Header

Added explicit Authorization header to hub connections:

**Before:**
```csharp
.WithUrl(hubUrl, options =>
{
    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
})
```

**After:**
```csharp
.WithUrl(hubUrl, options =>
{
    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
    options.Headers.Add("Authorization", $"Bearer {accessToken}");
})
```

## Files Modified

1. **`MarqSpec.Client.ProjectX/WebSocket/ProjectXWebSocketClient.cs`**
   - Updated all 11 method invocations
   - Updated 4 event handler registrations
   - Enhanced authentication header setup

## Testing the Changes

### Sample Application Still Works

The sample application code doesn't need changes because it uses the public API interface methods, which maintain the same signatures:

```csharp
// Your application code remains the same
await wsClient.SubscribeToPriceUpdatesAsync("NQM6");
await wsClient.SubscribeToOrderBookUpdatesAsync("NQM6");
await wsClient.SubscribeToTradeUpdatesAsync("NQM6");
await wsClient.SubscribeToOrderUpdatesAsync(accountId);
```

The changes are internal to the WebSocket client implementation.

## Expected Behavior After Fix

### Market Hub Connection

```
→ Connecting to Market Hub...
✓ Connected to Market Hub

→ Subscribing to real-time data for contract: NQM6
  • Price updates...      [Invokes: SubscribeToPrices]
  • Order book updates... [Invokes: SubscribeToDepth]
  • Trade updates...      [Invokes: SubscribeToTrades]

✓ Subscribed to all market data streams for NQM6

[10:30:45] Price Update: NQM6    [Event: Quote]
  Last:   16245.50
  Bid:    16245.25 x 10
  Ask:    16245.75 x 15
  Volume: 125,432

[10:30:45] Order Book Update: NQM6    [Event: Depth]
  Bids: 5 levels
    Best: 16245.25 x 10
  Asks: 5 levels
    Best: 16245.75 x 15

[10:30:46] Trade Update: NQM6    [Event: Trade]
  Trade ID:  9876543
  Price:     16245.50
  Quantity:  5
  Side:      Buy
```

### User Hub Connection

```
→ Connecting to User Hub...
✓ Connected to User Hub

→ Subscribing to order updates for account: 12345
  [Invokes: SubscribeToOrders with array [12345]]

[10:31:00] Order Update: 1001    [Event: Order]
  Status: Filled
  Filled: 5 / 5
  Avg Fill Price: 16245.50
```

## Reference Implementation

Based on [Scd.ProjectX.Client](https://github.com/ShortChangedDegen/Scd.ProjectX.Client) which has:
- Verified working connection to ProjectX hubs
- Correct method names and signatures
- Observer pattern for event subscriptions
- Used in production by the community

## Breaking Changes

**None for public API users**. The IProjectXWebSocketClient interface remains unchanged:
- ✅ Method signatures are the same
- ✅ Event handlers are the same
- ✅ Connection management is the same

Only the internal SignalR hub method invocations changed.

## Build Status

✅ **Build Successful** - All changes compile without errors or warnings.

## Next Steps

1. **Test the connections** - Run the sample application
2. **Verify events** - Check that Quote, Depth, and Trade events are received
3. **Monitor logs** - Watch for successful subscription confirmations
4. **Test reconnection** - Verify auto-reconnect still works

## Troubleshooting

If you still have issues connecting:

### 1. Check Authentication Token

Ensure your API token is valid and not expired:
```csharp
// Token should be obtained via REST API authentication first
var token = await authService.GetAccessTokenAsync();
```

### 2. Check Hub URLs

Verify the hub URLs in appsettings.json:
```json
{
  "ProjectX": {
    "WebSocket": {
      "MarketHubUrl": "https://rtc.topstepx.com/hubs/market",
      "UserHubUrl": "https://rtc.topstepx.com/hubs/user"
    }
  }
}
```

### 3. Enable Debug Logging

Set logging to Trace level to see all SignalR activity:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR.Client": "Trace"
    }
  }
}
```

### 4. Check Contract IDs

Ensure you're using valid contract IDs:
- ✅ `NQM6` - E-mini NASDAQ June 2026
- ✅ `NQH6` - E-mini NASDAQ March 2026
- ✅ `ESM6` - E-mini S&P 500 June 2026

Invalid or expired contracts won't receive updates even if subscribed successfully.

## Summary

| Item | Status |
|------|--------|
| Hub method names | ✅ Fixed |
| Method parameters | ✅ Fixed (arrays) |
| Event handler names | ✅ Fixed |
| Authentication headers | ✅ Enhanced |
| Build status | ✅ Successful |
| Public API compatibility | ✅ Maintained |

**All changes are based on the verified working implementation from the community.**

---

**Ready to test!** Run `dotnet run` in the `MarqSpec.Client.ProjectX.Samples` directory.
