using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.WebSocket;

namespace MarqSpec.Client.ProjectX.Tests.WebSocket;

public class WebSocketMessageSendFailedTests
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ProjectXWebSocketClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WebSocketOptions _options;

    public WebSocketMessageSendFailedTests()
    {
        _authService = A.Fake<IAuthenticationService>();
        _logger = A.Fake<ILogger<ProjectXWebSocketClient>>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        _options = new WebSocketOptions();

        A.CallTo(() => _authService.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-token");
    }

    [Fact]
    public void SubscribeToPriceUpdatesAsync_WhenNotConnected_ThrowsAndDoesNotRaiseEvent()
    {
        // Arrange
        var client = CreateClient();
        WebSocketMessageFailedEventArgs? receivedArgs = null;
        client.MessageSendFailed += (_, args) => receivedArgs = args;

        // Act
        var act = async () => await client.SubscribeToPriceUpdatesAsync("CON1");

        // Assert — throws InvalidOperationException because not connected (before InvokeAsync)
        act.Should().ThrowAsync<InvalidOperationException>();
        receivedArgs.Should().BeNull("event should only fire on InvokeAsync failures, not precondition checks");
    }

    [Fact]
    public void UnsubscribeFromPriceUpdatesAsync_WhenNotConnected_ThrowsAndDoesNotRaiseEvent()
    {
        // Arrange
        var client = CreateClient();
        WebSocketMessageFailedEventArgs? receivedArgs = null;
        client.MessageSendFailed += (_, args) => receivedArgs = args;

        // Act
        var act = async () => await client.UnsubscribeFromPriceUpdatesAsync("CON1");

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
        receivedArgs.Should().BeNull();
    }

    [Fact]
    public void SubscribeToOrderBookUpdatesAsync_WhenNotConnected_ThrowsAndDoesNotRaiseEvent()
    {
        // Arrange
        var client = CreateClient();
        WebSocketMessageFailedEventArgs? receivedArgs = null;
        client.MessageSendFailed += (_, args) => receivedArgs = args;

        // Act
        var act = async () => await client.SubscribeToOrderBookUpdatesAsync("CON1");

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
        receivedArgs.Should().BeNull();
    }

    [Fact]
    public void SubscribeToOrderUpdatesAsync_WhenNotConnected_ThrowsAndDoesNotRaiseEvent()
    {
        // Arrange
        var client = CreateClient();
        WebSocketMessageFailedEventArgs? receivedArgs = null;
        client.MessageSendFailed += (_, args) => receivedArgs = args;

        // Act
        var act = async () => await client.SubscribeToOrderUpdatesAsync(12345);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
        receivedArgs.Should().BeNull();
    }

    [Fact]
    public void SubscribeToPriceUpdatesAsync_WithNullContractId_ThrowsArgumentException()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var act = async () => await client.SubscribeToPriceUpdatesAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentException>().WithMessage("*Contract ID*");
    }

    [Fact]
    public void SubscribeToOrderUpdatesAsync_WithZeroAccountId_ThrowsArgumentException()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var act = async () => await client.SubscribeToOrderUpdatesAsync(0);

        // Assert
        act.Should().ThrowAsync<ArgumentException>().WithMessage("*Account ID*");
    }

    [Fact]
    public void WebSocketMessageFailedEventArgs_HasCorrectProperties()
    {
        // Arrange & Act
        var args = new WebSocketMessageFailedEventArgs
        {
            HubName = "Market",
            MethodName = "SubscribeToPrices",
            Arguments = ["CON1"],
            Exception = new InvalidOperationException("test error")
        };

        // Assert
        args.HubName.Should().Be("Market");
        args.MethodName.Should().Be("SubscribeToPrices");
        args.Arguments.Should().ContainSingle().Which.Should().Be("CON1");
        args.Exception.Message.Should().Be("test error");
        args.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private ProjectXWebSocketClient CreateClient()
    {
        return new ProjectXWebSocketClient(
            _authService,
            Options.Create(_options),
            _loggerFactory,
            _logger);
    }
}
