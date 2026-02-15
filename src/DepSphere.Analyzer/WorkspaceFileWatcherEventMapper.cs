using System.IO;

namespace DepSphere.Analyzer;

public static class WorkspaceFileWatcherEventMapper
{
    public static GraphChangeEventType Map(WatcherChangeTypes changeType)
    {
        return changeType switch
        {
            WatcherChangeTypes.Created => GraphChangeEventType.DocumentAdded,
            WatcherChangeTypes.Deleted => GraphChangeEventType.DocumentRemoved,
            WatcherChangeTypes.Renamed => GraphChangeEventType.DocumentRenamed,
            _ => GraphChangeEventType.DocumentChanged
        };
    }
}
