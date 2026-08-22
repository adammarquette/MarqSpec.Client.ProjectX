using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.FakeGateway.Auth;
using MarqSpec.Client.ProjectX.FakeGateway.State;

namespace MarqSpec.Client.ProjectX.FakeGateway.Endpoints;

/// <summary>
/// The gateway's REST surface, mirroring <c>swagger.json</c>. Every route is <c>POST</c> except
/// <c>/api/Status/ping</c>.
/// </summary>
public static class ApiEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // The field names are the gateway's and they are misleading: userName carries the operator's API key,
        // and apiKey carries their SECRET. See ADR-0003. Binding a matching shape here is what lets a test
        // catch a regression in the client's mapping.
        app.MapPost("/api/Auth/loginKey", (LoginRequest request, JwtIssuer jwt) =>
            string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.ApiKey)
                ? Results.Ok(new { success = false, errorCode = 3, errorMessage = (string?)"InvalidCredentials", token = (string?)null })
                : Results.Ok(new { success = true, errorCode = 0, errorMessage = (string?)null, token = jwt.Issue(request.UserName) }));

        app.MapPost("/api/Auth/validate", (JwtIssuer jwt) =>
            Results.Ok(new { success = true, errorCode = 0, errorMessage = (string?)null, newToken = jwt.Issue("revalidated") }));

        app.MapPost("/api/Auth/logout", () =>
            Results.Ok(new { success = true, errorCode = 0, errorMessage = (string?)null }));
    }

    public static void MapAccountEndpoints(this WebApplication app) =>
        app.MapPost("/api/Account/search", (SearchAccountRequest request, GatewayState state) =>
            Results.Ok(new SearchAccountResponse
            {
                Success = true,
                Accounts = request.OnlyActiveAccounts
                    ? state.Accounts.FindAll(a => a.CanTrade && a.IsVisible)
                    : [.. state.Accounts],
            }));

    public static void MapContractEndpoints(this WebApplication app)
    {
        app.MapPost("/api/Contract/search", (SearchContractRequest request, GatewayState state) =>
            Results.Ok(new SearchContractResponse
            {
                Success = true,
                Contracts = state.Contracts.FindAll(c =>
                    (request.Live ? c.ActiveContract : true)
                    && (string.IsNullOrWhiteSpace(request.SearchText)
                        || c.Name.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)
                        || c.Description.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))),
            }));

        app.MapPost("/api/Contract/searchById", (SearchContractByIdRequest request, GatewayState state) =>
            Results.Ok(new SearchContractByIdResponse
            {
                Success = true,
                Contract = state.Contracts.Find(c => c.Id == request.ContractId),
            }));

        app.MapPost("/api/Contract/available", (ListAvailableContractRequest request, GatewayState state) =>
            Results.Ok(new ListAvailableContractResponse
            {
                Success = true,
                Contracts = state.Contracts.FindAll(c => c.ActiveContract),
            }));
    }

    public static void MapHistoryEndpoints(this WebApplication app) =>
        app.MapPost("/api/History/retrieveBars", (RetrieveBarRequest request, GatewayState state) =>
        {
            if (state.Contracts.Find(c => c.Id == request.ContractId) is null)
            {
                return Results.Ok(new RetrieveBarResponse { Success = false, ErrorCode = 2, ErrorMessage = "contract not found" });
            }

            // Deterministic, walk-forward bars. A test asserting on OHLC needs the same answer every run.
            var step = UnitToTimeSpan(request.Unit, request.UnitNumber);
            var bars = new List<AggregateBar>();
            var cursor = request.StartTime;
            var open = 21_500.00m;

            while (cursor < request.EndTime && bars.Count < Math.Max(1, request.Limit))
            {
                var close = open + ((bars.Count % 5) - 2) * 0.25m;
                bars.Add(new AggregateBar
                {
                    Timestamp = cursor,
                    Open = open,
                    High = Math.Max(open, close) + 0.75m,
                    Low = Math.Min(open, close) - 0.75m,
                    Close = close,
                    Volume = 1_000 + (bars.Count * 37),
                });

                open = close;
                cursor = cursor.Add(step);
            }

            return Results.Ok(new RetrieveBarResponse { Success = true, Bars = bars });
        });

    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/Order/place", (PlaceOrderRequest request, GatewayState state) =>
        {
            var order = state.PlaceOrder(request);
            return Results.Ok(new PlaceOrderResponse { Success = true, OrderId = order.Id });
        });

        app.MapPost("/api/Order/modify", (ModifyOrderRequest request, GatewayState state) =>
            Results.Ok(state.Mutate(() =>
            {
                var order = state.Orders.Find(o => o.Id == request.OrderId && o.AccountId == request.AccountId);
                if (order is null)
                {
                    return new ModifyOrderResponse { Success = false, ErrorCode = 2, ErrorMessage = "order not found" };
                }

                order.Size = request.Size ?? order.Size;
                order.LimitPrice = request.LimitPrice ?? order.LimitPrice;
                order.StopPrice = request.StopPrice ?? order.StopPrice;
                order.UpdateTimestamp = DateTime.UtcNow;
                return new ModifyOrderResponse { Success = true };
            })));

        app.MapPost("/api/Order/cancel", (CancelOrderRequest request, GatewayState state) =>
            Results.Ok(state.Mutate(() =>
            {
                var order = state.Orders.Find(o => o.Id == request.OrderId && o.AccountId == request.AccountId);
                if (order is null)
                {
                    return new CancelOrderResponse { Success = false, ErrorCode = 2, ErrorMessage = "order not found" };
                }

                // Cancelling an already-cancelled order succeeds. That is what makes cancel safe to retry,
                // and the property ADR-0002 relies on when it excludes only placement.
                order.Status = OrderStatus.Cancelled;
                order.UpdateTimestamp = DateTime.UtcNow;
                return new CancelOrderResponse { Success = true };
            })));

        // Mirrors swagger.json exactly, and the two ways it used to not:
        //
        //   1. startTimestamp is `required`. Accepting a null one made the fake MORE permissive than the venue,
        //      which is the failure mode ADR-0007 exists to prevent -- green here, wrong there. It now rejects.
        //   2. contractId and status are NOT in the schema. Honouring them as filters taught tests that the
        //      gateway filters server-side when in reality it ignores both and returns the unfiltered window.
        app.MapPost("/api/Order/search", (SearchOrderRequest request, GatewayState state) =>
            request.StartTime is null
                ? Results.BadRequest(new
                {
                    success = false,
                    errorCode = 400,
                    errorMessage = "startTimestamp is required.",
                })
                : Results.Ok(new SearchOrderResponse
                {
                    Success = true,
                    Orders = state.Orders.FindAll(o =>
                        o.AccountId == request.AccountId
                        && o.CreationTimestamp >= request.StartTime
                        && (request.EndTime is null || o.CreationTimestamp < request.EndTime)),
                }));

        app.MapPost("/api/Order/searchOpen", (SearchOpenOrderRequest request, GatewayState state) =>
            Results.Ok(new SearchOrderResponse
            {
                Success = true,
                Orders = state.Orders.FindAll(o => o.AccountId == request.AccountId && o.Status == OrderStatus.Open),
            }));
    }

    public static void MapPositionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/Position/searchOpen", (SearchPositionRequest request, GatewayState state) =>
            Results.Ok(new SearchPositionResponse
            {
                Success = true,
                Positions = state.Positions.FindAll(p => p.AccountId == request.AccountId && p.Size > 0),
            }));

        app.MapPost("/api/Position/closeContract", (CloseContractPositionRequest request, GatewayState state) =>
            Results.Ok(state.Mutate(() =>
            {
                var removed = state.Positions.RemoveAll(p => p.AccountId == request.AccountId && p.ContractId == request.ContractId);
                return removed > 0
                    ? new ClosePositionResponse { Success = true }
                    : new ClosePositionResponse { Success = false, ErrorCode = 2, ErrorMessage = "no open position" };
            })));

        app.MapPost("/api/Position/partialCloseContract", (PartialCloseContractPositionRequest request, GatewayState state) =>
            Results.Ok(state.Mutate(() =>
            {
                var position = state.Positions.Find(p => p.AccountId == request.AccountId && p.ContractId == request.ContractId);
                if (position is null)
                {
                    return new PartialClosePositionResponse { Success = false, ErrorCode = 2, ErrorMessage = "no open position" };
                }

                if (request.Size >= position.Size)
                {
                    return new PartialClosePositionResponse { Success = false, ErrorCode = 4, ErrorMessage = "size exceeds position" };
                }

                position.Size -= request.Size;
                return new PartialClosePositionResponse { Success = true };
            })));
    }

    public static void MapTradeEndpoints(this WebApplication app) =>
        app.MapPost("/api/Trade/search", (SearchTradeRequest request, GatewayState state) =>
            Results.Ok(new SearchTradeResponse
            {
                Success = true,
                Trades = state.Trades.FindAll(t =>
                    t.AccountId == request.AccountId
                    && (request.StartTimestamp is null || t.CreationTimestamp >= request.StartTimestamp)
                    && (request.EndTimestamp is null || t.CreationTimestamp < request.EndTimestamp)),
            }));

    public static void MapStatusEndpoints(this WebApplication app) =>
        // text/plain, NOT JSON. Refit returns the raw body for a Task<string> rather than deserializing it, so
        // a JSON-encoded "pong" would arrive at the client complete with quotes and fail its
        // string.Equals(response, "pong") check. The swagger types this response as a bare string, and an
        // ASP.NET action returning string uses StringOutputFormatter — text/plain, unquoted. Serving JSON here
        // would have made the fake disagree with the real gateway in the client's favour, which is the one
        // direction a test double must never get wrong.
        app.MapGet("/api/Status/ping", () => Results.Text("pong"));

    private static TimeSpan UnitToTimeSpan(AggregateBarUnit unit, int number)
    {
        var count = Math.Max(1, number);
        return unit switch
        {
            AggregateBarUnit.Second => TimeSpan.FromSeconds(count),
            AggregateBarUnit.Minute => TimeSpan.FromMinutes(count),
            AggregateBarUnit.Hour => TimeSpan.FromHours(count),
            AggregateBarUnit.Day => TimeSpan.FromDays(count),
            AggregateBarUnit.Week => TimeSpan.FromDays(7 * count),
            AggregateBarUnit.Month => TimeSpan.FromDays(30 * count),
            _ => TimeSpan.FromMinutes(count),
        };
    }

    /// <summary>The <c>loginKey</c> body, in the gateway's own naming.</summary>
    public sealed record LoginRequest(string? UserName, string? ApiKey);
}
