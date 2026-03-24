using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("MarqSpec.Client.ProjectX.Tests/appsettings.integration.json")
    .Build();

// Setup DI
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
services.AddProjectXApiClient(configuration);

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<IProjectXApiClient>();

try
{
    Console.WriteLine("Testing contract search...");
    
    // Try searching for NQ contracts
    Console.WriteLine("\n1. Searching for 'NQ' contracts (live)...");
    var nqContracts = await client.SearchContractsAsync("NQ", live: true);
    Console.WriteLine($"   Found {nqContracts.Count()} contracts");
    foreach (var contract in nqContracts.Take(3))
    {
        Console.WriteLine($"   - {contract.Id}: {contract.Name}");
    }
    
    // Try searching for all contracts
    Console.WriteLine("\n2. Searching for ALL contracts (null search, live)...");
    var allContracts = await client.SearchContractsAsync(null, live: true);
    Console.WriteLine($"   Found {allContracts.Count()} contracts");
    foreach (var contract in allContracts.Take(5))
    {
        Console.WriteLine($"   - {contract.Id}: {contract.Name} (Symbol: {contract.SymbolId})");
    }
    
    // Try without live filter
    Console.WriteLine("\n3. Searching for 'NQ' contracts (not live)...");
    var nqContractsNotLive = await client.SearchContractsAsync("NQ", live: false);
    Console.WriteLine($"   Found {nqContractsNotLive.Count()} contracts");
    foreach (var contract in nqContractsNotLive.Take(3))
    {
        Console.WriteLine($"   - {contract.Id}: {contract.Name}");
    }
    
    // Try a specific contract if we found any
    if (allContracts.Any())
    {
        var firstContract = allContracts.First();
        Console.WriteLine($"\n4. Getting specific contract: {firstContract.Id}...");
        var specificContract = await client.GetContractAsync(firstContract.Id);
        if (specificContract != null)
        {
            Console.WriteLine($"   SUCCESS! Got contract: {specificContract.Name}");
            Console.WriteLine($"   Tick Size: {specificContract.TickSize}, Tick Value: {specificContract.TickValue}");
        }
        else
        {
            Console.WriteLine("   ERROR: GetContractAsync returned null");
        }
    }
    
    Console.WriteLine("\n✓ All tests completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
}
