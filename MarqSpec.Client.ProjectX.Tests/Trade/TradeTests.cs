using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;

namespace MarqSpec.Client.ProjectX.Tests.Trade;

/// <summary>
/// Unit tests for trade search functionality in ProjectXApiClient.
/// </summary>
public class TradeTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public TradeTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region GetTradesAsync Tests

    [Fact]
    public async Task GetTradesAsync_WithValidAccountId_ReturnsTrades()
    {
        // Arrange
        var expectedTrades = new List<HalfTrade>
        {
            new() { Id = 1001, AccountId = 12345, ContractId = "ESH5", Price = 5000.25m, Size = 2, Side = OrderSide.Bid, Fees = 4.50m, Voided = false, OrderId = 98765 },
            new() { Id = 1002, AccountId = 12345, ContractId = "ESH5", Price = 5010.00m, Size = 2, Side = OrderSide.Ask, Fees = 4.50m, Voided = false, OrderId = 98766 }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(
                A<SearchTradeRequest>.That.Matches(r => r.AccountId == 12345 && r.StartTimestamp == null && r.EndTimestamp == null),
                A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = true, Trades = expectedTrades });

        // Act
        var result = await _client.GetTradesAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedTrades);
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTradesAsync_WithTimeRange_PassesTimestampsToRequest()
    {
        // Arrange
        var startTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(
                A<SearchTradeRequest>.That.Matches(r =>
                    r.AccountId == 12345 &&
                    r.StartTimestamp == startTime &&
                    r.EndTimestamp == endTime),
                A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = true, Trades = [] });

        // Act
        await _client.GetTradesAsync(12345, startTime, endTime);

        // Assert
        A.CallTo(() => _restApi.SearchTradesAsync(
            A<SearchTradeRequest>.That.Matches(r =>
                r.AccountId == 12345 &&
                r.StartTimestamp == startTime &&
                r.EndTimestamp == endTime),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTradesAsync_WithEmptyResult_ReturnsEmptyCollection()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = true, Trades = [] });

        // Act
        var result = await _client.GetTradesAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTradesAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetTradesAsync(0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task GetTradesAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = false, ErrorCode = 1, ErrorMessage = "AccountNotFound" });

        // Act
        var act = async () => await _client.GetTradesAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve trades*");
    }

    [Fact]
    public async Task GetTradesAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized),
                new RefitSettings()));

        // Act
        var act = async () => await _client.GetTradesAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task GetTradesAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.GetTradesAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while retrieving trades*");
    }

    [Fact]
    public async Task GetTradesAsync_TradePropertiesAreMappedCorrectly()
    {
        // Arrange
        var expectedTrade = new HalfTrade
        {
            Id = 5001,
            AccountId = 12345,
            ContractId = "ESH5",
            CreationTimestamp = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc),
            Price = 5025.50m,
            ProfitAndLoss = 125.00m,
            Fees = 2.25m,
            Side = OrderSide.Ask,
            Size = 1,
            Voided = false,
            OrderId = 77001
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = true, Trades = [expectedTrade] });

        // Act
        var result = await _client.GetTradesAsync(12345);

        // Assert
        var trade = result.Should().ContainSingle().Subject;
        trade.Id.Should().Be(5001);
        trade.AccountId.Should().Be(12345);
        trade.ContractId.Should().Be("ESH5");
        trade.Price.Should().Be(5025.50m);
        trade.ProfitAndLoss.Should().Be(125.00m);
        trade.Fees.Should().Be(2.25m);
        trade.Side.Should().Be(OrderSide.Ask);
        trade.Size.Should().Be(1);
        trade.Voided.Should().BeFalse();
        trade.OrderId.Should().Be(77001);
    }

    [Fact]
    public async Task GetTradesAsync_EnsuresAuthentication()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchTradesAsync(A<SearchTradeRequest>._, A<CancellationToken>._))
            .Returns(new SearchTradeResponse { Success = true });

        // Act
        await _client.GetTradesAsync(12345);

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    #endregion
}
