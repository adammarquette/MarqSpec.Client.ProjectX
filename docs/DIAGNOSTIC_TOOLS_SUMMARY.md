# Contract Search Diagnostic Tools - Implementation Summary

## What Was Created

### 1. **Comprehensive Diagnostic Console Application** ✨

**Location**: `MarqSpec.Client.ProjectX.Diagnostics/`

A full .NET 10 console application that systematically tests the ProjectX API contract search endpoint.

**Features**:
- Tests 10+ different search parameter combinations
- Automatic credential detection (environment variables, appsettings, integration config)
- Color-coded output (green=success, yellow=warning, red=error)
- Detailed error reporting and troubleshooting guidance
- Performance measurements
- Summary statistics

**Files**:
- `Program.cs` - Main diagnostic logic (comprehensive, production-quality)
- `MarqSpec.Client.ProjectX.Diagnostics.csproj` - Project file
- `appsettings.json` - Configuration template
- `README.md` - Complete documentation

### 2. **Quick Diagnostic Scripts**

**DiagnoseContracts.cs**
- Quick and simple top-level program
- Tests basic scenarios
- Easier to run for quick checks

**TestContractSearch.csx** (already existed, kept as-is)
- C# script version
- Can be run with `dotnet script`

### 3. **Launch Scripts**

**run-diagnostics.bat** (Windows)
```batch
run-diagnostics.bat
```

**run-diagnostics.sh** (Linux/Mac)
```bash
chmod +x run-diagnostics.sh
./run-diagnostics.sh
```

## How to Use

### Quick Start

**Option 1: Run from Visual Studio**
1. Right-click `MarqSpec.Client.ProjectX.Diagnostics` project
2. Select "Set as Startup Project"
3. Press F5 or Ctrl+F5

**Option 2: Run from command line**
```bash
# From solution root
cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run
```

**Option 3: Use the launcher script**
```bash
# Windows
run-diagnostics.bat

# Linux/Mac
./run-diagnostics.sh
```

### Setting Credentials

The tool checks credentials in this order:
1. Environment variables: `PROJECTX_API_KEY`, `PROJECTX_API_SECRET`
2. `MarqSpec.Client.ProjectX.Tests/appsettings.integration.json`
3. `MarqSpec.Client.ProjectX.Diagnostics/appsettings.json`

**Recommended**: Use existing integration test config:
```bash
# No setup needed if you already have:
# MarqSpec.Client.ProjectX.Tests/appsettings.integration.json
```

## What Gets Tested

| Test # | Search Parameter | Purpose |
|--------|-----------------|---------|
| 1 | `searchText=null, live=true` | All live contracts |
| 2 | `searchText=null, live=false` | All contracts (including expired) |
| 3 | `searchText="", live=true` | Empty string search |
| 4 | `searchText="ES", live=true` | E-mini S&P 500 |
| 5 | `searchText="ES", live=false` | E-mini S&P 500 (with expired) |
| 6 | `searchText="NQ", live=true` | E-mini NASDAQ |
| 7 | `searchText="YM", live=true` | E-mini Dow Jones |
| 8 | `searchText="CL", live=true` | Crude Oil futures |
| 9 | `searchText="GC", live=true` | Gold futures |
| 10 | `searchText="ZB", live=true` | Treasury Bond futures |
| 11 | GetContractAsync | Tests specific contract retrieval (if any found) |

## Expected Outcomes

### Scenario 1: Success - Contracts Found ✓

```
✓ SUCCESS - Found 42 contracts (234ms)

First 3 contracts:
  • ID: ESH25               Symbol: ES         Name: E-mini S&P 500 Mar 2025
  • ID: NQH25               Symbol: NQ         Name: E-mini NASDAQ Mar 2025
  • ID: YMH25               Symbol: YM         Name: E-mini Dow Mar 2025

Summary:
  Total Tests:           11
  Successful (no error): 11
  Tests with contracts:  10
  Failed Tests:          0

  ✓ API is working - contracts were found!
```

**Next Steps**:
- ✅ API is working correctly
- Update integration tests to use same parameters
- Document which search terms work
- Use found contract IDs in tests

### Scenario 2: Empty Results - No Contracts ⚠

