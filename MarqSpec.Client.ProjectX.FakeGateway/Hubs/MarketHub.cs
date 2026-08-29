using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// Stands in for <c>rtc.topstepx.com/hubs/market</c>.
/// </summary>
public sealed class MarketHub : Hub
{
    private readonly GatewayState _state;
    private readonly HubConnectionRegistry _connections;

    public MarketHub(GatewayState state, HubConnectionRegistry connections)
    {
        _state = state;
        _connections = connections;
    }

    public override Task OnConnectedAsync()
    {
        HubTokenRecorder.Record(Context, _state, "market");
        _connections.Add("market", Context);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Remove("market", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task SubscribeContractQuotes(string contractId) =>
        Record("SubscribeContractQuotes", contractId, HubGroups.MarketQuotes(contractId), join: true);

    public Task UnsubscribeContractQuotes(string contractId) =>
        Record("UnsubscribeContractQuotes", contractId, HubGroups.MarketQuotes(contractId), join: false);

    public Task SubscribeContractTrades(string contractId) =>
        Record("SubscribeContractTrades", contractId, HubGroups.MarketTrades(contractId), join: true);

    public Task UnsubscribeContractTrades(string contractId) =>
        Record("UnsubscribeContractTrades", contractId, HubGroups.MarketTrades(contractId), join: false);

    public Task SubscribeContractMarketDepth(string contractId) =>
        Record("SubscribeContractMarketDepth", contractId, HubGroups.MarketDepth(contractId), join: true);

    public Task UnsubscribeContractMarketDepth(string contractId) =>
        Record("UnsubscribeContractMarketDepth", contractId, HubGroups.MarketDepth(contractId), join: false);

    private async Task Record(string method, string argument, string group, bool join)
    {
        if (join)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        _state.HubSubscriptions.Add($"market:{method}:{argument}");
    }
}
