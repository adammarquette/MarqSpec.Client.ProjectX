# Sample Application - Dynamic Contract Query Enhancement

## Overview
The sample application now dynamically queries for live contracts via the REST API before subscribing to WebSocket feeds, eliminating the need for hardcoded contract IDs.

## Key Features

### 1. Smart Contract Discovery
The application uses a progressive fallback strategy to find available contracts:

```csharp
// Try NQ (E-mini NASDAQ) first
contracts = await _apiClient.SearchContractsAsync("NQ", live: true);

// Fall back to ES (E-mini S&P 500) if no NQ
if (!contracts.Any())
{
    contracts = await _apiClient.SearchContractsAsync("ES", live: true);
}

// Fall back to all live contracts if needed
if (!contracts.Any())
{
    contracts = await _apiClient.SearchContractsAsync(null, live: true);
}
```

### 2. Contract Display
Shows available contracts to the user before selecting:

```
Available contracts:
  • NQH4            NQ       E-mini NASDAQ 100 Mar 2024
  • NQM4            NQ       E-mini NASDAQ 100 Jun 2024
  • NQU4            NQ       E-mini NASDAQ 100 Sep 2024
  ... and 5 more
```

### 3. Automatic Selection
Automatically selects the first available contract (typically the front month):

```csharp
var selectedContract = contracts.First();
var contractId = selectedContract.Id;

Console.WriteLine($"Selected contract: {contractId}");
Console.WriteLine($"  Symbol: {selectedContract.SymbolId}");
Console.WriteLine($"  Name:   {selectedContract.Name}");
```

### 4. Enhanced Error Handling
Provides clear error messages and troubleshooting guidance:

```
✗ ERROR: No live contracts found!

Possible causes:
  • Account may not have access to contract data
  • Outside of trading hours
  • API credentials may have limited permissions
```

## Workflow Steps

The application now follows this enhanced workflow:

1. **Load Configuration** - Read API credentials from appsettings.json or environment variables
2. **Setup Dependency Injection** - Register REST and WebSocket clients
3. **Query Live Contracts** - Search for available contracts (NQ → ES → All)
4. **Display Options** - Show user which contracts are available
5. **Auto-Select Contract** - Choose the first available contract
6. **Connect WebSocket** - Establish connection to Market Hub
7. **Subscribe to Feeds** - Subscribe to price, order book, and trade updates
8. **Stream Data** - Display real-time updates for 30 seconds
9. **Unsubscribe** - Clean up subscriptions
10. **Disconnect** - Close WebSocket connection

## Benefits

### ✅ No Hardcoded Contract IDs
- No need to manually update contract IDs when they expire
- Works automatically with currently trading contracts

### ✅ Intelligent Fallback
- Tries preferred contracts first (NQ)
- Falls back to alternatives (ES)
- Shows all available if specific ones not found

### ✅ Better User Experience
- Shows available contracts before subscribing
- Clear messages about what's happening
- Helpful error messages with troubleshooting tips

### ✅ Production Ready
- Handles API errors gracefully
- Provides context for failures
- Continues execution when possible

## Running the Sample

```bash
cd MarqSpec.Client.ProjectX.Samples
dotnet run
```

### Expected Output

