using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Api.Rest;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Exceptions;
using Microsoft.Extensions.Logging;
using Refit;

namespace MarqSpec.Client.ProjectX.Tests.Account;

/// <summary>
/// Unit tests for account functionality in ProjectXApiClient.
/// </summary>
public class AccountTests
{
    private readonly IProjectXRestApi _restApi;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXApiClient> _logger;
    private readonly ProjectXApiClient _client;

    public AccountTests()
    {
        _restApi = A.Fake<IProjectXRestApi>();
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXApiClient>>();
        _client = new ProjectXApiClient(_restApi, _authService, _logger);
    }

    #region GetAccountsAsync Tests

    [Fact]
    public async Task GetAccountsAsync_WithActiveAccountsOnly_ReturnsActiveAccounts()
    {
        // Arrange
        var expectedAccounts = new List<TradingAccount>
        {
            new() { Id = 1, Name = "Sim Account 1", Balance = 50000m, CanTrade = true, IsVisible = true, Simulated = true },
            new() { Id = 2, Name = "Sim Account 2", Balance = 100000m, CanTrade = true, IsVisible = true, Simulated = true }
        };

        var expectedResponse = new SearchAccountResponse
        {
            Success = true,
            ErrorCode = 0,
            Accounts = expectedAccounts
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(
                A<SearchAccountRequest>.That.Matches(r => r.OnlyActiveAccounts == true),
                A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.GetAccountsAsync(onlyActiveAccounts: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedAccounts);
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _restApi.SearchAccountsAsync(
            A<SearchAccountRequest>.That.Matches(r => r.OnlyActiveAccounts == true),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAccountsAsync_WithAllAccounts_PassesFalseToRequest()
    {
        // Arrange
        var expectedResponse = new SearchAccountResponse
        {
            Success = true,
            ErrorCode = 0,
            Accounts = [new() { Id = 1, Name = "Sim Account 1", Balance = 50000m, CanTrade = false, IsVisible = true, Simulated = true }]
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(
                A<SearchAccountRequest>.That.Matches(r => r.OnlyActiveAccounts == false),
                A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.GetAccountsAsync(onlyActiveAccounts: false);

        // Assert
        result.Should().HaveCount(1);
        A.CallTo(() => _restApi.SearchAccountsAsync(
            A<SearchAccountRequest>.That.Matches(r => r.OnlyActiveAccounts == false),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAccountsAsync_DefaultParameter_UsesOnlyActiveAccounts()
    {
        // Arrange
        var expectedResponse = new SearchAccountResponse
        {
            Success = true,
            ErrorCode = 0,
            Accounts = []
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        await _client.GetAccountsAsync();

        // Assert
        A.CallTo(() => _restApi.SearchAccountsAsync(
            A<SearchAccountRequest>.That.Matches(r => r.OnlyActiveAccounts == true),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAccountsAsync_WithEmptyResult_ReturnsEmptyCollection()
    {
        // Arrange
        var expectedResponse = new SearchAccountResponse
        {
            Success = true,
            ErrorCode = 0,
            Accounts = []
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Returns(expectedResponse);

        // Act
        var result = await _client.GetAccountsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountsAsync_WithFailedResponse_ThrowsProjectXApiException()
    {
        // Arrange
        var failedResponse = new SearchAccountResponse
        {
            Success = false,
            ErrorCode = 1,
            ErrorMessage = "Unexpected server error"
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Returns(failedResponse);

        // Act
        var act = async () => await _client.GetAccountsAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Failed to retrieve accounts*");
    }

    [Fact]
    public async Task GetAccountsAsync_WithApiException_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Throws(await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized),
                new RefitSettings()));

        // Act
        var act = async () => await _client.GetAccountsAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task GetAccountsAsync_WithNetworkError_ThrowsProjectXApiException()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await _client.GetAccountsAsync();

        // Assert
        await act.Should().ThrowAsync<ProjectXApiException>()
            .WithMessage("*Network error occurred while retrieving accounts*");
    }

    [Fact]
    public async Task GetAccountsAsync_EnsuresAuthentication()
    {
        // Arrange
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Returns(new SearchAccountResponse { Success = true });

        // Act
        await _client.GetAccountsAsync();

        // Assert
        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAccountsAsync_AccountPropertiesAreMappedCorrectly()
    {
        // Arrange
        var expectedAccount = new TradingAccount
        {
            Id = 42,
            Name = "My Sim Account",
            Balance = 75000.50m,
            CanTrade = true,
            IsVisible = true,
            Simulated = true
        };

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
        A.CallTo(() => _restApi.SearchAccountsAsync(A<SearchAccountRequest>._, A<CancellationToken>._))
            .Returns(new SearchAccountResponse { Success = true, Accounts = [expectedAccount] });

        // Act
        var result = await _client.GetAccountsAsync();

        // Assert
        var account = result.Should().ContainSingle().Subject;
        account.Id.Should().Be(42);
        account.Name.Should().Be("My Sim Account");
        account.Balance.Should().Be(75000.50m);
        account.CanTrade.Should().BeTrue();
        account.IsVisible.Should().BeTrue();
        account.Simulated.Should().BeTrue();
    }

    #endregion
}
