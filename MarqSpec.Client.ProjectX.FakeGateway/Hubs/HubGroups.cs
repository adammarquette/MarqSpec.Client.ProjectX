namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// SignalR group names for venue-like subscription membership.
/// </summary>
/// <remarks>
/// The real hub delivers only to the connection that subscribed. Emitting to <c>Clients.All</c> would
/// keep a reconnected client live after a connection-id change even if it never re-subscribed — the
/// gh#87 lie, painted green. Groups die with the connection, so a post-abort emit reaches nobody
/// until a new subscribe lands.
/// </remarks>
internal static class HubGroups
{
    public static string MarketQuotes(string contractId) => $"market:quotes:{contractId}";

    public static string MarketTrades(string contractId) => $"market:trades:{contractId}";

    public static string MarketDepth(string contractId) => $"market:depth:{contractId}";

    public static string UserOrders(int accountId) => $"user:orders:{accountId}";

    public static string UserPositions(int accountId) => $"user:positions:{accountId}";

    public static string UserAccounts() => "user:accounts";

    public static string UserTrades(int accountId) => $"user:trades:{accountId}";
}
