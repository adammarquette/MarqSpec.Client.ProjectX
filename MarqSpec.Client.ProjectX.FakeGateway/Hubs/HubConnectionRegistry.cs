using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// Live hub connections, so a test can abort one hub without touching the other.
/// </summary>
/// <remarks>
/// SignalR automatic reconnect is a new connection id and drops server-side group membership. Without
/// <see cref="Abort"/>, the integration tier cannot force that path — a quiet tape after reconnect is
/// indistinguishable from a quiet market.
/// </remarks>
public sealed class HubConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HubCallerContext>> _hubs = new(StringComparer.Ordinal);

    /// <summary>Registers <paramref name="context"/> under <paramref name="hub"/> ("market" or "user").</summary>
    public void Add(string hub, HubCallerContext context)
    {
        var connections = _hubs.GetOrAdd(hub, _ => new(StringComparer.Ordinal));
        connections[context.ConnectionId] = context;
    }

    /// <summary>Drops a connection that has already disconnected.</summary>
    public void Remove(string hub, string connectionId)
    {
        if (_hubs.TryGetValue(hub, out var connections))
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    /// <summary>Connection ids currently attached to <paramref name="hub"/>.</summary>
    public IReadOnlyList<string> ConnectionIds(string hub)
    {
        return _hubs.TryGetValue(hub, out var connections)
            ? connections.Keys.ToArray()
            : [];
    }

    /// <summary>
    /// Drops every live connection on <paramref name="hub"/>. Returns how many were signalled.
    /// </summary>
    /// <remarks>
    /// <see cref="HubCallerContext.Abort"/> sends a SignalR close with <c>allowReconnect: false</c>,
    /// which stops automatic reconnect — the opposite of what gh#92 needs. Killing the HTTP
    /// connection looks like a transport fault, so the client gets a new connection id.
    /// </remarks>
    public int Abort(string hub)
    {
        if (!_hubs.TryGetValue(hub, out var connections))
        {
            return 0;
        }

        // OnDisconnectedAsync mutates the dictionary — snapshot first.
        var snapshot = connections.Values.ToArray();
        foreach (var context in snapshot)
        {
            context.GetHttpContext()?.Abort();
        }

        return snapshot.Length;
    }

    /// <summary>Aborts both hubs. Used by <c>/_control/reset</c> so a leftover socket cannot leak into the next test.</summary>
    public int AbortAll() => Abort("market") + Abort("user");
}
