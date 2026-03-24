// Quick diagnostic program to test contract search
// Run with: dotnet run DiagnoseContracts.cs
// Or use dotnet script: dotnet script DiagnoseContracts.cs
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  Quick Contract Search Diagnostic");
Console.WriteLine("═══════════════════════════════════════════════════════\n");

// Load configuration from multiple sources
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("MarqSpec.Client.ProjectX.Tests/appsettings.integration.json", optional: true)
    .AddJsonFile("MarqSpec.Client.ProjectX.Diagnostics/appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Validate credentials
if (string.IsNullOrWhiteSpace(config["ProjectX:ApiKey"]))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("✗ ERROR: API credentials not found!");
    Console.ResetColor();
    Console.WriteLine("Set PROJECTX_API_KEY and PROJECTX_API_SECRET environment variables");
    Environment.Exit(1);
}

// Setup services
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddProjectXApiClient(config);

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IProjectXApiClient>();

Console.WriteLine($"✓ Credentials loaded");
Console.WriteLine($"  Base URL: {config["ProjectX:BaseUrl"]}\n");
Console.WriteLine("Testing ProjectX API Contract Search...\n");

try
{
    // Test 1: Search for all contracts
    Console.WriteLine("1. Searching for ALL contracts (null search, live=true):");
    var allLive = await client.SearchContractsAsync(null, live: true);
    Console.WriteLine($"   Found: {allLive.Count()} live contracts");
    if (allLive.Any())
    {
        Console.WriteLine("   First 5:");
        foreach (var c in allLive.Take(5))
            Console.WriteLine($"     - {c.Id} | {c.SymbolId} | {c.Name}");
    }
    
    // Test 2: Search with live=false
    Console.WriteLine("\n2. Searching for ALL contracts (null search, live=false):");
    var allNotLive = await client.SearchContractsAsync(null, live: false);
    Console.WriteLine($"   Found: {allNotLive.Count()} contracts");
    if (allNotLive.Any())
    {
        Console.WriteLine("   First 5:");
        foreach (var c in allNotLive.Take(5))
            Console.WriteLine($"     - {c.Id} | {c.SymbolId} | {c.Name}");
    }
    
    // Test 3: Search for NQ
    Console.WriteLine("\n3. Searching for 'NQ' contracts (live=true):");
    var nq = await client.SearchContractsAsync("NQ", live: true);
    Console.WriteLine($"   Found: {nq.Count()} contracts");
    foreach (var c in nq.Take(3))
        Console.WriteLine($"     - {c.Id} | {c.SymbolId} | {c.Name}");
    
    // Test 4: Try getting a specific contract if we found any
    if (allLive.Any() || allNotLive.Any())
    {
        var testContract = (allLive.Any() ? allLive : allNotLive).First();
        Console.WriteLine($"\n4. Getting specific contract '{testContract.Id}':");
        var specific = await client.GetContractAsync(testContract.Id);
        if (specific != null)
        {
            Console.WriteLine($"   ✓ Success!");
            Console.WriteLine($"     Name: {specific.Name}");
            Console.WriteLine($"     Symbol: {specific.SymbolId}");
            Console.WriteLine($"     Tick Size: {specific.TickSize}");
            Console.WriteLine($"     Tick Value: {specific.TickValue}");
            Console.WriteLine($"     Active: {specific.ActiveContract}");
        }
        else
        {
            Console.WriteLine("   ✗ GetContractAsync returned null");
        }
    }

    Console.WriteLine("\n✓ Diagnostic complete");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    Console.ResetColor();
    if (ex.InnerException != null)
        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    Console.WriteLine($"\nStack:\n{ex.StackTrace}");
    Environment.Exit(1);
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
