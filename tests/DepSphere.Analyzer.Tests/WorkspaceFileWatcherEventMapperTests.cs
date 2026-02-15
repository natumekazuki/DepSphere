using DepSphere.Analyzer;
using System.IO;

namespace DepSphere.Analyzer.Tests;

public class WorkspaceFileWatcherEventMapperTests
{
    [Theory]
    [InlineData(WatcherChangeTypes.Changed, GraphChangeEventType.DocumentChanged)]
    [InlineData(WatcherChangeTypes.Created, GraphChangeEventType.DocumentAdded)]
    [InlineData(WatcherChangeTypes.Deleted, GraphChangeEventType.DocumentRemoved)]
    [InlineData(WatcherChangeTypes.Renamed, GraphChangeEventType.DocumentRenamed)]
    public void Watcherイベントを変更イベントへ変換できる(WatcherChangeTypes input, GraphChangeEventType expected)
    {
        var actual = WorkspaceFileWatcherEventMapper.Map(input);

        Assert.Equal(expected, actual);
    }
}
