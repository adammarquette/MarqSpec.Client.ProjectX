# Future Date Validation for GetHistoricalBarsAsync

## Overview
Added validation to `GetHistoricalBarsAsync` method to prevent requesting historical data with future dates, as historical data by definition cannot exist in the future.

## Changes Made

### 1. Implementation Changes
**File**: `MarqSpec.Client.ProjectX\ProjectXApiClient.cs`

Added two new validation checks in the `GetHistoricalBarsAsync` method:
- Validates that `startTime` is not in the future
- Validates that `endTime` is not in the future

```csharp
if (startTime > DateTime.UtcNow)
{
    throw new ArgumentException("Start time cannot be in the future.", nameof(startTime));
}

if (endTime > DateTime.UtcNow)
{
    throw new ArgumentException("End time cannot be in the future.", nameof(endTime));
}
```

**Validation Order**:
1. Contract ID validation (not null/whitespace)
2. Unit number validation (> 0)
3. Limit validation (> 0)
4. Start time before end time validation
5. **NEW**: Start time not in future validation
6. **NEW**: End time not in future validation

### 2. Unit Tests Added
**File**: `MarqSpec.Client.ProjectX.Tests\MarketData\MarketDataTests.cs`

Added 4 new unit tests:
1. ✅ `GetHistoricalBarsAsync_WithStartTimeInFuture_ThrowsArgumentException` - Validates rejection when start time is in the future
2. ✅ `GetHistoricalBarsAsync_WithEndTimeInFuture_ThrowsArgumentException` - Validates rejection when end time is in the future
3. ✅ `GetHistoricalBarsAsync_WithBothTimesInFuture_ThrowsArgumentException` - Validates rejection when both times are in the future
4. ✅ (Existing) `GetHistoricalBarsAsync_WithStartTimeAfterEndTime_ThrowsArgumentException` - Validates rejection when start > end

**Test Results**: All 10 GetHistoricalBarsAsync unit tests pass (including 3 new tests)

### 3. Integration Tests Added
**File**: `MarqSpec.Client.ProjectX.Tests\Integration\MarketDataIntegrationTests.cs`

Added 2 new integration tests:
1. ✅ `GetHistoricalBarsAsync_WithStartTimeInFuture_ThrowsArgumentException` - Integration test for future start time validation
2. ✅ `GetHistoricalBarsAsync_WithEndTimeInFuture_ThrowsArgumentException` - Integration test for future end time validation

**Test Results**: Both new integration tests pass

## Test Results Summary

### Unit Tests
```
Total tests: 10
     Passed: 10
     Failed: 0
 Total time: 1.3 seconds
```

### New Integration Tests
```
Total tests: 2
     Passed: 2
     Failed: 0
 Total time: 0.9 seconds
```

## Validation Behavior

### Valid Request
```csharp
var yesterday = DateTime.UtcNow.Date.AddDays(-1);
var startTime = yesterday;
var endTime = yesterday.AddDays(1);

// ✅ Valid - Both times are in the past
var bars = await client.GetHistoricalBarsAsync(
    contractId, startTime, endTime, AggregateBarUnit.Hour);
```

### Invalid Requests
```csharp
// ❌ Throws ArgumentException: "Start time cannot be in the future"
var startTime = DateTime.UtcNow.AddDays(1);
var endTime = DateTime.UtcNow.AddDays(2);
await client.GetHistoricalBarsAsync(contractId, startTime, endTime, AggregateBarUnit.Hour);

// ❌ Throws ArgumentException: "End time cannot be in the future"
var startTime = DateTime.UtcNow.AddDays(-1);
var endTime = DateTime.UtcNow.AddDays(1);
await client.GetHistoricalBarsAsync(contractId, startTime, endTime, AggregateBarUnit.Hour);
```

## Rationale

Historical market data represents past trading activity. Requesting data for future dates is logically invalid because:
1. The data doesn't exist yet
2. It would cause unnecessary API calls that will always fail
3. It indicates a logical error in the calling code that should be caught early
4. It provides clear, actionable error messages to developers

This validation catches programming errors at the client level before making expensive API calls, improving developer experience and reducing unnecessary network traffic.

## Related Updates

This change complements the recent update where all `GetHistoricalBarsAsync` integration tests were modified to use "previous day" time ranges instead of current/trailing dates, ensuring tests request data that should definitely exist in the system.

## Date
January 8, 2025

## Build Status
✅ All changes compiled successfully
✅ All unit tests pass
✅ All new integration tests pass
