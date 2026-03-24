# Running Integration Tests - Quick Start Guide

## Prerequisites

You need valid ProjectX API credentials to run integration tests.

## Step-by-Step Instructions

### Step 1: Set Environment Variables

**Windows (PowerShell):**
```powershell
$env:PROJECTX_API_KEY = "your-actual-api-key"
$env:PROJECTX_API_SECRET = "your-actual-api-secret"
```

**Windows (Command Prompt):**
```cmd
set PROJECTX_API_KEY=your-actual-api-key
set PROJECTX_API_SECRET=your-actual-api-secret
```

**Linux/Mac:**
```bash
export PROJECTX_API_KEY="your-actual-api-key"
export PROJECTX_API_SECRET="your-actual-api-secret"
```

### Step 2: Enable Tests

Integration tests are skipped by default. To run them, you must remove the `Skip` parameter:

**Example:**
```csharp
// BEFORE (Skipped)
[Fact(Skip = "Manual execution only - requires valid API credentials")]
public async Task GetCurrentPriceAsync_WithValidSymbol_ReturnsValidPrice()

// AFTER (Enabled)
[Fact]
public async Task GetCurrentPriceAsync_WithValidSymbol_ReturnsValidPrice()
```

### Step 3: Run Tests

**Option A: Visual Studio**
1. Open Test Explorer (Test > Test Explorer)
2. Find the test you want to run under "Integration" folder
3. Right-click on the test
4. Select "Run"

**Option B: Command Line**
```bash
# Navigate to solution directory
cd MarqSpec.Client.ProjectX

# Run all tests (integration tests with Skip removed will execute)
dotnet test MarqSpec.Client.ProjectX.Tests/MarqSpec.Client.ProjectX.Tests.csproj --verbosity detailed
```

**Option C: Run Specific Test**
```bash
dotnet test --filter "FullyQualifiedName~GetCurrentPriceAsync_WithValidSymbol_ReturnsValidPrice"
```

## Recommended Test Execution Order

### 1. Start with Authentication
```csharp
// Remove Skip from this test first
AuthenticationIntegrationTests.GetAccessTokenAsync_WithValidCredentials_ReturnsToken
```

If this passes, your credentials are valid.

### 2. Test Basic API Calls
```csharp
// Remove Skip from these tests
ProjectXApiClientIntegrationTests.GetCurrentPriceAsync_WithValidSymbol_ReturnsValidPrice
ProjectXApiClientIntegrationTests.GetOrderBookAsync_WithValidSymbol_ReturnsValidOrderBook
ProjectXApiClientIntegrationTests.GetRecentTradesAsync_WithValidSymbol_ReturnsValidTrades
```

### 3. Test Performance
```csharp
// Remove Skip from these tests
ResilienceIntegrationTests.ApiCall_UnderNormalConditions_CompletesSuccessfully
ResilienceIntegrationTests.MultipleApiCalls_MeasurePerformanceConsistency
```

### 4. Test Advanced Scenarios
```csharp
// Remove Skip from these tests
ProjectXApiClientIntegrationTests.ConcurrentApiCalls_WithSameClient_SucceedWithoutErrors
AuthenticationIntegrationTests.ConcurrentAuthentication_HandlesMultipleRequests
```

## Configuration Options

### Update Test Symbols

If your API uses different symbols than ES/NQ, update the constants:

**File:** `MarqSpec.Client.ProjectX.Tests/Integration/ProjectXApiClientIntegrationTests.cs`

```csharp
private const string ValidSymbol = "YOUR_SYMBOL";       // Change ES to your symbol
private const string AlternateSymbol = "YOUR_ALT_SYMBOL"; // Change NQ to your symbol
```

### Adjust Logging Level

**File:** `MarqSpec.Client.ProjectX.Tests/appsettings.integration.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",           // Change to "Debug" for more details
      "MarqSpec.Client.ProjectX": "Debug" // Or "Information" for less detail
    }
  }
}
```

