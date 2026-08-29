using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// Stands in for <c>rtc.topstepx.com/hubs/user</c>.
/// </summary>
/// <remarks>
/// The subscribe methods must exist and return, because the client calls them with <c>InvokeAsync</c> — which
/// waits for a result and would fault if the method were missing. Each call is recorded so a test can assert
/// that reconnect actually re-subscribed rather than merely reconnecting.
/// </remarks>
public sealed class UserHub : Hub
{
    private readonly GatewayState _state;
    private readonly HubConnectionRegistry _connections;

    public UserHub(GatewayState state, HubConnectionRegistry connections)
    {
        _state = state;
        _connections = connections;
    }

    public override Task OnConnectedAsync()
    {
        // Recording the token is how a test proves a FRESH one was fetched on reconnect rather than a stale
        // captured one being replayed — the defect that broke reconnection before.
        HubTokenRecorder.Record(Context, _state, "user");
        _connections.Add("user", Context);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Remove("user", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task SubscribeAccounts() => Record("SubscribeAccounts", null, HubGroups.UserAccounts(), join: true);

    public Task UnsubscribeAccounts() => Record("UnsubscribeAccounts", null, HubGroups.UserAccounts(), join: false);

    public Task SubscribeOrders(int accountId) =>
        Record("SubscribeOrders", accountId.ToString(), HubGroups.UserOrders(accountId), join: true);

    public Task UnsubscribeOrders(int accountId) =>
        Record("UnsubscribeOrders", accountId.ToString(), HubGroups.UserOrders(accountId), join: false);

    public Task SubscribePositions(int accountId) =>
        Record("SubscribePositions", accountId.ToString(), HubGroups.UserPositions(accountId), join: true);

    public Task UnsubscribePositions(int accountId) =>
        Record("UnsubscribePositions", accountId.ToString(), HubGroups.UserPositions(accountId), join: false);

    public Task SubscribeTrades(int accountId) =>
        Record("SubscribeTrades", accountId.ToString(), HubGroups.UserTrades(accountId), join: true);

    public Task UnsubscribeTrades(int accountId) =>
        Record("UnsubscribeTrades", accountId.ToString(), HubGroups.UserTrades(accountId), join: false);

    private async Task Record(string method, string? argument, string group, bool join)
    {
        if (join)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        _state.HubSubscriptions.Add($"user:{method}:{argument ?? "-"}");
    }
}
