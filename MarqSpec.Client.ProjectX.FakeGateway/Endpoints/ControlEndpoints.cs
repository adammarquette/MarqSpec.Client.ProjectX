using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.FakeGateway.Hubs;
using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Endpoints;

/// <summary>
/// The scenario surface: everything a test needs to drive the fake deterministically.
/// </summary>
/// <remarks>
/// Deliberately outside <c>/api</c> so it is neither authenticated nor subject to fault injection — a test
/// arming a fault must not have that fault consumed by the arming call itself.
/// </remarks>
public static class ControlEndpoints
{
    public static void MapControlEndpoints(this WebApplication app)
    {
        app.MapPost("/_control/reset", (GatewayState state, HubConnectionRegistry connections) =>
        {
            connections.AbortAll();
            state.Reset();
            return Results.Ok(new { reset = true });
        });

        app.MapGet("/_control/state", (GatewayState state, HubConnectionRegistry connections) => Results.Ok(new
        {
            requestCounts = state.RequestCounts,
            hubTokensSeen = state.HubTokensSeen.Keys,
            hubSubscriptions = state.HubSubscriptions,
            hubConnections = new
            {
                market = connections.ConnectionIds("market"),
                user = connections.ConnectionIds("user"),
            },
            orders = state.Orders.Count,
            positions = state.Positions.Count,
            trades = state.Trades.Count,
        }));

        // How many times a route was actually hit. This is what proves a retry happened - or, for
        // /api/Order/place, that it did NOT (ADR-0002).
        app.MapGet("/_control/requests", (GatewayState state, string path) =>
            Results.Ok(new { path, count = state.RequestCounts.GetValueOrDefault(path, 0) }));

        app.MapPost("/_control/fault", (FaultDirective directive, GatewayState state) =>
        {
            state.Mutate<object?>(() => { state.Faults.Add(directive); return null; });
            return Results.Ok(directive);
        });

        // Drops the named hub's sockets so SignalR automatic reconnect gets a new connection id and
        // group membership is gone. Market and user are independent — aborting one must not touch the other.
        app.MapPost("/_control/abort/{hub}", (string hub, HubConnectionRegistry connections) =>
        {
            if (hub is not ("market" or "user"))
            {
                return Results.BadRequest(new { error = "hub must be 'market' or 'user'" });
            }

            var connectionIds = connections.ConnectionIds(hub);
            var aborted = connections.Abort(hub);
            return Results.Ok(new { hub, aborted, connectionIds });
        });

        app.MapPost("/_control/emit/order", (OrderUpdate update, IHubContext<UserHub> hub) =>
            EmitToGroup(hub, "GatewayUserOrder", HubGroups.UserOrders(update.AccountId), update));

        app.MapPost("/_control/emit/position", (PositionUpdate update, IHubContext<UserHub> hub) =>
            EmitToGroup(hub, "GatewayUserPosition", HubGroups.UserPositions(update.AccountId), update));

        app.MapPost("/_control/emit/account", (AccountUpdate update, IHubContext<UserHub> hub) =>
            EmitToGroup(hub, "GatewayUserAccount", HubGroups.UserAccounts(), update));

        app.MapPost("/_control/emit/trade-notification", (TradeNotification notification, IHubContext<UserHub> hub) =>
            EmitToGroup(hub, "GatewayUserTrade", HubGroups.UserTrades(notification.AccountId), notification));

        // The market hub's handlers take (contractId, payload); the array-shaped ones matter because
        // GatewayTrade and GatewayDepth deserialize as arrays - a single object silently failed to bind
        // before that was fixed. Emit to the subscribe group, not All: All would deliver after a
        // connection-id change even if the client never re-subscribed (gh#87).
        app.MapPost("/_control/emit/quote", async (QuoteEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.Group(HubGroups.MarketQuotes(emission.ContractId))
                .SendAsync("GatewayQuote", emission.ContractId, emission.Update);
            return Results.Ok(new { emitted = "GatewayQuote" });
        });

        app.MapPost("/_control/emit/market-trade", async (MarketTradeEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.Group(HubGroups.MarketTrades(emission.ContractId))
                .SendAsync("GatewayTrade", emission.ContractId, emission.Updates);
            return Results.Ok(new { emitted = "GatewayTrade", count = emission.Updates.Length });
        });

        app.MapPost("/_control/emit/depth", async (DepthEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.Group(HubGroups.MarketDepth(emission.ContractId))
                .SendAsync("GatewayDepth", emission.ContractId, emission.Updates);
            return Results.Ok(new { emitted = "GatewayDepth", count = emission.Updates.Length });
        });

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    }

    private static async Task<IResult> EmitToGroup<THub, TPayload>(
        IHubContext<THub> hub,
        string method,
        string group,
        TPayload payload)
        where THub : Hub
    {
        await hub.Clients.Group(group).SendAsync(method, payload);
        return Results.Ok(new { emitted = method });
    }

    public sealed record QuoteEmission(string ContractId, PriceUpdate Update);

    public sealed record MarketTradeEmission(string ContractId, TradeUpdate[] Updates);

    public sealed record DepthEmission(string ContractId, OrderBookUpdate[] Updates);
}
