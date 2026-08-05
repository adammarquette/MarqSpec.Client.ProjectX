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
        app.MapPost("/_control/reset", (GatewayState state) =>
        {
            state.Reset();
            return Results.Ok(new { reset = true });
        });

        app.MapGet("/_control/state", (GatewayState state) => Results.Ok(new
        {
            requestCounts = state.RequestCounts,
            hubTokensSeen = state.HubTokensSeen.Keys,
            hubSubscriptions = state.HubSubscriptions,
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

        app.MapPost("/_control/emit/order", (OrderUpdate update, IHubContext<UserHub> hub) =>
            Emit(hub, "GatewayUserOrder", update));

        app.MapPost("/_control/emit/position", (PositionUpdate update, IHubContext<UserHub> hub) =>
            Emit(hub, "GatewayUserPosition", update));

        app.MapPost("/_control/emit/account", (AccountUpdate update, IHubContext<UserHub> hub) =>
            Emit(hub, "GatewayUserAccount", update));

        app.MapPost("/_control/emit/trade-notification", (TradeNotification notification, IHubContext<UserHub> hub) =>
            Emit(hub, "GatewayUserTrade", notification));

        // The market hub's handlers take (contractId, payload); the array-shaped ones matter because
        // GatewayTrade and GatewayDepth deserialize as arrays - a single object silently failed to bind
        // before that was fixed.
        app.MapPost("/_control/emit/quote", async (QuoteEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.All.SendAsync("GatewayQuote", emission.ContractId, emission.Update);
            return Results.Ok(new { emitted = "GatewayQuote" });
        });

        app.MapPost("/_control/emit/market-trade", async (MarketTradeEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.All.SendAsync("GatewayTrade", emission.ContractId, emission.Updates);
            return Results.Ok(new { emitted = "GatewayTrade", count = emission.Updates.Length });
        });

        app.MapPost("/_control/emit/depth", async (DepthEmission emission, IHubContext<MarketHub> hub) =>
        {
            await hub.Clients.All.SendAsync("GatewayDepth", emission.ContractId, emission.Updates);
            return Results.Ok(new { emitted = "GatewayDepth", count = emission.Updates.Length });
        });

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    }

    private static async Task<IResult> Emit<THub, TPayload>(IHubContext<THub> hub, string method, TPayload payload)
        where THub : Hub
    {
        await hub.Clients.All.SendAsync(method, payload);
        return Results.Ok(new { emitted = method });
    }

    public sealed record QuoteEmission(string ContractId, PriceUpdate Update);

    public sealed record MarketTradeEmission(string ContractId, TradeUpdate[] Updates);

    public sealed record DepthEmission(string ContractId, OrderBookUpdate[] Updates);
}
