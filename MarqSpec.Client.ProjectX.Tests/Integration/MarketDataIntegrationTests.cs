using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Exceptions;

namespace MarqSpec.Client.ProjectX.Tests.Integration;

/// <summary>
/// Integration tests for market data functionality (contracts and historical bars).
/// </summary>
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
public class MarketDataIntegrationTests : IntegrationTestBase
{
    #region Contract Search Tests

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task SearchContractsAsync_WithValidSearchText_ReturnsMatchingContracts()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Act - Get all contracts first to see what's available
        var allContracts = await Client.SearchContractsAsync(null, live: true);

        if (!allContracts.Any())
        {
            // Skip test if no contracts available
            return;
        }

        // Use the first contract's symbol for searching
        var testSymbol = allContracts.First().SymbolId;
        var contracts = await Client.SearchContractsAsync(testSymbol, live: true);

        // Assert
        contracts.Should().NotBeNull();
        contracts.Should().NotBeEmpty();
        contracts.Should().AllSatisfy(contract =>
        {
            contract.Id.Should().NotBeNullOrEmpty();
            contract.SymbolId.Should().Contain(testSymbol);
            contract.Name.Should().NotBeNullOrEmpty();
            contract.TickSize.Should().BeGreaterThan(0);
            contract.TickValue.Should().BeGreaterThan(0);
        });
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task SearchContractsAsync_WithNullSearchText_ReturnsAllContracts()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Act - Get all live contracts
        var contracts = await Client.SearchContractsAsync(null, live: true);

        // Assert
        contracts.Should().NotBeNull();
        if (contracts.Any())
        {
            contracts.Should().AllSatisfy(contract =>
            {
                contract.Id.Should().NotBeNullOrEmpty();
                contract.SymbolId.Should().NotBeNullOrEmpty();
                contract.Name.Should().NotBeNullOrEmpty();
            });
        }
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task SearchContractsAsync_WithUnmatchedSearchText_ReturnsEmptyList()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Act
        var contracts = await Client.SearchContractsAsync("NONEXISTENT_SYMBOL_12345", live: true);

