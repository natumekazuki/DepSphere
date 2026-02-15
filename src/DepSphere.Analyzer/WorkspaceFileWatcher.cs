using System.IO;

namespace DepSphere.Analyzer;

public sealed class WorkspaceFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Action<GraphChangeEvent> _onEvent;

    public WorkspaceFileWatcher(string rootPath, Action<GraphChangeEvent> onEvent)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException(rootPath);
        }

        _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));

        _watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
            EnableRaisingEvents = false
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }

    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        if (!WorkspaceFileFilter.ShouldTrack(args.FullPath))
        {
            return;
        }

        var type = WorkspaceFileWatcherEventMapper.Map(args.ChangeType);
        _onEvent(new GraphChangeEvent(type, args.FullPath, DateTimeOffset.UtcNow));
    }

    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        if (!WorkspaceFileFilter.ShouldTrack(args.FullPath))
        {
            return;
        }

        _onEvent(new GraphChangeEvent(GraphChangeEventType.DocumentRenamed, args.FullPath, DateTimeOffset.UtcNow));
    }
}
