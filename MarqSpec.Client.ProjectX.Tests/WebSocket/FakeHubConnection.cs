using MarqSpec.Client.ProjectX.WebSocket;

namespace MarqSpec.Client.ProjectX.Tests.WebSocket;

/// <summary>
/// In-process stand-in for <see cref="IHubConnectionAdapter"/>. FakeItEasy
/// cannot proxy that internal interface without opening
/// <c>InternalsVisibleTo(DynamicProxyGenAssembly2)</c> on the library.
/// </summary>
internal sealed class FakeHubConnection : IHubConnectionAdapter
{
    private readonly List<(string Method, object?[] Args)> _invocations = [];
    private int _invokeCount;
    private readonly List<Task> _invokeResults = [];
    private Exception? _alwaysThrow;

    public event Func<Exception?, Task>? Closed;

    public event Func<Exception?, Task>? Reconnecting;

    public event Func<string?, Task>? Reconnected;

    public IReadOnlyList<(string Method, object?[] Args)> Invocations => _invocations;

    public Action<int>? ConnectedDuringInvoke { get; set; }

    public void QueueInvokeResult(Task result) => _invokeResults.Add(result);

    public void ThrowOnInvoke(Exception exception) => _alwaysThrow = exception;

    public Task RaiseClosedAsync(Exception? error) => Closed?.Invoke(error) ?? Task.CompletedTask;

    public Task RaiseReconnectingAsync(Exception? error) => Reconnecting?.Invoke(error) ?? Task.CompletedTask;

    public Task RaiseReconnectedAsync(string? connectionId) =>
        Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async Task InvokeAsync(string methodName, object?[] args, CancellationToken cancellationToken)
    {
        _invokeCount++;
        _invocations.Add((methodName, args));
        ConnectedDuringInvoke?.Invoke(_invokeCount);

        if (_alwaysThrow is not null)
        {
            throw _alwaysThrow;
        }

        if (_invokeResults.Count > 0)
        {
            var next = _invokeResults[0];
            _invokeResults.RemoveAt(0);
            await next;
            return;
        }

        await Task.CompletedTask;
    }

    public IDisposable On<T1>(string methodName, Action<T1> handler) => NullSubscription.Instance;

    public IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler) => NullSubscription.Instance;

    public int InvokeCount => _invokeCount;

    private sealed class NullSubscription : IDisposable
    {
        public static readonly NullSubscription Instance = new();

        public void Dispose()
        {
        }
    }
}
