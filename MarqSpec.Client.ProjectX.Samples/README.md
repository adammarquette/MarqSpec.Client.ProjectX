# ProjectX API Client - Sample Applications

This directory contains sample applications demonstrating how to use the ProjectX API Client for both REST API and WebSocket real-time data.

## Prerequisites

### API Credentials

You need valid ProjectX API credentials. Provide them via:

**Option 1: Environment Variables (Recommended)**
```bash
# Windows PowerShell
$env:PROJECTX_API_KEY = "your-api-key"
$env:PROJECTX_API_SECRET = "your-api-secret"

# Linux/Mac
export PROJECTX_API_KEY="your-api-key"
export PROJECTX_API_SECRET="your-api-secret"
```

**Option 2: appsettings.json**
Edit `appsettings.json`:
```json
{
  "ProjectX": {
    "ApiKey": "your-api-key-here",
    "ApiSecret": "your-api-secret-here"
  }
}
```

## Running the WebSocket Sample

### From Command Line
```bash
cd MarqSpec.Client.ProjectX.Samples
dotnet run
```

### From Visual Studio
1. Set `MarqSpec.Client.ProjectX.Samples` as startup project
2. Press F5 or Ctrl+F5

## What the Sample Demonstrates

### Dynamic Contract Discovery + WebSocket Real-time Market Data

The sample application shows:

1. **REST API Integration**
   - Querying for live contracts using the REST API
   - Searching by symbol (NQ, ES) or all contracts
   - Displaying contract information (ID, symbol, name)

2. **Smart Contract Selection**
   - Tries NQ (E-mini NASDAQ) first
   - Falls back to ES (E-mini S&P 500) if needed
   - Falls back to any live contract if specific ones not found
   - Handles cases where no contracts are available

3. **Connection Management**
   - Connecting to the Market Hub
   - Monitoring connection status changes
   - Automatic reconnection handling

4. **Market Data Subscriptions**
   - Subscribe to real-time price updates
   - Subscribe to order book depth updates
   - Subscribe to trade execution updates

5. **Event Handling**
   - Receiving real-time updates via events
   - Processing price, order book, and trade data
   - Counting and displaying statistics

6. **Error Handling**
   - Graceful handling of API errors
   - Informative error messages
   - Continued execution when possible

7. **Cleanup**
   - Unsubscribing from data streams
   - Disconnecting gracefully
   - Proper disposal of resources

## Sample Output

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
✓ Found 23 live contract(s)

Available contracts:
  • NQM6            NQ       E-mini NASDAQ June 2026
  • NQH6            NQ       E-mini NASDAQ March 2026
  • ESM6            ES       E-mini S&P 500 June 2026
  • ESH6            ES       E-mini S&P 500 March 2026
  • YMM6            YM       E-mini Dow June 2026

→ Selected contract for streaming: NQM6
  Symbol: NQ
  Name:   E-mini NASDAQ June 2026
  Active: True

→ Connecting to Market Hub...
✓ Connected to Market Hub

→ Subscribing to real-time data for contract: NQM6
  • Price updates...
  • Order book updates...
  • Trade updates...

✓ Subscribed to all market data streams for NQM6

─────────────────────────────────────────────────────────────
  Receiving real-time updates (Press Ctrl+C to exit)...
─────────────────────────────────────────────────────────────

[10:30:45] Price Update: NQM6
  Last:   16245.50
  Bid:    16245.25 x 10
  Ask:    16245.75 x 15
  Volume: 125,432

[10:30:45] Order Book Update: NQM6
  Bids: 5 levels
    Best: 16245.25 x 10
  Asks: 5 levels
    Best: 16245.75 x 15
  Sequence: 1234567

[10:30:46] Trade Update: NQM6
  Trade ID:  9876543
  Price:     16245.50
  Quantity:  5
  Side:      Buy
  Aggressive: True

Running... 30s | Price updates: 245 | Order book: 180 | Trades: 45   

─────────────────────────────────────────────────────────────
  Demo complete!