        // Assert
        contracts.Should().NotBeNull();
        contracts.Should().BeEmpty("no contracts should match the search text");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task SearchContractsAsync_ConcurrentCalls_AreThreadSafe()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Act - Multiple concurrent search requests for all contracts
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Client.SearchContractsAsync(null, live: true))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(contracts =>
        {
            contracts.Should().NotBeNull();
        });

        // If we got results, they should be consistent
        if (results[0].Any())
        {
            var firstResultIds = results[0].Select(c => c.Id).OrderBy(id => id).ToList();
            foreach (var result in results.Skip(1))
            {
                var resultIds = result.Select(c => c.Id).OrderBy(id => id).ToList();
                resultIds.Should().BeEquivalentTo(firstResultIds);
            }
        }
    }

    #endregion

    #region Get Contract Tests

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetContractAsync_WithValidContractId_ReturnsContract()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - First search for a valid contract
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var validContractId = contracts.First().Id;

        // Act
        var contract = await Client.GetContractAsync(validContractId);

        // Assert
        contract.Should().NotBeNull();
        contract!.Id.Should().Be(validContractId);
        contract.Name.Should().NotBeNullOrEmpty();
        contract.Description.Should().NotBeNullOrEmpty();
        contract.SymbolId.Should().NotBeNullOrEmpty();
        contract.TickSize.Should().BeGreaterThan(0);
        contract.TickValue.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetContractAsync_WithInvalidContractId_ReturnsNull()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Act
        var contract = await Client.GetContractAsync("INVALID_CONTRACT_ID_12345");

        // Assert
        contract.Should().BeNull("contract should not exist");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetContractAsync_CalledMultipleTimes_ReturnsSameData()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - First search for a valid contract
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var validContractId = contracts.First().Id;

        // Act - Call multiple times
        var contract1 = await Client.GetContractAsync(validContractId);
        var contract2 = await Client.GetContractAsync(validContractId);
        var contract3 = await Client.GetContractAsync(validContractId);

        // Assert
        contract1.Should().NotBeNull();
        contract2.Should().NotBeNull();
        contract3.Should().NotBeNull();

        contract1!.Id.Should().Be(contract2!.Id).And.Be(contract3!.Id);
        contract1.Name.Should().Be(contract2.Name).And.Be(contract3.Name);
        contract1.TickSize.Should().Be(contract2.TickSize).And.Be(contract3.TickSize);
    }

    #endregion

    #region Historical Bars Tests

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithValidParameters_ReturnsBars()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday;
        var endTime = yesterday.AddDays(1);

        // Act
        var bars = await Client.GetHistoricalBarsAsync(
            contractId,
            startTime,
            endTime,
            AggregateBarUnit.Hour,
            unitNumber: 1,
            limit: 100);

        // Assert
        bars.Should().NotBeNull();
        bars.Should().NotBeEmpty("should have historical data for the previous day");
        bars.Should().AllSatisfy(bar =>
        {
            bar.Timestamp.Should().BeOnOrAfter(startTime);
            bar.Timestamp.Should().BeOnOrBefore(endTime);
            bar.Open.Should().BeGreaterThan(0);
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Open);
            bar.Low.Should().BeLessThanOrEqualTo(bar.Open);
            bar.Close.Should().BeGreaterThan(0);
            bar.Volume.Should().BeGreaterOrEqualTo(0);
            
            // High should be >= all other prices
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Low);
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Close);
            
            // Low should be <= all other prices
            bar.Low.Should().BeLessThanOrEqualTo(bar.High);
            bar.Low.Should().BeLessThanOrEqualTo(bar.Close);
        });

        // Bars should be ordered by timestamp
        var timestamps = bars.Select(b => b.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder();
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithDifferentTimeframes_ReturnsAppropriateData()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday;
        var endTime = yesterday.AddHours(12);

        // Act - Get different timeframes
        var bars1Min = await Client.GetHistoricalBarsAsync(
            contractId, startTime, endTime, AggregateBarUnit.Minute, 1, limit: 50);
        var bars5Min = await Client.GetHistoricalBarsAsync(
            contractId, startTime, endTime, AggregateBarUnit.Minute, 5, limit: 50);
        var bars1Hour = await Client.GetHistoricalBarsAsync(
            contractId, startTime, endTime, AggregateBarUnit.Hour, 1, limit: 50);

        // Assert
        bars1Min.Should().NotBeEmpty();
        bars5Min.Should().NotBeEmpty();
        bars1Hour.Should().NotBeEmpty();

        // 1-minute bars should have more data points than 5-minute bars
        bars1Min.Count().Should().BeGreaterThan(bars5Min.Count(),
            "1-minute bars should have more data points than 5-minute bars");

        // 5-minute bars should have more data points than 1-hour bars
        bars5Min.Count().Should().BeGreaterThan(bars1Hour.Count(),
            "5-minute bars should have more data points than 1-hour bars");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithLimitParameter_RespectsLimit()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day and prior days
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday.AddDays(-30);
        var endTime = yesterday.AddDays(1);
        var limit = 10;

        // Act
        var bars = await Client.GetHistoricalBarsAsync(
            contractId,
            startTime,
            endTime,
            AggregateBarUnit.Day,
            unitNumber: 1,
            limit: limit);

        // Assert
        bars.Should().NotBeNull();
        bars.Count().Should().BeLessThanOrEqualTo(limit, "API should respect the limit parameter");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithInvalidContractId_ThrowsException()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddDays(-1);

        // Act
        var act = async () => await Client.GetHistoricalBarsAsync(
            "INVALID_CONTRACT_ID_12345",
            startTime,
            endTime,
            AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve historical bars*");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithStartTimeInFuture_ThrowsArgumentException()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = DateTime.UtcNow.AddDays(2);

        // Act
        var act = async () => await Client.GetHistoricalBarsAsync(
            "ESH5",
            startTime,
            endTime,
            AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time cannot be in the future*");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_WithEndTimeInFuture_ThrowsArgumentException()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow.AddDays(1);

        // Act
        var act = async () => await Client.GetHistoricalBarsAsync(
            "ESH5",
            startTime,
            endTime,
            AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time cannot be in the future*");
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_ConcurrentCalls_AreThreadSafe()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday;
        var endTime = yesterday.AddDays(1);

        // Act - Multiple concurrent requests
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Client.GetHistoricalBarsAsync(
                contractId,
                startTime,
                endTime,
                AggregateBarUnit.Hour,
                1))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(bars =>
        {
            bars.Should().NotBeNull();
            bars.Should().NotBeEmpty();
        });

        // All results should be consistent
        var firstResultTimestamps = results[0].Select(b => b.Timestamp).OrderBy(t => t).ToList();
        foreach (var result in results.Skip(1))
        {
            var resultTimestamps = result.Select(b => b.Timestamp).OrderBy(t => t).ToList();
            resultTimestamps.Should().BeEquivalentTo(firstResultTimestamps);
        }
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task GetHistoricalBarsAsync_PerformanceTest_MeetsRequirements()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday;
        var endTime = yesterday.AddDays(1);

        // Act - Measure multiple requests
        var durations = new List<TimeSpan>();
        for (int i = 0; i < 10; i++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await Client.GetHistoricalBarsAsync(
                contractId,
                startTime,
                endTime,
                AggregateBarUnit.Hour,
                1,
                limit: 100);
            stopwatch.Stop();
            durations.Add(stopwatch.Elapsed);
        }

        // Assert
        var p95Duration = durations.OrderBy(d => d).ElementAt((int)(durations.Count * 0.95));
        p95Duration.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "95th percentile should be under 5 seconds for historical data");

        var averageDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
        averageDuration.Should().BeLessThan(TimeSpan.FromSeconds(3),
            "average request should be under 3 seconds");
    }

    #endregion

    #region Data Validation Tests

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task ContractData_RemainsConsistent_AcrossSearchAndGet()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange & Act - Search for contracts
        var searchResults = await Client.SearchContractsAsync(null, live: true);
        if (!searchResults.Any())
        {
            return; // Skip test if no contracts available
        }

        var contractFromSearch = searchResults.First();
        var contractFromGet = await Client.GetContractAsync(contractFromSearch.Id);

        // Assert - Data should match
        contractFromGet.Should().NotBeNull();
        contractFromGet!.Id.Should().Be(contractFromSearch.Id);
        contractFromGet.Name.Should().Be(contractFromSearch.Name);
        contractFromGet.SymbolId.Should().Be(contractFromSearch.SymbolId);
        contractFromGet.TickSize.Should().Be(contractFromSearch.TickSize);
        contractFromGet.TickValue.Should().Be(contractFromSearch.TickValue);
        contractFromGet.ActiveContract.Should().Be(contractFromSearch.ActiveContract);
    }

    [Fact(Skip = "Requires live API credentials in environment variables")]
    public async Task HistoricalBars_HaveValidOHLCRelationships()
    {
        // Skip if credentials not available
        if (SkipReason != null) return;

        // Arrange - Get a valid contract first
        var contracts = await Client.SearchContractsAsync(null, live: true);
        if (!contracts.Any())
        {
            return; // Skip test if no contracts available
        }
        var contractId = contracts.First().Id;

        // Use previous complete trading day and prior days
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var startTime = yesterday.AddDays(-7);
        var endTime = yesterday.AddDays(1);

        // Act
        var bars = await Client.GetHistoricalBarsAsync(
            contractId,
            startTime,
            endTime,
            AggregateBarUnit.Day,
            1);

        // Assert - Validate OHLC relationships
        bars.Should().NotBeEmpty();
        bars.Should().AllSatisfy(bar =>
        {
            // High must be the highest price
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Open,
                $"High ({bar.High}) should be >= Open ({bar.Open}) at {bar.Timestamp}");
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Low,
                $"High ({bar.High}) should be >= Low ({bar.Low}) at {bar.Timestamp}");
            bar.High.Should().BeGreaterThanOrEqualTo(bar.Close,
                $"High ({bar.High}) should be >= Close ({bar.Close}) at {bar.Timestamp}");

            // Low must be the lowest price
            bar.Low.Should().BeLessThanOrEqualTo(bar.Open,
                $"Low ({bar.Low}) should be <= Open ({bar.Open}) at {bar.Timestamp}");
            bar.Low.Should().BeLessThanOrEqualTo(bar.High,
                $"Low ({bar.Low}) should be <= High ({bar.High}) at {bar.Timestamp}");
            bar.Low.Should().BeLessThanOrEqualTo(bar.Close,
                $"Low ({bar.Low}) should be <= Close ({bar.Close}) at {bar.Timestamp}");

            // All prices should be positive
            bar.Open.Should().BeGreaterThan(0);
            bar.High.Should().BeGreaterThan(0);
            bar.Low.Should().BeGreaterThan(0);
            bar.Close.Should().BeGreaterThan(0);
        });
    }

    #endregion
}
