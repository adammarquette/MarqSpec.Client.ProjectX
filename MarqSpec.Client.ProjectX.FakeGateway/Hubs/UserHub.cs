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

    public UserHub(GatewayState state) => _state = state;

    public override Task OnConnectedAsync()
    {
        // Recording the token is how a test proves a FRESH one was fetched on reconnect rather than a stale
        // captured one being replayed — the defect that broke reconnection before.
        HubTokenRecorder.Record(Context, _state, "user");
        return base.OnConnectedAsync();
    }

    public Task SubscribeAccounts() => Record("SubscribeAccounts", null);

    public Task UnsubscribeAccounts() => Record("UnsubscribeAccounts", null);

    public Task SubscribeOrders(int accountId) => Record("SubscribeOrders", accountId.ToString());

    public Task UnsubscribeOrders(int accountId) => Record("UnsubscribeOrders", accountId.ToString());

    public Task SubscribePositions(int accountId) => Record("SubscribePositions", accountId.ToString());

    public Task UnsubscribePositions(int accountId) => Record("UnsubscribePositions", accountId.ToString());

    public Task SubscribeTrades(int accountId) => Record("SubscribeTrades", accountId.ToString());

    public Task UnsubscribeTrades(int accountId) => Record("UnsubscribeTrades", accountId.ToString());

    private Task Record(string method, string? argument)
    {
        _state.HubSubscriptions.Add($"user:{method}:{argument ?? "-"}");
        return Task.CompletedTask;
    }
}
