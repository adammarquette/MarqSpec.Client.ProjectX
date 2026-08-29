using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarqSpec.Client.ProjectX.Tests.WebSocket;

/// <summary>
/// SignalR automatic reconnect is a new connection id; server-side hub subscriptions
/// do not survive it. These tests raise the adapter's <c>Reconnected</c> callback —
/// no socket — so a silent empty stream is a failed assertion, not a quiet
/// overnight tape (gh#87, R-5.3).
/// </summary>
public class HubReconnectResubscribeTests
{
    private const string Contract = "CON.F.US.EP.Z26";
    private const string OtherContract = "CON.F.US.EP.H27";
    private const int AccountId = 42;

    [Theory]
    [InlineData("SubscribeContractQuotes")]
    [InlineData("SubscribeContractMarketDepth")]
    [InlineData("SubscribeContractTrades")]
    public async Task HandleMarketHubReconnectedAsync_ShouldReinvokeRecordedSubscriptions_WhenTheConnectionIdChanges(
        string hubMethod)
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectMarketHubAsync();
        await SubscribeMarketAsync(client, hubMethod, Contract);

        hub.Invocations.Should().ContainSingle(i => i.Method == hubMethod && ContractId(i) == Contract);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.Invocations.Where(i => i.Method == hubMethod && ContractId(i) == Contract)
            .Should()
            .HaveCount(2, "the successful subscribe must be replayed against the new connection id");
    }

    [Fact]
    public async Task HandleMarketHubReconnectedAsync_ShouldNotPublishConnected_UntilRestorationHasBeenAttempted()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        var connectedDuringRestore = false;

        hub.ConnectedDuringInvoke = invokeCount =>
        {
            if (invokeCount > 1)
            {
                connectedDuringRestore = client.MarketHubState == ConnectionState.Connected;
            }
        };

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);
        hub.InvokeCount.Should().Be(1);

        await hub.RaiseReconnectingAsync(new InvalidOperationException("connection dropped"));
        client.MarketHubState.Should().Be(ConnectionState.Reconnecting);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.InvokeCount.Should().Be(2);
        connectedDuringRestore.Should().BeFalse(
            "Connected must not be published while restore is still invoking subscribe methods");
        client.MarketHubState.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public async Task HandleMarketHubReconnectedAsync_ShouldRaiseMessageSendFailedAndNotPublishConnected_WhenAResubscribeFails()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        WebSocketMessageFailedEventArgs? failed = null;
        var statesAfterReconnect = new List<ConnectionState>();

        hub.QueueInvokeResult(Task.CompletedTask);
        hub.QueueInvokeResult(Task.FromException(new InvalidOperationException("hub rejected resubscribe")));

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);

        client.MessageSendFailed += (_, args) => failed = args;
        client.ConnectionStatusChanged += (_, change) => statesAfterReconnect.Add(change.CurrentState);

        await hub.RaiseReconnectedAsync("new-connection-id");

        failed.Should().NotBeNull();
        failed!.HubName.Should().Be("Market");
        failed.Exception.Message.Should().Contain("hub rejected resubscribe");
        statesAfterReconnect.Should().NotContain(ConnectionState.Connected);
        client.MarketHubState.Should().Be(ConnectionState.Failed);
    }

    [Fact]
    public async Task HandleMarketHubReconnectedAsync_ShouldNotReinvokeUnsubscribedContracts_WhenTheConnectionIdChanges()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);
        await client.SubscribeToPriceUpdatesAsync(OtherContract);
        await client.UnsubscribeFromPriceUpdatesAsync(Contract);

        var beforeReconnect = hub.Invocations.Count;
        await hub.RaiseReconnectedAsync("new-connection-id");
        var restored = hub.Invocations.Skip(beforeReconnect).ToList();

        restored.Should().ContainSingle(
            i => i.Method == "SubscribeContractQuotes" && ContractId(i) == OtherContract);
        restored.Should().NotContain(i => ContractId(i) == Contract);
    }

    [Fact]
    public async Task HandleMarketHubReconnectedAsync_ShouldNotReinvokeASubscribe_WhenTheOriginalInvokeFailed()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        hub.ThrowOnInvoke(new InvalidOperationException("subscribe rejected"));

        await client.ConnectMarketHubAsync();
        var act = () => client.SubscribeToPriceUpdatesAsync(Contract);
        await act.Should().ThrowAsync<InvalidOperationException>();
        hub.InvokeCount.Should().Be(1);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.InvokeCount.Should().Be(1, "a failed subscribe is not recorded and must not be replayed on reconnect");
        client.MarketHubState.Should().Be(ConnectionState.Connected);
    }

    [Theory]
    [InlineData("SubscribeOrders")]
    [InlineData("SubscribePositions")]
    [InlineData("SubscribeTrades")]
    public async Task HandleUserHubReconnectedAsync_ShouldReinvokeRecordedSubscriptions_WhenTheConnectionIdChanges(
        string hubMethod)
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectUserHubAsync();
        await SubscribeUserAsync(client, hubMethod, AccountId);

        hub.Invocations.Should().ContainSingle(i => i.Method == hubMethod && Account(i) == AccountId);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.Invocations.Where(i => i.Method == hubMethod && Account(i) == AccountId)
            .Should()
            .HaveCount(2);
    }

    [Fact]
    public async Task HandleUserHubReconnectedAsync_ShouldReinvokeSubscribeAccounts_WhenTheConnectionIdChanges()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectUserHubAsync();
        await client.SubscribeToAccountUpdatesAsync();

        hub.Invocations.Should().ContainSingle(i => i.Method == "SubscribeAccounts");

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.Invocations.Where(i => i.Method == "SubscribeAccounts").Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleUserHubReconnectedAsync_ShouldRaiseMessageSendFailedAndNotPublishConnected_WhenAResubscribeFails()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        WebSocketMessageFailedEventArgs? failed = null;
        var statesAfterReconnect = new List<ConnectionState>();

        hub.QueueInvokeResult(Task.CompletedTask);
        hub.QueueInvokeResult(Task.FromException(new InvalidOperationException("hub rejected resubscribe")));

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);

        client.MessageSendFailed += (_, args) => failed = args;
        client.ConnectionStatusChanged += (_, change) => statesAfterReconnect.Add(change.CurrentState);

        await hub.RaiseReconnectedAsync("new-connection-id");

        failed.Should().NotBeNull();
        failed!.HubName.Should().Be("User");
        failed.Exception.Message.Should().Contain("hub rejected resubscribe");
        statesAfterReconnect.Should().NotContain(ConnectionState.Connected);
        client.UserHubState.Should().Be(ConnectionState.Failed);
    }

    [Fact]
    public async Task HandleUserHubReconnectedAsync_ShouldNotPublishConnected_UntilRestorationHasBeenAttempted()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        var connectedDuringRestore = false;

        hub.ConnectedDuringInvoke = invokeCount =>
        {
            if (invokeCount > 1)
            {
                connectedDuringRestore = client.UserHubState == ConnectionState.Connected;
            }
        };

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        hub.InvokeCount.Should().Be(1);

        await hub.RaiseReconnectingAsync(new InvalidOperationException("connection dropped"));
        client.UserHubState.Should().Be(ConnectionState.Reconnecting);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.InvokeCount.Should().Be(2);
        connectedDuringRestore.Should().BeFalse(
            "Connected must not be published while restore is still invoking subscribe methods");
        client.UserHubState.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public async Task HandleUserHubReconnectedAsync_ShouldNotReinvokeUnsubscribedAccounts_WhenTheConnectionIdChanges()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        const int otherAccount = 99;

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        await client.SubscribeToOrderUpdatesAsync(otherAccount);
        await client.UnsubscribeFromOrderUpdatesAsync(AccountId);

        var beforeReconnect = hub.Invocations.Count;
        await hub.RaiseReconnectedAsync("new-connection-id");
        var restored = hub.Invocations.Skip(beforeReconnect).ToList();

        restored.Should().ContainSingle(
            i => i.Method == "SubscribeOrders" && Account(i) == otherAccount);
        restored.Should().NotContain(i => Account(i) == AccountId);
    }

    [Fact]
    public async Task HandleUserHubReconnectedAsync_ShouldNotReinvokeASubscribe_WhenTheOriginalInvokeFailed()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        hub.ThrowOnInvoke(new InvalidOperationException("subscribe rejected"));

        await client.ConnectUserHubAsync();
        var act = () => client.SubscribeToOrderUpdatesAsync(AccountId);
        await act.Should().ThrowAsync<InvalidOperationException>();
        hub.InvokeCount.Should().Be(1);

        await hub.RaiseReconnectedAsync("new-connection-id");

        hub.InvokeCount.Should().Be(1, "a failed subscribe is not recorded and must not be replayed on reconnect");
        client.UserHubState.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public async Task ConnectUserHubAsync_ShouldRestoreRecordedSubscriptionsBeforePublishingConnected_WhenCalledAfterAFailedRestore()
    {
        var first = new FakeHubConnection();
        var second = new FakeHubConnection();
        var client = CreateClient(first, second);
        var connectedDuringRestore = false;

        first.QueueInvokeResult(Task.CompletedTask);
        first.QueueInvokeResult(Task.FromException(new InvalidOperationException("hub rejected resubscribe")));

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        await first.RaiseReconnectedAsync("new-connection-id");
        client.UserHubState.Should().Be(ConnectionState.Failed);
        client.UserSubscriptions.OrderAccountIds.Should().Equal(AccountId);

        second.ConnectedDuringInvoke = _ =>
        {
            connectedDuringRestore = client.UserHubState == ConnectionState.Connected;
        };

        var statesAfterReconnect = new List<ConnectionState>();
        client.ConnectionStatusChanged += (_, change) => statesAfterReconnect.Add(change.CurrentState);

        await client.ConnectUserHubAsync();

        first.Disposed.Should().BeTrue("the hub that failed restore must not be leaked");
        second.Invocations.Should().ContainSingle(i => i.Method == "SubscribeOrders" && Account(i) == AccountId);
        connectedDuringRestore.Should().BeFalse();
        statesAfterReconnect.Should().Contain(ConnectionState.Connected);
        client.UserHubState.Should().Be(ConnectionState.Connected);
        client.UserSubscriptions.OrderAccountIds.Should().Equal(AccountId);
    }

    [Fact]
    public async Task ConnectMarketHubAsync_ShouldRestoreRecordedSubscriptionsBeforePublishingConnected_WhenCalledAfterAFailedRestore()
    {
        var first = new FakeHubConnection();
        var second = new FakeHubConnection();
        var client = CreateClient(first, second);

        first.QueueInvokeResult(Task.CompletedTask);
        first.QueueInvokeResult(Task.FromException(new InvalidOperationException("hub rejected resubscribe")));

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);
        await first.RaiseReconnectedAsync("new-connection-id");
        client.MarketHubState.Should().Be(ConnectionState.Failed);

        await client.ConnectMarketHubAsync();

        first.Disposed.Should().BeTrue();
        second.Invocations.Should().ContainSingle(i => i.Method == "SubscribeContractQuotes" && ContractId(i) == Contract);
        client.MarketHubState.Should().Be(ConnectionState.Connected);
        client.MarketSubscriptions.PriceContractIds.Should().Equal(Contract);
    }

    [Fact]
    public async Task ConnectUserHubAsync_ShouldNotPublishConnected_WhenRestoreOnTheReplacementHubFails()
    {
        var first = new FakeHubConnection();
        var second = new FakeHubConnection();
        var client = CreateClient(first, second);
        WebSocketMessageFailedEventArgs? failed = null;

        first.QueueInvokeResult(Task.CompletedTask);
        first.QueueInvokeResult(Task.FromException(new InvalidOperationException("first restore failed")));
        second.QueueInvokeResult(Task.FromException(new InvalidOperationException("second restore failed")));

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        await first.RaiseReconnectedAsync("new-connection-id");

        client.MessageSendFailed += (_, args) => failed = args;
        var act = () => client.ConnectUserHubAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*second restore failed*");

        failed.Should().NotBeNull();
        failed!.HubName.Should().Be("User");
        client.UserHubState.Should().Be(ConnectionState.Failed);
        client.UserHubState.Should().NotBe(ConnectionState.Connected);
    }

    [Fact]
    public async Task ConnectUserHubAsync_ShouldWaitForInFlightRestore_WhenCalledDuringReconnecting()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);
        var restoreGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        hub.StartCount.Should().Be(1);

        hub.QueueInvokeResult(restoreGate.Task);
        await hub.RaiseReconnectingAsync(new InvalidOperationException("connection dropped"));

        var restoreTask = hub.RaiseReconnectedAsync("new-connection-id");
        await WaitUntilAsync(() => hub.InvokeCount >= 2);

        var connectTask = client.ConnectUserHubAsync();
        await Task.Delay(50);
        connectTask.IsCompleted.Should().BeFalse("Connect* must wait on the hub lock held by restore");

        restoreGate.SetResult();
        await restoreTask;
        await connectTask;

        hub.StartCount.Should().Be(1, "a successful in-flight restore already left the hub Connected");
        client.UserHubState.Should().Be(ConnectionState.Connected);
        hub.Invocations.Where(i => i.Method == "SubscribeOrders").Should().HaveCount(2);
    }

    [Fact]
    public async Task MarketSubscriptions_ShouldIncludeContract_WhenSubscribeToPriceUpdatesAsyncSucceeds()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);

        client.MarketSubscriptions.PriceContractIds.Should().Equal(Contract);

        await client.SubscribeToOrderBookUpdatesAsync(Contract);
        await client.SubscribeToTradeUpdatesAsync(Contract);
        client.MarketSubscriptions.OrderBookContractIds.Should().Equal(Contract);
        client.MarketSubscriptions.TradeContractIds.Should().Equal(Contract);
    }

    [Fact]
    public async Task UserSubscriptions_ShouldIncludeAccount_WhenSubscribeToOrderUpdatesAsyncSucceeds()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectUserHubAsync();
        await client.SubscribeToOrderUpdatesAsync(AccountId);
        await client.SubscribeToPositionUpdatesAsync(AccountId);
        await client.SubscribeToTradeNotificationsAsync(AccountId);
        await client.SubscribeToAccountUpdatesAsync();

        client.UserSubscriptions.OrderAccountIds.Should().Equal(AccountId);
        client.UserSubscriptions.PositionAccountIds.Should().Equal(AccountId);
        client.UserSubscriptions.TradeAccountIds.Should().Equal(AccountId);
        client.UserSubscriptions.Accounts.Should().BeTrue();
    }

    [Fact]
    public async Task MarketSubscriptions_ShouldOmitContract_WhenUnsubscribeFromPriceUpdatesAsyncSucceeds()
    {
        var hub = new FakeHubConnection();
        var client = CreateClient(hub);

        await client.ConnectMarketHubAsync();
        await client.SubscribeToPriceUpdatesAsync(Contract);
        await client.UnsubscribeFromPriceUpdatesAsync(Contract);

        client.MarketSubscriptions.PriceContractIds.Should().BeEmpty();
    }

    private static ProjectXWebSocketClient CreateClient(params FakeHubConnection[] hubs)
    {
        var remaining = new Queue<FakeHubConnection>(hubs);
        var client = new ProjectXWebSocketClient(
            A.Fake<IAuthenticationService>(),
            Options.Create(new WebSocketOptions()),
            A.Fake<ILoggerFactory>(),
            A.Fake<ILogger<ProjectXWebSocketClient>>());
        client.HubConnectionFactory = _ => remaining.Dequeue();
        return client;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        condition().Should().BeTrue("the condition should become true before the wait expires");
    }

    private static Task SubscribeMarketAsync(ProjectXWebSocketClient client, string hubMethod, string contractId) =>
        hubMethod switch
        {
            "SubscribeContractQuotes" => client.SubscribeToPriceUpdatesAsync(contractId),
            "SubscribeContractMarketDepth" => client.SubscribeToOrderBookUpdatesAsync(contractId),
            "SubscribeContractTrades" => client.SubscribeToTradeUpdatesAsync(contractId),
            _ => throw new ArgumentOutOfRangeException(nameof(hubMethod), hubMethod, "Unknown market hub method.")
        };

    private static Task SubscribeUserAsync(ProjectXWebSocketClient client, string hubMethod, int accountId) =>
        hubMethod switch
        {
            "SubscribeOrders" => client.SubscribeToOrderUpdatesAsync(accountId),
            "SubscribePositions" => client.SubscribeToPositionUpdatesAsync(accountId),
            "SubscribeTrades" => client.SubscribeToTradeNotificationsAsync(accountId),
            _ => throw new ArgumentOutOfRangeException(nameof(hubMethod), hubMethod, "Unknown user hub method.")
        };

    private static string? ContractId((string Method, object?[] Args) invocation) =>
        invocation.Args.ElementAtOrDefault(0) as string;

    private static int? Account((string Method, object?[] Args) invocation) =>
        invocation.Args.ElementAtOrDefault(0) as int?;
}
