namespace MarqSpec.Client.ProjectX.WebSocket;

/// <summary>
/// Test seam over <see cref="Microsoft.AspNetCore.SignalR.Client.HubConnection"/>.
/// Unit tests fake this instead of opening a socket (gh#87).
/// </summary>
internal interface IHubConnectionAdapter : IAsyncDisposable
{
    event Func<Exception?, Task>? Closed;

    event Func<Exception?, Task>? Reconnecting;

    event Func<string?, Task>? Reconnected;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task InvokeAsync(string methodName, object?[] args, CancellationToken cancellationToken);

    IDisposable On<T1>(string methodName, Action<T1> handler);

    IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler);
}
