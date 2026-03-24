# ProjectX API Contract Search Diagnostics

This is a comprehensive diagnostic tool to investigate why the ProjectX API contract search is returning empty results in integration tests.

## Purpose

This tool systematically tests the `/api/Contract/search` endpoint with various parameter combinations to:
1. Identify which search parameters work
2. Determine if the account has access to contract data
3. Validate API connectivity and authentication
4. Find valid contract IDs for testing

## Quick Start

### Prerequisites

You need valid ProjectX API credentials. Provide them via one of these methods:

#### Option 1: appsettings.json (in this directory)

Edit `appsettings.json`:
```json
{
  "ProjectX": {
    "ApiKey": "your-api-key-here",
    "ApiSecret": "your-api-secret-here",
    "BaseUrl": "https://api.topstepx.com"
  }
}
```

#### Option 2: Use existing integration test config

The tool automatically looks for:
```
../MarqSpec.Client.ProjectX.Tests/appsettings.integration.json
```

#### Option 3: Environment Variables

```bash
# Windows PowerShell
$env:PROJECTX_API_KEY = "your-api-key"
$env:PROJECTX_API_SECRET = "your-api-secret"

# Linux/Mac
export PROJECTX_API_KEY="your-api-key"
export PROJECTX_API_SECRET="your-api-secret"
```

### Run the Diagnostics

```bash
# From solution root
cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run
```

Or from Visual Studio:
1. Set `MarqSpec.Client.ProjectX.Diagnostics` as startup project
2. Press F5 or Ctrl+F5

## What It Tests

The diagnostic tool runs 10+ tests with different search parameters:

| Test | Search Text | Live Flag | Purpose |
|------|-------------|-----------|---------|
| 1 | `null` | `true` | Get all live contracts |
| 2 | `null` | `false` | Get all contracts (live and expired) |
| 3 | `""` | `true` | Empty string search |
| 4 | `"NQ"` | `true` | E-mini NASDAQ |
| 5 | `"NQ"` | `false` | E-mini NASDAQ (include expired) |
| 6 | `"ES"` | `true` | E-mini S&P 500 |
| 7 | `"YM"` | `true` | E-mini Dow |
| 8 | `"CL"` | `true` | Crude Oil |
| 9 | `"GC"` | `true` | Gold |
| 10 | `"ZB"` | `true` | Treasury Bonds |

If any test finds contracts, it will also test `GetContractAsync()` with a specific contract ID.

## Understanding the Output

### Success Case (Contracts Found)

```
═══════════════════════════════════════════════════════════════
  ProjectX API Contract Search Diagnostic Tool
═══════════════════════════════════════════════════════════════

✓ Configuration loaded
  API Key: 12345678... (masked)
  Base URL: https://api.topstepx.com

─────────────────────────────────────────────────────────────
Test 1: Search ALL contracts (searchText=null, live=true)
─────────────────────────────────────────────────────────────
✓ SUCCESS - Found 42 contracts (234ms)

First 3 contracts:
  • ID: NQH25               Symbol: NQ         Name: E-mini NASDAQ Mar 2025
  • ID: ESH25               Symbol: ES         Name: E-mini S&P 500 Mar 2025
  • ID: YMH25               Symbol: YM         Name: E-mini Dow Mar 2025
```

### Problem Case (No Contracts)

```
⚠ SUCCESS but EMPTY - 0 contracts returned (156ms)

═══════════════════════════════════════════════════════════════
⚠ WARNING: NO CONTRACTS FOUND IN ANY TEST!
═══════════════════════════════════════════════════════════════

Possible causes:
  1. Account permissions - your account may not have access to contract data
  2. API endpoint restrictions - contract endpoint may require elevated access
  3. Empty database - test environment may not have contract data
  4. Market hours - some contracts may only be available during trading hours
```

### Error Case (API Failure)

```
✗ FAILED - ProjectXApiException: Failed to search contracts: Unauthorized
```

## Next Steps Based on Results

### If contracts are found ✓

Great! The API is working. The integration test issue may be:
- Different credentials between diagnostic and tests
- Test environment vs production environment
- Timing issue (tests running at different time)

**Action**: Update integration tests to use the same credentials and parameters that work here.

### If no contracts found (but no errors) ⚠

The API is responding but returning empty results. This suggests:
- Account doesn't have access to contract data
- Need different subscription tier
- Contract data not available in test environment

**Actions**:
1. Log into ProjectX web portal - can you see contract data there?
2. Contact ProjectX support - verify account has API access to contracts
3. Test in Swagger UI: https://api.topstepx.com/swagger

### If API errors occur ✗

Authentication or authorization issue.

**Actions**:
1. Verify API credentials are correct
2. Check if account is active
3. Verify API key has not expired
4. Test authentication separately

## Troubleshooting

### "API credentials not found"

Credentials are not configured. See "Prerequisites" section above.

### "Unauthorized" errors

- Check API key and secret are correct
- Verify account is active
- Try regenerating API credentials in ProjectX portal

### "Network error"

- Check internet connectivity
- Verify firewall allows access to api.topstepx.com
- Check if behind corporate proxy

## Advanced Usage

### Enable detailed logging

Edit `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "MarqSpec.Client.ProjectX": "Trace",
      "System.Net.Http.HttpClient": "Debug"
    }
  }
}
```

This will show:
- Full HTTP requests/responses
- Authentication token details
- Detailed error information

### Test against different environment

Edit `appsettings.json`:
```json
{
  "ProjectX": {
    "BaseUrl": "https://api-staging.topstepx.com"  // if staging exists
  }
}
```

## Related Files

- `DiagnoseContracts.cs` - Quick diagnostic script (simpler version)
- `TestContractSearch.csx` - C# script for interactive testing
- `../MarqSpec.Client.ProjectX.Tests/Integration/MarketDataIntegrationTests.cs` - Full integration tests

## Support

If diagnostics show API is working but integration tests still fail:
1. Compare credentials used
2. Compare environment variables
3. Check test execution context
4. Review test collection setup

If diagnostics show no contracts:
1. Contact ProjectX support
2. Verify account permissions
3. Test in ProjectX web portal UI
4. Check subscription tier requirements
