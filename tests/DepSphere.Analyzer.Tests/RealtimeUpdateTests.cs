using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class RealtimeUpdateTests
{
    [Fact]
    public void 同一パスの変更イベントを統合できる()
    {
        var events = new[]
        {
            new GraphChangeEvent(GraphChangeEventType.DocumentChanged, "/tmp/A.cs"),
            new GraphChangeEvent(GraphChangeEventType.DocumentChanged, "/tmp/A.cs"),
            new GraphChangeEvent(GraphChangeEventType.DocumentAdded, "/tmp/B.cs")
        };

        var merged = GraphChangeBatcher.Merge(events);

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, item => item.Path == "/tmp/A.cs");
        Assert.Contains(merged, item => item.Path == "/tmp/B.cs");
    }

    [Fact]
    public void グラフ差分を算出できる()
    {
        var oldGraph = new DependencyGraph(
            new[]
            {
                new DependencyNode("A", new TypeMetrics(1, 1, 0, 0, 0, 0, 0.1)),
                new DependencyNode("B", new TypeMetrics(1, 1, 0, 0, 0, 0, 0.1))
            },
            new[]
            {
                new DependencyEdge("A", "B", DependencyKind.Reference)
            });

        var newGraph = new DependencyGraph(
            new[]
            {
                new DependencyNode("A", new TypeMetrics(1, 1, 0, 0, 0, 0, 0.1)),
                new DependencyNode("C", new TypeMetrics(1, 1, 0, 0, 0, 0, 0.1))
            },
            new[]
            {
                new DependencyEdge("A", "C", DependencyKind.Reference)
            });

        var patch = GraphDiffBuilder.Build(oldGraph, newGraph);

        Assert.Contains(patch.UpsertNodes, node => node.Id == "C");
        Assert.Contains(patch.RemoveNodeIds, id => id == "B");
        Assert.Contains(patch.UpsertEdges, edge => edge.From == "A" && edge.To == "C");
        Assert.Contains(patch.RemoveEdges, edge => edge.From == "A" && edge.To == "B");
    }

    [Fact]
    public async Task 変更イベントから再解析してパッチを返せる()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var projectPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(projectPath);

        var addedFile = Path.Combine(workspaceRoot, "SampleLib", "NewAdded.cs");
        await File.WriteAllTextAsync(
            addedFile,
            "namespace SampleFixture; public class NewAdded {}",
            CancellationToken.None);

        var updater = new RealtimeGraphUpdater();
        var result = await updater.UpdateAsync(
            projectPath,
            graph,
            new[] { new GraphChangeEvent(GraphChangeEventType.DocumentAdded, addedFile) });

        Assert.Contains(result.Patch.UpsertNodes, node => node.Id == "SampleFixture.NewAdded");
        Assert.Contains(result.UpdatedGraph.Nodes, node => node.Id == "SampleFixture.NewAdded");
    }

    [Fact]
    public async Task cs変更のみなら無効な解析パスでも増分更新できる()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "depsphere-inc-" + Guid.NewGuid().ToString("N") + ".cs");
        await File.WriteAllTextAsync(tempFile, "namespace Inc; public class AddedType {}", CancellationToken.None);

        var updater = new RealtimeGraphUpdater();
        var result = await updater.UpdateAsync(
            "/path/does-not-exist.csproj",
            new DependencyGraph(Array.Empty<DependencyNode>(), Array.Empty<DependencyEdge>()),
            new[] { new GraphChangeEvent(GraphChangeEventType.DocumentAdded, tempFile) });

        Assert.Contains(result.UpdatedGraph.Nodes, node => node.Id == "Inc.AddedType");
        Assert.Contains(result.Patch.UpsertNodes, node => node.Id == "Inc.AddedType");
    }

    [Fact]
    public async Task csproj変更イベントは全量再解析へフォールバックする()
    {
        var updater = new RealtimeGraphUpdater();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            updater.UpdateAsync(
                "/path/does-not-exist.csproj",
                new DependencyGraph(Array.Empty<DependencyNode>(), Array.Empty<DependencyEdge>()),
                new[] { new GraphChangeEvent(GraphChangeEventType.DocumentChanged, "/path/does-not-exist.csproj") }));
    }

    private static string CopyFixtureToTemp()
    {
        var fixtureRoot = GetFixturePath();
        var destinationRoot = Path.Combine(Path.GetTempPath(), "depsphere-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destinationRoot);

        foreach (var sourceFile in Directory.EnumerateFiles(fixtureRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(fixtureRoot, sourceFile);
            var destinationFile = Path.Combine(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        return destinationRoot;
    }

    private static string GetFixturePath()
    {
        var root = FindRepoRoot();
        return Path.Combine(root, "tests", "DepSphere.Analyzer.Tests", "Fixtures");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
