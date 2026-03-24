using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarqSpec.Client.ProjectX.Samples;

/// <summary>
/// Sample application demonstrating WebSocket real-time market data and order updates.
/// </summary>
public class Program
{
    private static IProjectXApiClient? _apiClient;
    private static IProjectXWebSocketClient? _wsClient;
    private static int _priceUpdateCount = 0;
    private static int _orderBookUpdateCount = 0;
    private static int _tradeUpdateCount = 0;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  ProjectX WebSocket Client - Sample Application");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("../MarqSpec.Client.ProjectX.Tests/appsettings.integration.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Validate credentials
        var apiKey = configuration["ProjectX:ApiKey"];
        var apiSecret = configuration["ProjectX:ApiSecret"];
        
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ ERROR: API credentials not found!");
            Console.ResetColor();
            Console.WriteLine("\nPlease provide credentials via:");
            Console.WriteLine("  1. appsettings.json in this directory, OR");
            Console.WriteLine("  2. Environment variables: PROJECTX_API_KEY and PROJECTX_API_SECRET\n");
            return;
        }

        Console.WriteLine("✓ Configuration loaded");
        Console.WriteLine($"  API Key: {apiKey[..Math.Min(8, apiKey.Length)]}... (masked)\n");

        // Setup DI
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });
        services.AddProjectXApiClient(configuration);

        var serviceProvider = services.BuildServiceProvider();
        _apiClient = serviceProvider.GetRequiredService<IProjectXApiClient>();
        _wsClient = serviceProvider.GetRequiredService<IProjectXWebSocketClient>();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            await Cleanup();
            Environment.Exit(0);
        };

        try
        {
            await RunSampleAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
        }
        finally
        {
            await Cleanup();
        }
    }

    private static async Task RunSampleAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  Demo: Real-time Market Data Streaming");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        // Step 1: Query for live contracts
        Console.WriteLine("→ Querying for live contracts...");
        IEnumerable<Api.Models.Contract> contracts;

        try
        {
            // Search for NQ contracts first (E-mini NASDAQ)
            contracts = await _apiClient!.SearchContractsAsync("NQ", live: true);

            // If no NQ contracts, try ES (E-mini S&P 500)
            if (!contracts.Any())
            {
                Console.WriteLine("  No NQ contracts found, trying ES...");
                contracts = await _apiClient.SearchContractsAsync("ES", live: true);
            }

            // If still no contracts, try all live contracts
            if (!contracts.Any())
            {
                Console.WriteLine("  No specific contracts found, trying all live...");
                contracts = await _apiClient.SearchContractsAsync(null, live: true);
            }

            if (!contracts.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ ERROR: No live contracts found!");
                Console.ResetColor();
                Console.WriteLine("\nPossible causes:");
                Console.WriteLine("  • Account may not have access to contract data");
                Console.WriteLine("  • Outside of trading hours");
                Console.WriteLine("  • API credentials may have limited permissions");
                return;
            }

            Console.WriteLine($"✓ Found {contracts.Count()} live contract(s)\n");

            // Display available contracts
            Console.WriteLine("Available contracts:");
            foreach (var contract in contracts.Take(5))
            {
                Console.WriteLine($"  • {contract.Id,-15} {contract.SymbolId,-8} {contract.Name}");
            }
            if (contracts.Count() > 5)
            {
                Console.WriteLine($"  ... and {contracts.Count() - 5} more");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ ERROR querying contracts: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // Step 2: Select a contract to subscribe to
        var selectedContract = contracts.First();
        var contractId = selectedContract.Id;

        Console.WriteLine($"→ Selected contract for streaming: {contractId}");
        Console.WriteLine($"  Symbol: {selectedContract.SymbolId}");
        Console.WriteLine($"  Name:   {selectedContract.Name}");
        Console.WriteLine($"  Active: {selectedContract.ActiveContract}");
        Console.WriteLine();

        // Step 3: Subscribe to connection status changes
        _wsClient!.ConnectionStatusChanged += OnConnectionStatusChanged;

        // Step 4: Subscribe to market data events
        _wsClient.PriceUpdateReceived += OnPriceUpdateReceived;
        _wsClient.OrderBookUpdateReceived += OnOrderBookUpdateReceived;
        _wsClient.TradeUpdateReceived += OnTradeUpdateReceived;

        // Step 5: Connect to market hub
        Console.WriteLine("→ Connecting to Market Hub...");
        await _wsClient.ConnectMarketHubAsync();
        Console.WriteLine("✓ Connected to Market Hub\n");

        // Step 6: Subscribe to real-time data streams
        Console.WriteLine($"→ Subscribing to real-time data for contract: {contractId}");

        try
        {
            Console.WriteLine($"  • Price updates...");
            await _wsClient.SubscribeToPriceUpdatesAsync(contractId);

            Console.WriteLine($"  • Order book updates...");
            await _wsClient.SubscribeToOrderBookUpdatesAsync(contractId);

            Console.WriteLine($"  • Trade updates...");
            await _wsClient.SubscribeToTradeUpdatesAsync(contractId);

            Console.WriteLine($"\n✓ Subscribed to all market data streams for {contractId}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ ERROR subscribing to data streams: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("\nContinuing anyway to test connection...");
        }

        Console.WriteLine("\n─────────────────────────────────────────────────────────────");
        Console.WriteLine("  Receiving real-time updates (Press Ctrl+C to exit)...");
        Console.WriteLine("─────────────────────────────────────────────────────────────\n");

        // Step 7: Run for 30 seconds showing stats
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(1000);
            Console.Write($"\rRunning... {i + 1}s | Price updates: {_priceUpdateCount} | Order book: {_orderBookUpdateCount} | Trades: {_tradeUpdateCount}   ");
        }

        Console.WriteLine("\n\n─────────────────────────────────────────────────────────────");
        Console.WriteLine("  Demo complete!");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"\nTotal updates received:");
        Console.WriteLine($"  • Price updates:      {_priceUpdateCount}");
        Console.WriteLine($"  • Order book updates: {_orderBookUpdateCount}");
        Console.WriteLine($"  • Trade updates:      {_tradeUpdateCount}");

        if (_priceUpdateCount == 0 && _orderBookUpdateCount == 0 && _tradeUpdateCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠ No updates received. Possible causes:");
            Console.WriteLine("  • Contract may not be actively trading");
            Console.WriteLine("  • Outside of trading hours");
            Console.WriteLine("  • WebSocket subscription may not be working");
            Console.ResetColor();
        }

        // Step 8: Unsubscribe
        Console.WriteLine($"\n→ Unsubscribing from {contractId}...");
        try
        {
            await _wsClient.UnsubscribeFromPriceUpdatesAsync(contractId);
            await _wsClient.UnsubscribeFromOrderBookUpdatesAsync(contractId);
            await _wsClient.UnsubscribeFromTradeUpdatesAsync(contractId);
            Console.WriteLine("✓ Unsubscribed from all streams");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error unsubscribing: {ex.Message}");
        }
    }

    private static void OnConnectionStatusChanged(object? sender, ConnectionStatusChange change)
    {
        var color = change.CurrentState switch
        {
            ConnectionState.Connected => ConsoleColor.Green,
            ConnectionState.Connecting => ConsoleColor.Yellow,
            ConnectionState.Reconnecting => ConsoleColor.Yellow,
            ConnectionState.Disconnected => ConsoleColor.Gray,
            ConnectionState.Failed => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"\n[{change.Timestamp:HH:mm:ss}] Connection: {change.PreviousState} → {change.CurrentState}");
        if (!string.IsNullOrEmpty(change.ErrorMessage))
        {
            Console.WriteLine($"  Error: {change.ErrorMessage}");
        }
        Console.ResetColor();
    }

    private static void OnPriceUpdateReceived(object? sender, Api.Models.PriceUpdate update)
    {
        _priceUpdateCount++;
        
        // Show first few updates in detail
        if (_priceUpdateCount <= 3)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[{update.Timestamp:HH:mm:ss}] Price Update: {update.ContractId}");
            Console.ResetColor();
            Console.WriteLine($"  Last:   {update.LastPrice:F2}");
            Console.WriteLine($"  Bid:    {update.BidPrice:F2} x {update.BidSize}");
            Console.WriteLine($"  Ask:    {update.AskPrice:F2} x {update.AskSize}");
            Console.WriteLine($"  Volume: {update.Volume:N0}");
        }
    }

    private static void OnOrderBookUpdateReceived(object? sender, Api.Models.OrderBookUpdate update)
    {
        _orderBookUpdateCount++;
        
        // Show first few updates in detail
        if (_orderBookUpdateCount <= 3)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n[{update.Timestamp:HH:mm:ss}] Order Book Update: {update.ContractId}");
            Console.ResetColor();
            Console.WriteLine($"  Bids: {update.Bids.Count} levels");
            if (update.Bids.Any())
            {
                var bestBid = update.Bids[0];
                Console.WriteLine($"    Best: {bestBid.Price:F2} x {bestBid.Quantity}");
            }
            Console.WriteLine($"  Asks: {update.Asks.Count} levels");
            if (update.Asks.Any())
            {
                var bestAsk = update.Asks[0];
                Console.WriteLine($"    Best: {bestAsk.Price:F2} x {bestAsk.Quantity}");
            }
            Console.WriteLine($"  Sequence: {update.SequenceNumber}");
        }
    }

    private static void OnTradeUpdateReceived(object? sender, Api.Models.TradeUpdate update)
    {
        _tradeUpdateCount++;
        
        // Show first few updates in detail
        if (_tradeUpdateCount <= 3)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[{update.Timestamp:HH:mm:ss}] Trade Update: {update.ContractId}");
            Console.ResetColor();
            Console.WriteLine($"  Trade ID:  {update.TradeId}");
            Console.WriteLine($"  Price:     {update.Price:F2}");
            Console.WriteLine($"  Quantity:  {update.Quantity}");
            Console.WriteLine($"  Side:      {update.Side}");
            Console.WriteLine($"  Aggressive: {update.IsAggressive}");
        }
    }

    private static async Task Cleanup()
    {
        if (_wsClient != null)
        {
            Console.WriteLine("\n\n→ Disconnecting...");
            await _wsClient.DisconnectMarketHubAsync();
            await _wsClient.DisposeAsync();
            Console.WriteLine("✓ Disconnected and disposed");
        }
    }
}
