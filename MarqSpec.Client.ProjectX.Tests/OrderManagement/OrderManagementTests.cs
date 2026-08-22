using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;

namespace MarqSpec.Client.ProjectX.Tests.OrderManagement;

/// <summary>
/// Unit tests for order management functionality in ProjectXApiClient.
/// </summary>
public class OrderManagementTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public OrderManagementTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region PlaceOrderAsync Tests

    [Fact]
    public async Task PlaceOrderAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1,
            LimitPrice = 5000.00m
        };

        var expectedResponse = new PlaceOrderResponse
        {
            Success = true,
            ErrorCode = 0,
            OrderId = 98765
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PlaceOrderAsync(request, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OrderId.Should().Be(98765);
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _restApi.PlaceOrderAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PlaceOrderAsync_WithFailedResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1,
            LimitPrice = 5000.00m
        };

        var expectedResponse = new PlaceOrderResponse
        {
            Success = false,
            ErrorCode = 1001,
            ErrorMessage = "Insufficient margin"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PlaceOrderAsync(request, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(1001);
        result.ErrorMessage.Should().Be("Insufficient margin");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _client.PlaceOrderAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 0,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1
        };

        // Act
        var act = async () => await _client.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithEmptyContractId_ThrowsArgumentException()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1
        };

        // Act
        var act = async () => await _client.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithInvalidSize_ThrowsArgumentException()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 0
        };

        // Act
        var act = async () => await _client.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Order size must be greater than zero*");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PlaceOrderAsync(request, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest),
                new RefitSettings()));

        // Act
        var act = async () => await _client.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 400);
    }

    [Fact]
    public async Task PlaceOrderAsync_EnsuresAuthentication()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            AccountId = 12345,
            ContractId = "ESH5",
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PlaceOrderAsync(request, A<CancellationToken>._))
            .Returns(new PlaceOrderResponse { Success = true });

        // Act
        await _client.PlaceOrderAsync(request);

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    #endregion

    #region ModifyOrderAsync Tests

    [Fact]
    public async Task ModifyOrderAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new ModifyOrderRequest
        {
            AccountId = 12345,
            OrderId = 98765,
            LimitPrice = 5010.00m,
            Size = 2
        };

        var expectedResponse = new ModifyOrderResponse
        {
            Success = true,
            ErrorCode = 0
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.ModifyOrderAsync(request, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.ModifyOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        A.CallTo(() => _restApi.ModifyOrderAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ModifyOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _client.ModifyOrderAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task ModifyOrderAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Arrange
        var request = new ModifyOrderRequest
        {
            AccountId = 0,
            OrderId = 98765
        };

        // Act
        var act = async () => await _client.ModifyOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task ModifyOrderAsync_WithInvalidOrderId_ThrowsArgumentException()
    {
        // Arrange
        var request = new ModifyOrderRequest
        {
            AccountId = 12345,
            OrderId = 0
        };

        // Act
        var act = async () => await _client.ModifyOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Order ID must be greater than zero*");
    }

    #endregion

    #region CancelOrderAsync Tests

    [Fact]
    public async Task CancelOrderAsync_WithValidParameters_ReturnsSuccessResponse()
    {
        // Arrange
        int accountId = 12345;
        long orderId = 98765;

        var expectedResponse = new CancelOrderResponse
        {
            Success = true,
            ErrorCode = 0
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.CancelOrderAsync(
            A<CancelOrderRequest>.That.Matches(r => r.AccountId == accountId && r.OrderId == orderId),
            A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.CancelOrderAsync(accountId, orderId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelOrderAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.CancelOrderAsync(0, 98765);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task CancelOrderAsync_WithInvalidOrderId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.CancelOrderAsync(12345, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Order ID must be greater than zero*");
    }

    #endregion

    #region GetOrderAsync Tests

    // gh#57 -- the windowless overload carries [Obsolete(error: true)], so source cannot call it at all: the
    // compiler raises CS0619, which is an error and cannot be pragma-suppressed. That compile break is the
    // primary guarantee, and it is asserted below by reflecting on the attribute.
    //
    // The runtime guard still matters, because a consumer assembly compiled against 1.0.x binds to this method
    // and keeps calling it after a package upgrade. Reflection is how that caller reaches it, so reflection is
    // how the test does.
    [Fact]
    public void GetOrderAsync_ShouldBeMarkedObsoleteAsError_WhenNoWindowIsAccepted()
    {
        var method = typeof(IProjectXApiClient).GetMethod(
            nameof(IProjectXApiClient.GetOrderAsync),
            [typeof(int), typeof(long), typeof(CancellationToken)]);

        var obsolete = method!.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();

        obsolete.Should().NotBeNull();
        obsolete!.IsError.Should().BeTrue();
        obsolete.Message.Should().Contain("startTimestamp");
    }

    [Fact]
    public void GetOrderAsync_ShouldThrowNotSupported_WhenAPreviouslyCompiledCallerInvokesIt()
    {
        // Arrange: the binding an assembly compiled against 1.0.x already holds.
        var method = typeof(ProjectXApiClient).GetMethod(
            nameof(ProjectXApiClient.GetOrderAsync),
            [typeof(int), typeof(long), typeof(CancellationToken)]);

        // Act
        var act = () => method!.Invoke(_client, [12345, 98765L, CancellationToken.None]);

        // Assert
        var assertion = act.Should().Throw<TargetInvocationException>();
        assertion.WithInnerException<NotSupportedException>()
            .Which.Message.Should().Contain("search window");
        A.CallTo(() => _restApi.SearchOrdersAsync(A<SearchOrderRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetOrderAsync_WithExistingOrder_ReturnsOrder()
    {
        // Arrange
        int accountId = 12345;
        long orderId = 98765;

        var expectedOrder = new Order
        {
            Id = orderId,
            AccountId = accountId,
            ContractId = "ESH5",
            SymbolId = "ES",
            Status = OrderStatus.Open,
            Type = OrderType.Limit,
            Side = OrderSide.Bid,
            Size = 1,
            FillVolume = 0,
            CreationTimestamp = DateTime.UtcNow
        };

        var searchResponse = new SearchOrderResponse
        {
            Success = true,
            ErrorCode = 0,
            Orders = new List<Order> { expectedOrder }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        var startTime = DateTime.UtcNow.AddDays(-1);
        A.CallTo(() => _restApi.SearchOrdersAsync(
            A<SearchOrderRequest>.That.Matches(r => r.AccountId == accountId && r.StartTime == startTime),
            A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetOrderAsync(accountId, orderId, startTime);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task GetOrderAsync_WithNonExistingOrder_ReturnsNull()
    {
        // Arrange
        int accountId = 12345;
        long orderId = 98765;

        var searchResponse = new SearchOrderResponse
        {
            Success = true,
            ErrorCode = 0,
            Orders = new List<Order>()
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOrdersAsync(
            A<SearchOrderRequest>._,
            A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetOrderAsync(accountId, orderId, DateTime.UtcNow.AddDays(-1));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetOrderAsync(0, 98765, DateTime.UtcNow.AddDays(-1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    #endregion

    #region GetOrdersAsync Tests

    [Fact]
    public async Task GetOrdersAsync_WithValidParameters_ReturnsOrders()
    {
        // Arrange
        int accountId = 12345;
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;

        var expectedOrders = new List<Order>
        {
            new Order
            {
                Id = 1,
                AccountId = accountId,
                ContractId = "ESH5",
                SymbolId = "ES",
                Status = OrderStatus.Filled,
                Type = OrderType.Market,
                Side = OrderSide.Bid,
                Size = 1,
                FillVolume = 1,
                CreationTimestamp = DateTime.UtcNow.AddDays(-1)
            },
            new Order
            {
                Id = 2,
                AccountId = accountId,
                ContractId = "NQH5",
                SymbolId = "NQ",
                Status = OrderStatus.Cancelled,
                Type = OrderType.Limit,
                Side = OrderSide.Ask,
                Size = 2,
                FillVolume = 0,
                CreationTimestamp = DateTime.UtcNow.AddDays(-2)
            }
        };

        var searchResponse = new SearchOrderResponse
        {
            Success = true,
            ErrorCode = 0,
            Orders = expectedOrders
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOrdersAsync(
            A<SearchOrderRequest>.That.Matches(r =>
                r.AccountId == accountId &&
                r.StartTime == startTime &&
                r.EndTime == endTime),
            A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetOrdersAsync(accountId, startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);
    }

    [Fact]
    public async Task GetOrdersAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetOrdersAsync(0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    // gh#57 -- the gateway's /api/Order/search schema marks startTimestamp REQUIRED. Sending null let the
    // gateway pick a window, and an order outside it came back absent: a live order reported as not placed.
    // A window the client invents silently would reproduce exactly that, so the call fails instead.

    [Fact]
    public async Task GetOrdersAsync_ShouldThrowArgumentException_WhenNoStartTimeIsSupplied()
    {
        // Act
        var act = async () => await _client.GetOrdersAsync(12345);

        // Assert
        var assertion = await act.Should().ThrowAsync<ArgumentException>();
        assertion.Which.Message.Should().Contain("startTimestamp");
        assertion.Which.ParamName.Should().Be("startTime");
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldNotCallTheGateway_WhenNoStartTimeIsSupplied()
    {
        // Act
        var act = async () => await _client.GetOrdersAsync(12345, null, DateTime.UtcNow);
        await act.Should().ThrowAsync<ArgumentException>();

        // Assert: it must fail before the request, not send a request the schema rejects.
        A.CallTo(() => _restApi.SearchOrdersAsync(A<SearchOrderRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldSendTheWindowOnTheWire_WhenStartTimeIsSupplied()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-7);
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).Returns("test-token");
        A.CallTo(() => _restApi.SearchOrdersAsync(A<SearchOrderRequest>._, A<CancellationToken>._))
            .Returns(new SearchOrderResponse { Success = true, ErrorCode = 0, Orders = [] });

        // Act
        await _client.GetOrdersAsync(12345, startTime);

        // Assert
        A.CallTo(() => _restApi.SearchOrdersAsync(
            A<SearchOrderRequest>.That.Matches(r => r.StartTime == startTime),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    #endregion

    #region GetOpenOrdersAsync Tests

    [Fact]
    public async Task GetOpenOrdersAsync_WithValidAccountId_ReturnsOpenOrders()
    {
        // Arrange
        int accountId = 12345;

        var expectedOrders = new List<Order>
        {
            new Order
            {
                Id = 1,
                AccountId = accountId,
                ContractId = "ESH5",
                SymbolId = "ES",
                Status = OrderStatus.Open,
                Type = OrderType.Limit,
                Side = OrderSide.Bid,
                Size = 1,
                FillVolume = 0,
                CreationTimestamp = DateTime.UtcNow
            },
            new Order
            {
                Id = 2,
                AccountId = accountId,
                ContractId = "NQH5",
                SymbolId = "NQ",
                Status = OrderStatus.Pending,
                Type = OrderType.Limit,
                Side = OrderSide.Ask,
                Size = 3,
                FillVolume = 1,
                CreationTimestamp = DateTime.UtcNow
            }
        };

        var searchResponse = new SearchOrderResponse
        {
            Success = true,
            ErrorCode = 0,
            Orders = expectedOrders
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenOrdersAsync(A<SearchOpenOrderRequest>.That.Matches(r => r.AccountId == accountId), A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var result = await _client.GetOpenOrdersAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.AccountId == accountId);
    }

    [Fact]
    public async Task GetOpenOrdersAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetOpenOrdersAsync(0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        int accountId = 12345;

        var searchResponse = new SearchOrderResponse
        {
            Success = false,
            ErrorCode = 500,
            ErrorMessage = "Internal server error"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenOrdersAsync(A<SearchOpenOrderRequest>.That.Matches(r => r.AccountId == accountId), A<CancellationToken>._))
            .Returns(searchResponse);

        // Act
        var act = async () => await _client.GetOpenOrdersAsync(accountId);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve open orders*");
    }

    #endregion
}
