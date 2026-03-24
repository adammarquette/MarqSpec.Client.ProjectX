# 🎯 QUICK START: Run Contract Diagnostics Now

## ⚡ 30-Second Quick Start

### Windows
```bash
run-diagnostics.bat
```

### Linux/Mac  
```bash
chmod +x run-diagnostics.sh
./run-diagnostics.sh
```

### Or Manual
```bash
cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run
```

## 🔧 Credentials Setup

**Already have integration tests configured?**
✅ **No setup needed!** Tool auto-detects from:
- `MarqSpec.Client.ProjectX.Tests/appsettings.integration.json`

**Or use environment variables:**
```bash
# Windows PowerShell
$env:PROJECTX_API_KEY = "your-api-key"
$env:PROJECTX_API_SECRET = "your-api-secret"

# Linux/Mac
export PROJECTX_API_KEY="your-api-key"
export PROJECTX_API_SECRET="your-api-secret"
```

## 📊 Understanding the Output

### ✅ SUCCESS (Green)
```
✓ SUCCESS - Found 42 contracts (234ms)
  • ID: ESH25      Symbol: ES    Name: E-mini S&P 500 Mar 2025
```
**Meaning**: API is working! 🎉
**Action**: Use these contract IDs in your tests → Proceed to WebSocket

### ⚠️ EMPTY (Yellow)
```
⚠ SUCCESS but EMPTY - 0 contracts returned (156ms)
```
**Meaning**: API responds but account has no contract data
**Action**: Contact support, proceed to WebSocket in parallel

### ❌ ERROR (Red)
```
✗ FAILED - Unauthorized
```
**Meaning**: Authentication/authorization issue
**Action**: Check credentials, verify account is active

## 🚀 Next Steps Based on Results

| Result | What It Means | What To Do |
|--------|---------------|------------|
| **Contracts Found** ✅ | API working! | → Update tests, implement WebSocket |
| **Empty Results** ⚠️ | Account access issue | → Contact support + WebSocket parallel |
| **API Errors** ❌ | Auth problem | → Fix credentials first |

## 📁 What You Got

```
MarqSpec.Client.ProjectX.Diagnostics/  ← Complete diagnostic app
run-diagnostics.bat                    ← Windows quick launcher
run-diagnostics.sh                     ← Linux/Mac quick launcher  
DiagnoseContracts.cs                   ← Simple diagnostic script
DIAGNOSTIC_TOOLS_SUMMARY.md            ← Full implementation details
NEXT_STEPS_PRIORITY_1.md               ← Detailed next steps guide
```

## 📚 More Info

- **Full Documentation**: `MarqSpec.Client.ProjectX.Diagnostics/README.md`
- **Implementation Details**: `DIAGNOSTIC_TOOLS_SUMMARY.md`
- **Next Steps Guide**: `NEXT_STEPS_PRIORITY_1.md`

---

## 🎯 Priority 1: COMPLETE ✅

✅ Professional diagnostic tool created
✅ Tests 10+ scenarios automatically  
✅ Auto-detects credentials
✅ Color-coded output with troubleshooting
✅ Full documentation included

**Run it now to unblock your integration tests!**

```bash
run-diagnostics.bat
```