## Troubleshooting

### ❌ Problem: "Integration tests require PROJECTX_API_KEY..."

**Solution:** Environment variables not set. Follow Step 1 above.

### ❌ Problem: Authentication fails

**Possible Causes:**
1. Invalid API credentials
2. API endpoint is down
3. Network connectivity issues

**Solution:**
```bash
# Verify credentials are set
echo $env:PROJECTX_API_KEY  # Windows PowerShell
echo $PROJECTX_API_KEY      # Linux/Mac

# Check if you can reach the API
curl https://api.topstepx.com

# Try authentication test first
# Run: AuthenticationIntegrationTests.GetAccessTokenAsync_WithValidCredentials_ReturnsToken
```

### ❌ Problem: Tests still skip even after removing Skip attribute

**Solution:** Rebuild the solution

```bash
dotnet clean
dotnet build
dotnet test
```

### ❌ Problem: Rate limiting errors (429)

**Possible Causes:**
- Too many requests too quickly
- Multiple test runs without delays

**Solution:**
1. Add delays between test runs
2. Run fewer tests at once
3. Wait 60 seconds before retrying

### ❌ Problem: Test timeouts

**Possible Causes:**
- Slow network connection
- API experiencing high load

**Solution:**
Increase timeout in the test:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Increase from 5 to 30
await Client.GetCurrentPriceAsync(symbol, cts.Token);
```

## Safety Reminders

### ⚠️ DO NOT:
- Commit enabled integration tests (without Skip)
- Hardcode credentials in source code
- Run integration tests in automated CI/CD without proper controls
- Execute all tests simultaneously (may hit rate limits)

### ✅ DO:
- Keep Skip attribute in committed code
- Use environment variables for credentials
- Run tests manually and sparingly
- Monitor API usage
- Use test/staging credentials when available

## Example Session

Here's a complete example of running integration tests:

```powershell
# 1. Set credentials
$env:PROJECTX_API_KEY = "abc123xyz"
$env:PROJECTX_API_SECRET = "secret789"

# 2. Verify they're set
echo $env:PROJECTX_API_KEY

# 3. Edit test file - remove Skip from one test
# Open: MarqSpec.Client.ProjectX.Tests/Integration/AuthenticationIntegrationTests.cs
# Change line 12 from:
#   [Fact(Skip = "Manual execution only - requires valid API credentials")]
# To:
#   [Fact]

# 4. Build solution
dotnet build

# 5. Run the specific test
dotnet test --filter "GetAccessTokenAsync_WithValidCredentials_ReturnsToken" --verbosity detailed

# 6. Check results
# If PASSED: ✅ Credentials are valid, proceed with other tests
# If FAILED: ❌ Check error message, verify credentials

# 7. Re-enable Skip before committing
# Change back to:
#   [Fact(Skip = "Manual execution only - requires valid API credentials")]
```

## Viewing Test Output

### Detailed Output
```bash
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

### View Performance Metrics
Integration tests output performance data to console:
```
Latency stats: Avg=245.33ms, Max=423ms, Min=187ms
Concurrent calls: 10, Total time: 1250ms, Avg: 125.00ms
```

### View in Visual Studio
1. Run test in Test Explorer
2. Click on the test result
3. View "Output" link
4. See detailed logs and timing information

## Next Steps After Successful Run

1. ✅ Document baseline performance metrics
2. ✅ Set up monitoring for API usage
3. ✅ Configure staging environment testing
4. ✅ Create test data validation report
5. ✅ Schedule regular integration test runs

## Support

If you encounter issues:
1. Check the Integration/README.md for detailed documentation
2. Review test logs for specific error messages
3. Verify API endpoint status
4. Contact development team with test output

---

**Remember:** Always re-enable the `Skip` attribute before committing code to prevent accidental API calls in CI/CD pipelines.
