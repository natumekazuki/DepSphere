using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class RealtimeSchedulerTests
{
    [Theory]
    [InlineData("/tmp/A.cs", true)]
    [InlineData("/tmp/B.csproj", true)]
    [InlineData("/tmp/C.sln", true)]
    [InlineData("/tmp/README.md", false)]
    [InlineData("/tmp/script.js", false)]
    public void 監視対象ファイルを判定できる(string path, bool expected)
    {
        Assert.Equal(expected, WorkspaceFileFilter.ShouldTrack(path));
    }

    [Fact]
    public async Task 変更イベントをデバウンスしてバッチ化できる()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<GraphChangeEvent>>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var scheduler = new RealtimeUpdateScheduler(
            TimeSpan.FromMilliseconds(80),
            events =>
            {
                tcs.TrySetResult(events);
                return Task.CompletedTask;
            });

        scheduler.Submit(new GraphChangeEvent(GraphChangeEventType.DocumentChanged, "/tmp/A.cs"));
        scheduler.Submit(new GraphChangeEvent(GraphChangeEventType.DocumentChanged, "/tmp/A.cs"));
        scheduler.Submit(new GraphChangeEvent(GraphChangeEventType.DocumentAdded, "/tmp/B.cs"));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, timeout.Token));

        Assert.Same(tcs.Task, completed);
        var batch = await tcs.Task;
        Assert.Equal(2, batch.Count);
        Assert.Contains(batch, item => item.Path == "/tmp/A.cs");
        Assert.Contains(batch, item => item.Path == "/tmp/B.cs");
    }
}
