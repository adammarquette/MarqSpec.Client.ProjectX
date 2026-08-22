using System.Collections.Concurrent;
using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX.FakeGateway.State;

/// <summary>
/// The whole fake venue: accounts, contracts, orders, positions, trades, plus whatever scenario a test has
/// armed. Registered as a singleton and mutated under a lock — this stands in for a venue, so a test that
/// places an order and then searches for it must see it.
/// </summary>
public sealed class GatewayState
{
    private readonly object _gate = new();
    private long _nextOrderId = 1000;
    private int _nextPositionId = 500;

    /// <summary>Requests counted per route suffix, so a test can assert how many attempts arrived.</summary>
    public ConcurrentDictionary<string, int> RequestCounts { get; } = new();

    /// <summary>Armed fault scenarios, consumed in order.</summary>
    public List<FaultDirective> Faults { get; } = [];

    /// <summary>Access tokens the hubs have seen, so a test can prove a fresh one was supplied on reconnect.</summary>
    public ConcurrentDictionary<string, string> HubTokensSeen { get; } = new();

    /// <summary>Hub subscription calls received, as "hub:method:argument".</summary>
    public ConcurrentBag<string> HubSubscriptions { get; } = [];

    public List<TradingAccount> Accounts { get; private set; } = [];
    public List<Contract> Contracts { get; private set; } = [];
    public List<Order> Orders { get; } = [];
    public List<Position> Positions { get; } = [];
    public List<HalfTrade> Trades { get; } = [];

    public GatewayState() => Reset();

    /// <summary>Restores the deterministic seed and clears everything a test may have armed or created.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _nextOrderId = 1000;
            _nextPositionId = 500;
            RequestCounts.Clear();
            HubTokensSeen.Clear();
            HubSubscriptions.Clear();
            Faults.Clear();
            Orders.Clear();
            Positions.Clear();
            Trades.Clear();

            Accounts =
            [
                new TradingAccount { Id = 1, Name = "PRACTICE-1", Balance = 50_000m, CanTrade = true, IsVisible = true, Simulated = true },
                new TradingAccount { Id = 2, Name = "PRACTICE-2", Balance = 25_000m, CanTrade = true, IsVisible = true, Simulated = true },
                new TradingAccount { Id = 3, Name = "ARCHIVED", Balance = 0m, CanTrade = false, IsVisible = false, Simulated = true },
            ];

            Contracts =
            [
                new Contract { Id = "CON.F.US.ENQ.Z25", Name = "ENQZ25", Description = "E-mini Nasdaq-100: December 2025", TickSize = 0.25m, TickValue = 5m, ActiveContract = true, SymbolId = "F.US.ENQ" },
                new Contract { Id = "CON.F.US.EP.Z25", Name = "EPZ25", Description = "E-mini S&P 500: December 2025", TickSize = 0.25m, TickValue = 12.50m, ActiveContract = true, SymbolId = "F.US.EP" },
                new Contract { Id = "CON.F.US.ENQ.U25", Name = "ENQU25", Description = "E-mini Nasdaq-100: September 2025", TickSize = 0.25m, TickValue = 5m, ActiveContract = false, SymbolId = "F.US.ENQ" },
            ];
        }
    }

    /// <summary>
    /// Consumes an armed fault for <paramref name="path"/>, if one applies. Also records the request, so the
    /// count is a faithful tally of attempts that actually reached the gateway.
    /// </summary>
    public FaultDirective? NextFault(string path)
    {
        RequestCounts.AddOrUpdate(path, 1, (_, count) => count + 1);

        lock (_gate)
        {
            var fault = Faults.Find(f => f.Remaining > 0 && f.Matches(path));
            if (fault is null)
            {
                return null;
            }

            fault.Remaining--;
            return fault;
        }
    }

    /// <summary>Records an order and, when it is a market order, the fill and position it produces.</summary>
    public Order PlaceOrder(PlaceOrderRequest request)
    {
        lock (_gate)
        {
            var isMarket = request.Type == OrderType.Market;
            var fillPrice = request.LimitPrice ?? request.StopPrice ?? 21_500.00m;

            var order = new Order
            {
                Id = Interlocked.Increment(ref _nextOrderId),
                AccountId = request.AccountId,
                ContractId = request.ContractId,
                SymbolId = Contracts.Find(c => c.Id == request.ContractId)?.SymbolId ?? "F.US.ENQ",
                CreationTimestamp = DateTime.UtcNow,
                Status = isMarket ? OrderStatus.Filled : OrderStatus.Open,
                Type = request.Type,
                Side = request.Side,
                Size = request.Size,
                LimitPrice = request.LimitPrice,
                StopPrice = request.StopPrice,
                FillVolume = isMarket ? request.Size : 0,
                FilledPrice = isMarket ? fillPrice : null,
                CustomTag = request.CustomTag,
            };

            Orders.Add(order);

            if (isMarket)
            {
                Trades.Add(new HalfTrade
                {
                    Id = order.Id,
                    AccountId = order.AccountId,
                    ContractId = order.ContractId,
                    CreationTimestamp = order.CreationTimestamp,
                    Price = fillPrice,
                    Fees = 1.24m,
                    Side = order.Side,
                    Size = order.Size,
                    Voided = false,
                    OrderId = order.Id,
                });

                Positions.Add(new Position
                {
                    Id = Interlocked.Increment(ref _nextPositionId),
                    AccountId = order.AccountId,
                    ContractId = order.ContractId,
                    ContractDisplayName = Contracts.Find(c => c.Id == order.ContractId)?.Name,
                    CreationTimestamp = order.CreationTimestamp,
                    Type = order.Side == OrderSide.Bid ? PositionType.Long : PositionType.Short,
                    Size = order.Size,
                    AveragePrice = fillPrice,
                });
            }

            return order;
        }
    }

    /// <summary>Applies a mutation under the state lock.</summary>
    public T Mutate<T>(Func<T> mutation)
    {
        lock (_gate)
        {
            return mutation();
        }
    }
}
