using Microsoft.AspNetCore.SignalR.Client;

namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Forwards <see cref="IHubConnectionAdapter"/> onto a real SignalR
/// <see cref="HubConnection"/>.
/// </summary>
internal sealed class SignalRHubConnectionAdapter : IHubConnectionAdapter
{
    private readonly HubConnection _connection;

    public SignalRHubConnectionAdapter(HubConnection connection)
    {
        _connection = connection;
    }

    public event Func<Exception?, Task>? Closed
    {
        add => _connection.Closed += value;
        remove => _connection.Closed -= value;
    }

    public event Func<Exception?, Task>? Reconnecting
    {
        add => _connection.Reconnecting += value;
        remove => _connection.Reconnecting -= value;
    }

    public event Func<string?, Task>? Reconnected
    {
        add => _connection.Reconnected += value;
        remove => _connection.Reconnected -= value;
    }

    public Task StartAsync(CancellationToken cancellationToken) => _connection.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _connection.StopAsync(cancellationToken);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    public Task InvokeAsync(string methodName, object?[] args, CancellationToken cancellationToken) =>
        args.Length switch
        {
            0 => _connection.InvokeAsync(methodName, cancellationToken),
            1 => _connection.InvokeAsync(methodName, args[0], cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(args),
                args.Length,
                "Hub invokes take zero or one argument.")
        };

    public IDisposable On<T1>(string methodName, Action<T1> handler) => _connection.On(methodName, handler);

    public IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler) =>
        _connection.On(methodName, handler);
}
