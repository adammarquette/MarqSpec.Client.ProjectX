using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;
using ContractModel = MarqSpec.Client.ProjectX.Api.Models.Contract;

namespace MarqSpec.Client.ProjectX.Tests.MarketData;

/// <summary>
/// Unit tests for market data functionality (contracts and historical bars) in ProjectXApiClient.
/// </summary>
public class MarketDataTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public MarketDataTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region SearchContractsAsync Tests

    [Fact]
    public async Task SearchContractsAsync_WithValidCriteria_ReturnsContracts()
    {
        // Arrange
        var expectedContracts = new List<ContractModel>
        {
            new ContractModel
            {
                Id = "ESH5",
                Name = "E-mini S&P 500 Mar 2025",
                Description = "E-mini S&P 500 Futures",
                TickSize = 0.25m,
                TickValue = 12.50m,
                ActiveContract = true,
                SymbolId = "ES"
            },
            new ContractModel
            {
                Id = "ESM5",
                Name = "E-mini S&P 500 Jun 2025",
                Description = "E-mini S&P 500 Futures",
                TickSize = 0.25m,
                TickValue = 12.50m,
                ActiveContract = true,
                SymbolId = "ES"
            }
        };

        var searchResponse = new SearchContractResponse
        {
            Success = true,
            ErrorCode = 0,
            Contracts = expectedContracts
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(
            A<SearchContractRequest>.That.Matches(r => r.SearchText == "ES" && r.Live == true),
            A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.SearchContractsAsync("ES", true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be("ESH5");
        result.Last().Id.Should().Be("ESM5");
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchContractsAsync_WithNullSearchText_ReturnsAllContracts()
    {
        // Arrange
        var searchResponse = new SearchContractResponse
        {
            Success = true,
            ErrorCode = 0,
            Contracts = new List<ContractModel> { new ContractModel { Id = "ESH5" } }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.SearchContractsAsync(null, true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchContractsAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        var searchResponse = new SearchContractResponse
        {
            Success = false,
            ErrorCode = 500,
            ErrorMessage = "Internal server error"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var act = async () => await _client.SearchContractsAsync("ES");

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to search contracts*")
            .Where(ex => ex.StatusCode == 500);
    }

    [Fact]
    public async Task SearchContractsAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest),
                new RefitSettings()));

        // Act
        var act = async () => await _client.SearchContractsAsync("ES");

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 400);
    }

    #endregion

    #region GetContractAsync Tests

    [Fact]
    public async Task GetContractAsync_WithExistingContract_ReturnsContract()
    {
        // Arrange
        var expectedContract = new ContractModel
        {
            Id = "ESH5",
            Name = "E-mini S&P 500 Mar 2025",
            Description = "E-mini S&P 500 Futures",
            TickSize = 0.25m,
            TickValue = 12.50m,
            ActiveContract = true,
            SymbolId = "ES"
        };

        var searchResponse = new SearchContractResponse
        {
            Success = true,
            ErrorCode = 0,
            Contracts = new List<ContractModel> { expectedContract }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetContractAsync("ESH5");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("ESH5");
        result.Name.Should().Be("E-mini S&P 500 Mar 2025");
        result.TickSize.Should().Be(0.25m);
    }

    [Fact]
    public async Task GetContractAsync_WithNonExistingContract_ReturnsNull()
    {
        // Arrange
        var searchResponse = new SearchContractResponse
        {
            Success = true,
            ErrorCode = 0,
            Contracts = new List<ContractModel>()
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetContractAsync("INVALID");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContractAsync_WithNullContractId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetContractAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task GetContractAsync_WithEmptyContractId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetContractAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    #endregion

    #region GetHistoricalBarsAsync Tests

    [Fact]
    public async Task GetHistoricalBarsAsync_WithValidParameters_ReturnsBars()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;
        var expectedBars = new List<AggregateBar>
        {
            new AggregateBar
            {
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Open = 4500.00m,
                High = 4505.00m,
                Low = 4498.00m,
                Close = 4502.00m,
                Volume = 10000
            },
            new AggregateBar
            {
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Open = 4502.00m,
                High = 4508.00m,
                Low = 4500.00m,
                Close = 4506.00m,
                Volume = 12000
            }
        };

        var barResponse = new RetrieveBarResponse
        {
            Success = true,
            ErrorCode = 0,
            Bars = expectedBars
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.RetrieveBarsAsync(
            A<RetrieveBarRequest>.That.Matches(r => 
                r.ContractId == "ESH5" && 
                r.Unit == AggregateBarUnit.Minute &&
                r.UnitNumber == 5),
            A<CancellationToken>._))
            .Returns(barResponse);

        // Act
        var result = await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute, 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Open.Should().Be(4500.00m);
        result.Last().Close.Should().Be(4506.00m);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithNullContractId_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow;

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            null!, startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithInvalidUnitNumber_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow;

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unit number must be greater than zero*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithInvalidLimit_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow;

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute, 1, -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Limit must be greater than zero*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithStartTimeAfterEndTime_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time must be before end time*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithStartTimeInFuture_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = DateTime.UtcNow.AddDays(2);

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time cannot be in the future*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithEndTimeInFuture_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow.AddDays(1);

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time cannot be in the future*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithBothTimesInFuture_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = DateTime.UtcNow.AddHours(2);

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "ESH5", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time cannot be in the future*");
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow;
        var barResponse = new RetrieveBarResponse
        {
            Success = false,
            ErrorCode = 404,
            ErrorMessage = "Contract not found"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.RetrieveBarsAsync(A<RetrieveBarRequest>._, A<CancellationToken>._))
            .Returns(barResponse);

        // Act
        var act = async () => await _client.GetHistoricalBarsAsync(
            "INVALID", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve historical bars*")
            .Where(ex => ex.StatusCode == 404);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_EnsuresAuthentication()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = DateTime.UtcNow;
        var barResponse = new RetrieveBarResponse
        {
            Success = true,
            ErrorCode = 0,
            Bars = new List<AggregateBar>()
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.RetrieveBarsAsync(A<RetrieveBarRequest>._, A<CancellationToken>._))
            .Returns(barResponse);

        // Act
        await _client.GetHistoricalBarsAsync("ESH5", startTime, endTime, AggregateBarUnit.Minute);

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    #endregion
}