```
═══════════════════════════════════════════════════════════════
  ProjectX WebSocket Client - Sample Application
═══════════════════════════════════════════════════════════════

✓ Configuration loaded
  API Key: 12345678... (masked)

═══════════════════════════════════════════════════════════════
  Demo: Real-time Market Data Streaming
═══════════════════════════════════════════════════════════════

→ Querying for live contracts...
✓ Found 12 live contract(s)

Available contracts:
  • NQH4            NQ       E-mini NASDAQ 100 Mar 2024
  • NQM4            NQ       E-mini NASDAQ 100 Jun 2024
  • NQU4            NQ       E-mini NASDAQ 100 Sep 2024
  • NQZ4            NQ       E-mini NASDAQ 100 Dec 2024
  • NQH5            NQ       E-mini NASDAQ 100 Mar 2025
  ... and 7 more

→ Selected contract for streaming: NQH4
  Symbol: NQ
  Name:   E-mini NASDAQ 100 Mar 2024
  Active: True

→ Connecting to Market Hub...
✓ Connected to Market Hub

→ Subscribing to real-time data for contract: NQH4
  • Price updates...
  • Order book updates...
  • Trade updates...

✓ Subscribed to all market data streams for NQH4

─────────────────────────────────────────────────────────────
  Receiving real-time updates (Press Ctrl+C to exit)...
─────────────────────────────────────────────────────────────

Running... 30s | Price updates: 145 | Order book: 67 | Trades: 23

─────────────────────────────────────────────────────────────
  Demo complete!
─────────────────────────────────────────────────────────────

Total updates received:
  • Price updates:      145
  • Order book updates: 67
  • Trade updates:      23

→ Unsubscribing from NQH4...
✓ Unsubscribed from all streams

→ Disconnecting...
✓ Disconnected and disposed
```

## Customization

### Subscribe to Multiple Contracts

You can easily modify the code to subscribe to multiple contracts:

```csharp
// Take the first 3 contracts instead of just 1
foreach (var contract in contracts.Take(3))
{
    await _wsClient.SubscribeToPriceUpdatesAsync(contract.Id);
    await _wsClient.SubscribeToOrderBookUpdatesAsync(contract.Id);
    await _wsClient.SubscribeToTradeUpdatesAsync(contract.Id);
}
```

### Filter by Specific Symbol

Search for a specific symbol:

```csharp
// Search for Gold futures (GC)
contracts = await _apiClient.SearchContractsAsync("GC", live: true);
```

### Change Runtime Duration

Adjust how long the sample runs:

```csharp
// Run for 60 seconds instead of 30
for (int i = 0; i < 60; i++)
{
    await Task.Delay(1000);
    // ... display stats
}
```

## Troubleshooting

### No Contracts Found
If the API returns no contracts:
- Check that your account has access to contract data
- Verify you're querying during trading hours
- Ensure API credentials have proper permissions
- Try the diagnostic tool: `cd MarqSpec.Client.ProjectX.Diagnostics && dotnet run`

### No Updates Received
If WebSocket connects but no data arrives:
- Contract may not be actively trading (check trading hours)
- Try a more active contract (ES, NQ, CL are typically very active)
- Check WebSocket logs for subscription errors

### Connection Failures
If WebSocket won't connect:
- Verify API credentials are correct
- Check network connectivity to rtc.topstepx.com
- Review logs for specific error messages

## Technical Details

### REST API Integration
The sample uses `IProjectXApiClient` to query contracts:
- Method: `SearchContractsAsync(string? search, bool? live)`
- Returns: `IEnumerable<Contract>` with Id, SymbolId, Name, etc.

### WebSocket Integration
Uses `IProjectXWebSocketClient` for real-time data:
- Connection: `ConnectMarketHubAsync()`
- Subscriptions: `SubscribeToPriceUpdatesAsync(contractId)`
- Events: `PriceUpdateReceived`, `OrderBookUpdateReceived`, `TradeUpdateReceived`

## Next Steps

1. **Test the Sample** - Run it and verify dynamic contract selection works
2. **Customize for Your Needs** - Modify to subscribe to specific symbols or multiple contracts
3. **Review Data Models** - Check `PriceUpdate`, `OrderBookUpdate`, `TradeUpdate` models for available fields
4. **Implement Your Logic** - Use the event handlers to process real-time data for your use case

## Related Files

- `Program.cs` - Main sample application
- `README.md` - Detailed usage and configuration guide
- `appsettings.json` - Configuration template
- `../MarqSpec.Client.ProjectX.Tests/appsettings.integration.json` - Test credentials (also used by sample)
