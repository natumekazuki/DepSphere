namespace DepSphere.Analyzer;

public sealed class RealtimeUpdateScheduler : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _debounce;
    private readonly Func<IReadOnlyList<GraphChangeEvent>, Task> _onBatch;
    private readonly List<GraphChangeEvent> _events = new();
    private CancellationTokenSource? _scheduleCts;
    private bool _disposed;

    public RealtimeUpdateScheduler(TimeSpan debounce, Func<IReadOnlyList<GraphChangeEvent>, Task> onBatch)
    {
        if (debounce <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(debounce));
        }

        _debounce = debounce;
        _onBatch = onBatch ?? throw new ArgumentNullException(nameof(onBatch));
    }

    public void Submit(GraphChangeEvent changeEvent)
    {
        CancellationToken token;

        lock (_gate)
        {
            ThrowIfDisposed();

            _events.Add(changeEvent);
            _scheduleCts?.Cancel();
            _scheduleCts?.Dispose();
            _scheduleCts = new CancellationTokenSource();
            token = _scheduleCts.Token;
        }

        _ = ScheduleFlushAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? cts;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            cts = _scheduleCts;
            _scheduleCts = null;
            _events.Clear();
        }

        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        await Task.CompletedTask;
    }

    private async Task ScheduleFlushAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounce, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        IReadOnlyList<GraphChangeEvent> snapshot;
        lock (_gate)
        {
            if (_disposed || cancellationToken.IsCancellationRequested || _events.Count == 0)
            {
                return;
            }

            snapshot = GraphChangeBatcher.Merge(_events);
            _events.Clear();
        }

        await _onBatch(snapshot);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RealtimeUpdateScheduler));
        }
    }
}