```
⚠ SUCCESS but EMPTY - 0 contracts returned (156ms)

⚠ WARNING: NO CONTRACTS FOUND IN ANY TEST!

Possible causes:
  1. Account permissions - your account may not have access to contract data
  2. API endpoint restrictions - contract endpoint may require elevated access
  3. Empty database - test environment may not have contract data
  4. Market hours - some contracts may only be available during trading hours

Summary:
  Total Tests:           10
  Successful (no error): 10
  Tests with contracts:  0
  Failed Tests:          0

  ⚠ API is responding but returning empty results
```

**Next Steps**:
1. Log into ProjectX web portal - verify contracts are visible in UI
2. Contact ProjectX support - verify account has API access
3. Test in Swagger UI: https://api.topstepx.com/swagger
4. Check subscription/account tier requirements
5. Consider using mock data for integration tests

### Scenario 3: API Errors ✗

```
✗ FAILED - ProjectXApiException: Unauthorized

Summary:
  Total Tests:           10
  Successful (no error): 0
  Tests with contracts:  0
  Failed Tests:          10

  ✗ API errors detected
```

**Next Steps**:
1. Verify API credentials are correct
2. Check account is active
3. Regenerate API key if needed
4. Test authentication separately

## Integration with PRD Goals

This diagnostic tool directly addresses:

✅ **PRD Investigation Goal**: "Investigate API Contract Access"
- Systematically tests all scenarios
- Provides clear troubleshooting path
- Documents findings for support tickets

✅ **PRD Documentation Goal**: "Well-documented with usage examples"
- Comprehensive README
- Clear output messages
- Step-by-step troubleshooting

✅ **PRD Error Handling Goal**: "Clear and concise error handling"
- Color-coded output
- Specific error messages
- Actionable recommendations

## Troubleshooting Guide

### Problem: "API credentials not found"
**Solution**: Set credentials in one of the three supported locations

### Problem: "Unauthorized" errors
**Solution**: 
- Verify credentials are correct
- Check account is active
- Try regenerating API key

### Problem: No contracts found but no errors
**Solution**:
- Check account permissions in web portal
- Contact ProjectX support
- Verify subscription tier

### Problem: Network errors
**Solution**:
- Check internet connectivity
- Verify firewall settings
- Check corporate proxy settings

## Next Steps After Running Diagnostics

### If Contracts Are Found ✓
1. Document which search parameters work
2. Update integration tests with working parameters
3. Add found contract IDs to test fixtures
4. Proceed with WebSocket implementation

### If No Contracts Found ⚠
1. **Parallel Track**: Proceed with WebSocket implementation (not blocked)
2. **Investigation Track**: 
   - Contact ProjectX support with diagnostic results
   - Test in web portal
   - Verify account permissions
3. **Testing Track**: Add mock data for CI/CD integration tests

### If API Errors ✗
1. Fix authentication/authorization issues first
2. Re-run diagnostics after fix
3. Document resolution for team

## Files Created

```
MarqSpec.Client.ProjectX.Diagnostics/
├── Program.cs                        # Main diagnostic application
├── MarqSpec.Client.ProjectX.Diagnostics.csproj
├── appsettings.json                  # Configuration template
└── README.md                         # Comprehensive documentation

DiagnoseContracts.cs                  # Quick diagnostic script (updated)
TestContractSearch.csx                # C# script version (existing)
run-diagnostics.bat                   # Windows launcher
run-diagnostics.sh                    # Linux/Mac launcher
DIAGNOSTIC_TOOLS_SUMMARY.md          # This file
```

## Recommended Immediate Action

**Run the diagnostic now**:

```bash
# Windows
run-diagnostics.bat

# Linux/Mac
chmod +x run-diagnostics.sh
./run-diagnostics.sh

# Or manually
cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run
```

This will immediately tell you:
1. ✅ Is the API accessible?
2. ✅ Are credentials working?
3. ✅ Which search parameters return data?
4. ✅ What contract IDs are available?
5. ✅ Is this an account permissions issue?

---

## Summary

You now have:
- ✅ Professional diagnostic console application
- ✅ Quick diagnostic scripts
- ✅ Easy-to-use launcher scripts
- ✅ Comprehensive documentation
- ✅ Clear troubleshooting guidance
- ✅ Next steps based on results

**Time to Resolution**: Run the diagnostic (2-3 minutes) to get definitive answers about the contract API issue.