─────────────────────────────────────────────────────────────

Total updates received:
  • Price updates:      245
  • Order book updates: 180
  • Trade updates:      45

→ Unsubscribing from NQM6...
✓ Unsubscribed from all streams

→ Disconnecting...
✓ Disconnected and disposed
```

## Customizing the Sample

### Change Contract Search Priority

The sample searches in this order:
1. NQ (E-mini NASDAQ) - Most volatile, good for demos
2. ES (E-mini S&P 500) - Very liquid
3. All live contracts - Any available

Edit `Program.cs` to change the search order:
```csharp
// Search for ES first instead
contracts = await _apiClient.SearchContractsAsync("ES", live: true);

// Or search for a specific symbol
contracts = await _apiClient.SearchContractsAsync("YM", live: true);  // Dow
contracts = await _apiClient.SearchContractsAsync("CL", live: true);  // Crude Oil
contracts = await _apiClient.SearchContractsAsync("GC", live: true);  // Gold
```

### Subscribe to Multiple Contracts

Modify the code to subscribe to multiple contracts:
```csharp
// Take top 3 contracts instead of just first
var selectedContracts = contracts.Take(3).ToList();

foreach (var contract in selectedContracts)
{
    await _wsClient.SubscribeToPriceUpdatesAsync(contract.Id);
    await _wsClient.SubscribeToOrderBookUpdatesAsync(contract.Id);
    await _wsClient.SubscribeToTradeUpdatesAsync(contract.Id);
}
```

### Change Run Duration

Edit `Program.cs`:
```csharp
// Run for 60 seconds instead of 30
for (int i = 0; i < 60; i++)
{
    await Task.Delay(1000);
    // ...
}
```

### Add User Hub / Order Updates

```csharp
// Connect to user hub
await _wsClient.ConnectUserHubAsync();

// Subscribe to order updates
_wsClient.OrderUpdateReceived += (sender, update) =>
{
    Console.WriteLine($"Order Update: {update.OrderId} - {update.Status}");
};

await _wsClient.SubscribeToOrderUpdatesAsync(accountId: 12345);
```

## Troubleshooting

### "API credentials not found"
Set credentials via environment variables or appsettings.json (see Prerequisites above).

### "Failed to connect to Market Hub"
- Check internet connectivity
- Verify API credentials are correct
- Check firewall allows WebSocket connections
- Verify the Market Hub URL is correct

### "Contract not found" or no updates
- The sample now automatically finds valid contracts!
- If you see "No live contracts found":
  - Check account permissions in web portal
  - Verify you're within trading hours
  - Contact support about API access
- If contracts are found but no updates received:
  - Contract may not be actively trading
  - Check market hours for the specific instrument
  - Try during peak trading hours

### Connection keeps reconnecting
- Check network stability
- Verify ProjectX service is operational
- Check logs for detailed error messages

## Learning More

### Key Files to Study
- `Program.cs` - Complete sample implementation
- `../MarqSpec.Client.ProjectX/WebSocket/IProjectXWebSocketClient.cs` - Full API interface
- `../MarqSpec.Client.ProjectX/WebSocket/ProjectXWebSocketClient.cs` - Implementation details

### API Documentation
- **WebSocket Client Interface**: See XML documentation in `IProjectXWebSocketClient.cs`
- **Models**: See `MarqSpec.Client.ProjectX/Api/Models/` for all data structures
- **Configuration**: See `WebSocketOptions.cs` for all configuration options

### PRD Reference
See `../PRD.md` for complete requirements and specifications.

## Next Steps

After running this sample, you can:
1. Build your own real-time trading application
2. Integrate with your own UI framework (WPF, Blazor, etc.)
3. Add your own business logic for trade signals
4. Combine with REST API for order management

## Additional Samples Coming Soon

- REST API samples (placing orders, querying data)
- Combined REST + WebSocket samples
- Advanced reconnection handling
- High-frequency data processing examples
- Order execution with real-time updates

---

**Need Help?** Check the main README or contact support.
