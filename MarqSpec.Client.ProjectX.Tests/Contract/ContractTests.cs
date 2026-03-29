using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;

namespace MarqSpec.Client.ProjectX.Tests.Contract;

/// <summary>
/// Unit tests for contract functionality in ProjectXApiClient.
/// </summary>
public class ContractTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public ContractTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region GetContractByIdAsync Tests

    [Fact]
    public async Task GetContractByIdAsync_WithValidId_ReturnsContract()
    {
        // Arrange
        var expectedContract = new Api.Models.Contract
        {
            Id = "ESH5",
            Name = "E-Mini S&P 500 Mar 2025",
            Description = "E-Mini S&P 500 Futures",
            TickSize = 0.25m,
            TickValue = 12.50m,
            ActiveContract = true,
            SymbolId = "ES"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractByIdAsync(
                A<SearchContractByIdRequest>.That.Matches(r => r.ContractId == "ESH5"),
                A<CancellationToken>._))
            .Returns(new SearchContractByIdResponse { Success = true, ErrorCode = 0, Contract = expectedContract });

        // Act
        var result = await _client.GetContractByIdAsync("ESH5");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("ESH5");
        result.Name.Should().Be("E-Mini S&P 500 Mar 2025");
        result.TickSize.Should().Be(0.25m);
        result.TickValue.Should().Be(12.50m);
        result.ActiveContract.Should().BeTrue();
        A.CallTo(() => _restApi.SearchContractByIdAsync(
            A<SearchContractByIdRequest>.That.Matches(r => r.ContractId == "ESH5"),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetContractByIdAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractByIdAsync(A<SearchContractByIdRequest>._, A<CancellationToken>._))
            .Returns(new SearchContractByIdResponse { Success = false, ErrorCode = 1, ErrorMessage = "ContractNotFound" });

        // Act
        var result = await _client.GetContractByIdAsync("DOES_NOT_EXIST");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContractByIdAsync_WithNullOrWhitespaceId_ThrowsArgumentException()
    {
        // Act
        var actNull = async () => await _client.GetContractByIdAsync(null!);
        var actEmpty = async () => await _client.GetContractByIdAsync("   ");

        // Assert
        await actNull.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
        await actEmpty.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contract ID cannot be null or whitespace*");
    }

    [Fact]
    public async Task GetContractByIdAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractByIdAsync(A<SearchContractByIdRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable),
                new RefitSettings()));

        // Act
        var act = async () => await _client.GetContractByIdAsync("ESH5");

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 503);
    }

    [Fact]
    public async Task GetContractByIdAsync_EnsuresAuthentication()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractByIdAsync(A<SearchContractByIdRequest>._, A<CancellationToken>._))
            .Returns(new SearchContractByIdResponse { Success = true, Contract = new Api.Models.Contract { Id = "ESH5" } });

        // Act
        await _client.GetContractByIdAsync("ESH5");

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetContractAsync_DelegatesToGetContractByIdAsync()
    {
        // Arrange
        var expectedContract = new Api.Models.Contract { Id = "ESH5", Name = "E-Mini S&P 500" };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchContractByIdAsync(
                A<SearchContractByIdRequest>.That.Matches(r => r.ContractId == "ESH5"),
                A<CancellationToken>._))
            .Returns(new SearchContractByIdResponse { Success = true, Contract = expectedContract });

        // Act
        var result = await _client.GetContractAsync("ESH5");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("ESH5");
        A.CallTo(() => _restApi.SearchContractByIdAsync(A<SearchContractByIdRequest>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _restApi.SearchContractsAsync(A<SearchContractRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    #endregion

    #region GetAvailableContractsAsync Tests

    [Fact]
    public async Task GetAvailableContractsAsync_WithLiveTrue_ReturnsLiveContracts()
    {
        // Arrange
        var expectedContracts = new List<Api.Models.Contract>
        {
            new() { Id = "ESH5", Name = "E-Mini S&P 500", SymbolId = "ES" },
            new() { Id = "NQH5", Name = "E-Mini NASDAQ", SymbolId = "NQ" }
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.GetAvailableContractsAsync(
                A<ListAvailableContractRequest>.That.Matches(r => r.Live == true),
                A<CancellationToken>._))
            .Returns(new ListAvailableContractResponse { Success = true, Contracts = expectedContracts });

        // Act
        var result = await _client.GetAvailableContractsAsync(live: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedContracts);
        A.CallTo(() => _restApi.GetAvailableContractsAsync(
            A<ListAvailableContractRequest>.That.Matches(r => r.Live == true),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAvailableContractsAsync_DefaultParameter_UsesLiveTrue()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.GetAvailableContractsAsync(A<ListAvailableContractRequest>._, A<CancellationToken>._))
            .Returns(new ListAvailableContractResponse { Success = true });

        // Act
        await _client.GetAvailableContractsAsync();

        // Assert
        A.CallTo(() => _restApi.GetAvailableContractsAsync(
            A<ListAvailableContractRequest>.That.Matches(r => r.Live == true),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAvailableContractsAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.GetAvailableContractsAsync(A<ListAvailableContractRequest>._, A<CancellationToken>._))
            .Returns(new ListAvailableContractResponse { Success = false, ErrorCode = 1, ErrorMessage = "Unavailable" });

        // Act
        var act = async () => await _client.GetAvailableContractsAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve available contracts*");
    }

    [Fact]
    public async Task GetAvailableContractsAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.GetAvailableContractsAsync(A<ListAvailableContractRequest>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.GetAvailableContractsAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while retrieving available contracts*");
    }

    #endregion

    #region PingAsync Tests

    [Fact]
    public async Task PingAsync_WithPongResponse_ReturnsTrue()
    {
        // Arrange
        A.CallTo(() => _restApi.PingAsync(A<CancellationToken>._))
            .Returns("pong");

        // Act
        var result = await _client.PingAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_WithUnexpectedResponse_ReturnsFalse()
    {
        // Arrange
        A.CallTo(() => _restApi.PingAsync(A<CancellationToken>._))
            .Returns("error");

        // Act
        var result = await _client.PingAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PingAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _restApi.PingAsync(A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.PingAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while pinging*");
    }

    [Fact]
    public async Task PingAsync_DoesNotRequireAuthentication()
    {
        // Arrange
        A.CallTo(() => _restApi.PingAsync(A<CancellationToken>._))
            .Returns("pong");

        // Act
        await _client.PingAsync();

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    #endregion
}
