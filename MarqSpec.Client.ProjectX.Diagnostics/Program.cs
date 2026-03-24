using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarqSpec.Client.ProjectX.Diagnostics;

/// <summary>
/// Comprehensive diagnostic tool to investigate ProjectX API contract search behavior.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  ProjectX API Contract Search Diagnostic Tool");
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
            Console.WriteLine("  2. ../MarqSpec.Client.ProjectX.Tests/appsettings.integration.json, OR");
            Console.WriteLine("  3. Environment variables: PROJECTX_API_KEY and PROJECTX_API_SECRET\n");
            return 1;
        }

        Console.WriteLine($"✓ Configuration loaded");
        Console.WriteLine($"  API Key: {apiKey[..Math.Min(8, apiKey.Length)]}... (masked)");
        Console.WriteLine($"  Base URL: {configuration["ProjectX:BaseUrl"]}\n");

        // Setup DI
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });
        services.AddProjectXApiClient(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IProjectXApiClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Run all diagnostic tests
            await RunDiagnostics(client, logger);
            
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Diagnostic completed successfully!");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Diagnostic failed with error:");
            Console.ResetColor();
            Console.WriteLine($"\n{ex.GetType().Name}: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
            }
            
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════\n");
            
            logger.LogError(ex, "Diagnostic failed");
            return 1;
        }
    }

    private static async Task RunDiagnostics(IProjectXApiClient client, ILogger logger)
    {
        var results = new List<DiagnosticResult>();

        // Test 1: Search all contracts (null, live=true)
        results.Add(await RunTest(
            "Test 1: Search ALL contracts (searchText=null, live=true)",
            async () => await client.SearchContractsAsync(null, live: true),
            logger));

        // Test 2: Search all contracts (null, live=false)
        results.Add(await RunTest(
            "Test 2: Search ALL contracts (searchText=null, live=false)",
            async () => await client.SearchContractsAsync(null, live: false),
            logger));

        // Test 3: Search with empty string (live=true)
        results.Add(await RunTest(
            "Test 3: Search with empty string (searchText=\"\", live=true)",
            async () => await client.SearchContractsAsync("", live: true),
            logger));

        // Test 4: Search for "NQ" (live=true)
        results.Add(await RunTest(
            "Test 4: Search for \"NQ\" (live=true)",
            async () => await client.SearchContractsAsync("NQ", live: true),
            logger));

        // Test 5: Search for "NQ" (live=false)
        results.Add(await RunTest(
            "Test 5: Search for \"NQ\" (live=false)",
            async () => await client.SearchContractsAsync("NQ", live: false),
            logger));

        // Test 6: Search for "ES" (live=true)
        results.Add(await RunTest(
            "Test 6: Search for \"ES\" (live=true)",
            async () => await client.SearchContractsAsync("ES", live: true),
            logger));

        // Test 7: Search for "YM" (live=true)
        results.Add(await RunTest(
            "Test 7: Search for \"YM\" (live=true)",
            async () => await client.SearchContractsAsync("YM", live: true),
            logger));

        // Test 8: Search for "CL" (Crude Oil, live=true)
        results.Add(await RunTest(
            "Test 8: Search for \"CL\" (live=true)",
            async () => await client.SearchContractsAsync("CL", live: true),
            logger));

        // Test 9: Search for "GC" (Gold, live=true)
        results.Add(await RunTest(
            "Test 9: Search for \"GC\" (live=true)",
            async () => await client.SearchContractsAsync("GC", live: true),
            logger));

        // Test 10: Search for "ZB" (Treasury Bonds, live=true)
        results.Add(await RunTest(
            "Test 10: Search for \"ZB\" (live=true)",
            async () => await client.SearchContractsAsync("ZB", live: true),
            logger));

        // Find if any test returned contracts
        var successfulTest = results.FirstOrDefault(r => r.ContractCount > 0);

        if (successfulTest != null)
        {
            Console.WriteLine($"\n✓ SUCCESS! Found {successfulTest.ContractCount} contracts in: {successfulTest.TestName}");
            Console.WriteLine("\nFirst 10 contracts:");
            
            foreach (var contract in successfulTest.Contracts.Take(10))
            {
                Console.WriteLine($"  • ID: {contract.Id,-20} Symbol: {contract.SymbolId,-10} Name: {contract.Name}");
            }

            // Test GetContractAsync with a valid contract
            var testContract = successfulTest.Contracts.First();
            Console.WriteLine($"\n─────────────────────────────────────────────────────────────");
            Console.WriteLine($"Test 11: GetContractAsync with ID '{testContract.Id}'");
            Console.WriteLine($"─────────────────────────────────────────────────────────────");
            
            try
            {
                var retrieved = await client.GetContractAsync(testContract.Id);
                
                if (retrieved != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ SUCCESS");
                    Console.ResetColor();
                    Console.WriteLine($"\nContract Details:");
                    Console.WriteLine($"  ID:           {retrieved.Id}");
                    Console.WriteLine($"  Name:         {retrieved.Name}");
                    Console.WriteLine($"  Symbol:       {retrieved.SymbolId}");
                    Console.WriteLine($"  Description:  {retrieved.Description}");
                    Console.WriteLine($"  Tick Size:    {retrieved.TickSize}");
                    Console.WriteLine($"  Tick Value:   {retrieved.TickValue}");
                    Console.WriteLine($"  Active:       {retrieved.ActiveContract}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ WARNING: GetContractAsync returned null");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ FAILED: {ex.Message}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ WARNING: NO CONTRACTS FOUND IN ANY TEST!");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("\nPossible causes:");
            Console.WriteLine("  1. Account permissions - your account may not have access to contract data");
            Console.WriteLine("  2. API endpoint restrictions - contract endpoint may require elevated access");
            Console.WriteLine("  3. Empty database - test environment may not have contract data");
            Console.WriteLine("  4. Market hours - some contracts may only be available during trading hours");
            Console.WriteLine("\nRecommended actions:");
            Console.WriteLine("  1. Log into the ProjectX web portal and verify contract data is visible");
            Console.WriteLine("  2. Test the API directly using Swagger UI: https://api.topstepx.com/swagger");
            Console.WriteLine("  3. Contact ProjectX support to verify account permissions");
            Console.WriteLine("  4. Check if your account requires a specific subscription tier");
        }

        // Summary
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Summary:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        var totalTests = results.Count;
        var successfulTests = results.Count(r => r.Success);
        var testsWithContracts = results.Count(r => r.ContractCount > 0);
        var failedTests = results.Count(r => !r.Success);

        Console.WriteLine($"  Total Tests:           {totalTests}");
        Console.WriteLine($"  Successful (no error): {successfulTests}");
        Console.WriteLine($"  Tests with contracts:  {testsWithContracts}");
        Console.WriteLine($"  Failed Tests:          {failedTests}");
        
        if (testsWithContracts > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  ✓ API is working - contracts were found!");
            Console.ResetColor();
        }
        else if (successfulTests == totalTests)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ⚠ API is responding but returning empty results");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  ✗ API errors detected");
            Console.ResetColor();
        }
    }

    private static async Task<DiagnosticResult> RunTest(
        string testName, 
        Func<Task<IEnumerable<MarqSpec.Client.ProjectX.Api.Models.Contract>>> testFunc,
        ILogger logger)
    {
        Console.WriteLine($"\n─────────────────────────────────────────────────────────────");
        Console.WriteLine(testName);
        Console.WriteLine($"─────────────────────────────────────────────────────────────");

        var result = new DiagnosticResult { TestName = testName };

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var contracts = await testFunc();
            stopwatch.Stop();

            result.Success = true;
            result.Contracts = contracts.ToList();
            result.ContractCount = result.Contracts.Count;
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            if (result.ContractCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ SUCCESS - Found {result.ContractCount} contracts ({result.DurationMs}ms)");
                Console.ResetColor();
                
                Console.WriteLine("\nFirst 3 contracts:");
                foreach (var contract in result.Contracts.Take(3))
                {
                    Console.WriteLine($"  • ID: {contract.Id,-20} Symbol: {contract.SymbolId,-10} Name: {contract.Name}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ SUCCESS but EMPTY - 0 contracts returned ({result.DurationMs}ms)");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ FAILED - {ex.GetType().Name}: {ex.Message}");
            Console.ResetColor();

            logger.LogError(ex, "Test failed: {TestName}", testName);
        }

        return result;
    }
}

internal class DiagnosticResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ContractCount { get; set; }
    public List<MarqSpec.Client.ProjectX.Api.Models.Contract> Contracts { get; set; } = new();
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
