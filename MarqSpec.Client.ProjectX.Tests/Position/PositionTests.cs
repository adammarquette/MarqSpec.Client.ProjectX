using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;

namespace MarqSpec.Client.ProjectX.Tests.Position;

/// <summary>
/// Unit tests for position management functionality in ProjectXApiClient.
/// </summary>
public class PositionTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public PositionTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region GetOpenPositionsAsync Tests

    [Fact]
    public async Task GetOpenPositionsAsync_WithValidAccountId_ReturnsPositions()
    {
        // Arrange
        var expectedPositions = new List<Api.Models.Position>
        {
            new() { Id = 1, AccountId = 12345, ContractId = "ESH5", Type = PositionType.Long, Size = 2, AveragePrice = 5000.25m },
            new() { Id = 2, AccountId = 12345, ContractId = "NQH5", Type = PositionType.Short, Size = 1, AveragePrice = 21000.00m }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(
                A<SearchPositionRequest>.That.Matches(r => r.AccountId == 12345),
                A<CancellationToken>._))
            .Returns(new SearchPositionResponse { Success = true, Positions = expectedPositions });

        // Act
        var result = await _client.GetOpenPositionsAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedPositions);
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WithNoOpenPositions_ReturnsEmptyCollection()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(A<SearchPositionRequest>._, A<CancellationToken>._))
            .Returns(new SearchPositionResponse { Success = true, Positions = [] });

        // Act
        var result = await _client.GetOpenPositionsAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.GetOpenPositionsAsync(0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(A<SearchPositionRequest>._, A<CancellationToken>._))
            .Returns(new SearchPositionResponse { Success = false, ErrorCode = 1, ErrorMessage = "AccountNotFound" });

        // Act
        var act = async () => await _client.GetOpenPositionsAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve open positions*");
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(A<SearchPositionRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized),
                new RefitSettings()));

        // Act
        var act = async () => await _client.GetOpenPositionsAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(A<SearchPositionRequest>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.GetOpenPositionsAsync(12345);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while retrieving open positions*");
    }

    [Fact]
    public async Task GetOpenPositionsAsync_PositionPropertiesAreMappedCorrectly()
    {
        // Arrange
        var expectedPosition = new Api.Models.Position
        {
            Id = 99,
            AccountId = 12345,
            ContractId = "ESH5",
            ContractDisplayName = "E-Mini S&P 500 Mar 2025",
            CreationTimestamp = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Type = PositionType.Long,
            Size = 3,
            AveragePrice = 5050.75m
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchOpenPositionsAsync(A<SearchPositionRequest>._, A<CancellationToken>._))
            .Returns(new SearchPositionResponse { Success = true, Positions = [expectedPosition] });

        // Act
        var result = await _client.GetOpenPositionsAsync(12345);

        // Assert
        var position = result.Should().ContainSingle().Subject;
        position.Id.Should().Be(99);
        position.AccountId.Should().Be(12345);
        position.ContractId.Should().Be("ESH5");
        position.ContractDisplayName.Should().Be("E-Mini S&P 500 Mar 2025");
        position.Type.Should().Be(PositionType.Long);
        position.Size.Should().Be(3);
        position.AveragePrice.Should().Be(5050.75m);
    }

    #endregion

    #region ClosePositionAsync Tests

    [Fact]
    public async Task ClosePositionAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.CloseContractPositionAsync(
                A<CloseContractPositionRequest>.That.Matches(r => r.AccountId == 12345 && r.ContractId == "ESH5"),
                A<CancellationToken>._))
            .Returns(new ClosePositionResponse { Success = true, ErrorCode = 0 });

        // Act
        var result = await _client.ClosePositionAsync(12345, "ESH5");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        A.CallTo(() => _restApi.CloseContractPositionAsync(
            A<CloseContractPositionRequest>.That.Matches(r => r.AccountId == 12345 && r.ContractId == "ESH5"),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ClosePositionAsync_WithRejectedResponse_ReturnsFailureResponse()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.CloseContractPositionAsync(A<CloseContractPositionRequest>._, A<CancellationToken>._))
            .Returns(new ClosePositionResponse { Success = false, ErrorCode = 5, ErrorMessage = "OrderRejected" });

        // Act
        var result = await _client.ClosePositionAsync(12345, "ESH5");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(5);
    }

    [Fact]
    public async Task ClosePositionAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.ClosePositionAsync(0, "ESH5");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task ClosePositionAsync_WithEmptyContractId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.ClosePositionAsync(12345, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task ClosePositionAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.CloseContractPositionAsync(A<CloseContractPositionRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest),
                new RefitSettings()));

        // Act
        var act = async () => await _client.ClosePositionAsync(12345, "ESH5");

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 400);
    }

    [Fact]
    public async Task ClosePositionAsync_EnsuresAuthentication()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.CloseContractPositionAsync(A<CloseContractPositionRequest>._, A<CancellationToken>._))
            .Returns(new ClosePositionResponse { Success = true });

        // Act
        await _client.ClosePositionAsync(12345, "ESH5");

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    #endregion

    #region PartialClosePositionAsync Tests

    [Fact]
    public async Task PartialClosePositionAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PartialCloseContractPositionAsync(
                A<PartialCloseContractPositionRequest>.That.Matches(r =>
                    r.AccountId == 12345 && r.ContractId == "ESH5" && r.Size == 1),
                A<CancellationToken>._))
            .Returns(new PartialClosePositionResponse { Success = true, ErrorCode = 0 });

        // Act
        var result = await _client.PartialClosePositionAsync(12345, "ESH5", 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        A.CallTo(() => _restApi.PartialCloseContractPositionAsync(
            A<PartialCloseContractPositionRequest>.That.Matches(r =>
                r.AccountId == 12345 && r.ContractId == "ESH5" && r.Size == 1),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PartialClosePositionAsync_WithInvalidAccountId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.PartialClosePositionAsync(0, "ESH5", 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account ID must be greater than zero*");
    }

    [Fact]
    public async Task PartialClosePositionAsync_WithEmptyContractId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.PartialClosePositionAsync(12345, "", 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task PartialClosePositionAsync_WithZeroSize_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _client.PartialClosePositionAsync(12345, "ESH5", 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Size must be greater than zero*");
    }

    [Fact]
    public async Task PartialClosePositionAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PartialCloseContractPositionAsync(A<PartialCloseContractPositionRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest),
                new RefitSettings()));

        // Act
        var act = async () => await _client.PartialClosePositionAsync(12345, "ESH5", 1);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 400);
    }

    [Fact]
    public async Task PartialClosePositionAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.PartialCloseContractPositionAsync(A<PartialCloseContractPositionRequest>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.PartialClosePositionAsync(12345, "ESH5", 1);

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while partially closing position*");
    }

    #endregion
}
