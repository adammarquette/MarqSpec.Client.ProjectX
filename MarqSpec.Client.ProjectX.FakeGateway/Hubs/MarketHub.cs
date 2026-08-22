using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// Stands in for <c>rtc.topstepx.com/hubs/market</c>.
/// </summary>
public sealed class MarketHub : Hub
{
    private readonly GatewayState _state;

    public MarketHub(GatewayState state) => _state = state;

    public override Task OnConnectedAsync()
    {
        HubTokenRecorder.Record(Context, _state, "market");
        return base.OnConnectedAsync();
    }

    public Task SubscribeContractQuotes(string contractId) => Record("SubscribeContractQuotes", contractId);

    public Task UnsubscribeContractQuotes(string contractId) => Record("UnsubscribeContractQuotes", contractId);

    public Task SubscribeContractTrades(string contractId) => Record("SubscribeContractTrades", contractId);

    public Task UnsubscribeContractTrades(string contractId) => Record("UnsubscribeContractTrades", contractId);

    public Task SubscribeContractMarketDepth(string contractId) => Record("SubscribeContractMarketDepth", contractId);

    public Task UnsubscribeContractMarketDepth(string contractId) => Record("UnsubscribeContractMarketDepth", contractId);

    private Task Record(string method, string argument)
    {
        _state.HubSubscriptions.Add($"market:{method}:{argument}");
        return Task.CompletedTask;
    }
}
