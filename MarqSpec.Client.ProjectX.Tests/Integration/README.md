# Integration Tests

This directory contains integration tests that make actual calls to the ProjectX API.

## Prerequisites

### API Credentials (Required)

You can provide API credentials in **two ways** (environment variables take precedence):

#### Option 1: Environment Variables (Recommended for CI/CD)

```bash
# Windows PowerShell
$env:PROJECTX_API_KEY = "your-api-key"
$env:PROJECTX_API_SECRET = "your-api-secret"

# Linux/Mac
export PROJECTX_API_KEY="your-api-key"
export PROJECTX_API_SECRET="your-api-secret"
```

#### Option 2: appsettings.integration.json (Recommended for Local Development)

Create or edit `appsettings.integration.json` in the test project:

```json
{
  "ProjectX": {
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "BaseUrl": "https://api.topstepx.com"
  }
}
```

**Note:** If both are set, environment variables will override appsettings values.

## Running Integration Tests

Integration tests will automatically use credentials from either source. If no credentials are found, tests will be skipped with an informative message.

### Option 1: Run All Integration Tests

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Option 2: Run Specific Integration Test Category

```bash
# Authentication tests only
dotnet test --filter "FullyQualifiedName~AuthenticationIntegrationTests"

# Market data tests only
dotnet test --filter "FullyQualifiedName~MarketDataIntegrationTests"
```

### Option 3: Run from Visual Studio Test Explorer

1. Right-click on the test
2. Select "Run"

Tests will automatically detect credentials from environment variables or appsettings.integration.json.

## Test Behavior

### With Valid Credentials
- Tests execute against the real ProjectX API
- Results validate actual API behavior
- Tests may take several seconds due to network calls

### Without Credentials
- Tests are automatically skipped
- Skip message indicates where to set credentials:
  ```
  Integration tests require PROJECTX_API_KEY and PROJECTX_API_SECRET to be set in either:
    1. Environment variables, or
    2. appsettings.integration.json (ProjectX:ApiKey and ProjectX:ApiSecret)
  ```

1. Open the test file
2. Remove the `Skip` parameter from the `[Fact]` attribute
3. Run the test

**Example:**
```csharp
// Before
[Fact(Skip = "Manual execution only - requires valid API credentials")]

// After
[Fact]
```

## Test Categories

### 1. ProjectXApiClientIntegrationTests

Tests the main API client functionality:
- `GetCurrentPriceAsync()` - Current price retrieval
- `GetOrderBookAsync()` - Order book depth queries
- `GetRecentTradesAsync()` - Recent trade history
- Performance and latency validation
- Concurrent call handling
- Data consistency checks

**Key Tests:**
- ✅ Valid symbol returns proper data
- ✅ Invalid symbol throws appropriate exception
- ✅ Token caching works across multiple calls
- ✅ Order book data is properly ordered
- ✅ Concurrent calls are thread-safe
- ✅ Performance meets PRD requirements (p99 < 1000ms)

### 2. AuthenticationIntegrationTests

Tests authentication functionality:
- Token acquisition
- Token caching
- Token refresh
- Concurrent authentication requests
- Invalid credential handling

**Key Tests:**
- ✅ Valid credentials return JWT token
- ✅ Multiple calls cache the token
- ✅ Token refresh works correctly
- ✅ Concurrent requests are handled safely

### 3. ResilienceIntegrationTests

Tests resilience features:
- Retry policies
- Rate limiting compliance
- Performance under load
- Timeout handling
- Throughput testing

**Key Tests:**
- ✅ API calls complete within SLA (PRD requirements)
- ✅ Average latency < 500ms
- ✅ Max latency < 1000ms
- ✅ Concurrent calls maintain performance
- ✅ Rate limiting is respected (200 requests/60s for GET)

## Expected Test Symbols

The tests use the following symbols (update based on your API):
- `ES` - E-mini S&P 500 futures
- `NQ` - E-mini NASDAQ-100 futures

Update these constants in the test files if different symbols are available in your environment.

## Performance Benchmarks

Based on PRD requirements:

| Metric | Requirement | Tested By |
|--------|-------------|-----------|
| REST API p95 latency | < 500ms | `ApiCall_UnderNormalConditions_CompletesSuccessfully` |
| REST API p99 latency | < 1000ms | `GetCurrentPriceAsync_PerformanceTest_CompletesWithinTimeout` |
| GET rate limit | 200 req/60s | `SequentialApiCalls_TestRateLimiting` |
| API success rate | >99.9% | All integration tests |

## Troubleshooting

### Tests are Skipped

**Cause:** Environment variables not set or tests have `Skip` attribute.

**Solution:**
1. Set `PROJECTX_API_KEY` and `PROJECTX_API_SECRET` environment variables
2. Remove `Skip` parameter from test attributes

### Authentication Fails

**Cause:** Invalid or expired credentials.

**Solution:**
1. Verify credentials are correct
2. Check if API key has necessary permissions
3. Verify network connectivity to API endpoint

### Rate Limiting Errors

**Cause:** Too many requests in short time.

**Solution:**
1. Add delays between test runs
2. Reduce number of test iterations
3. Run tests with longer intervals

### Tests Timeout

**Cause:** Network issues or API slowness.

**Solution:**
1. Check network connectivity
2. Verify API endpoint is accessible
3. Increase timeout values if necessary

## Best Practices

1. **Don't run integration tests in CI/CD** without proper rate limiting
2. **Use test credentials** separate from production
3. **Run integration tests sparingly** to avoid rate limits
4. **Monitor API usage** when running integration tests
5. **Clean up test data** if tests create any resources

## CI/CD Integration

For CI/CD pipelines, consider:

```yaml
# Example: Only run integration tests on-demand
- name: Integration Tests
  run: dotnet test --filter "Category=Integration"
  if: github.event_name == 'workflow_dispatch'
  env:
    PROJECTX_API_KEY: ${{ secrets.PROJECTX_API_KEY }}
    PROJECTX_API_SECRET: ${{ secrets.PROJECTX_API_SECRET }}
```

## Security Notes

- ⚠️ **Never commit API credentials** to source control
- ⚠️ **Use environment variables** for sensitive data
- ⚠️ **Rotate test credentials** regularly
- ⚠️ **Use separate credentials** for testing vs production
- ⚠️ **Monitor API usage** to detect credential leaks

## Support

For issues with integration tests:
1. Verify all prerequisites are met
2. Check test output logs for detailed error messages
3. Review API documentation for endpoint changes
4. Contact development team for assistance
