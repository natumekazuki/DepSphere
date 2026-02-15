using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class EndToEndFlowTests
{
    [Fact]
    public async Task 解析_可視化_閲覧_移動_追随更新が連続動作する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");

        var initialGraph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);
        Assert.Contains(initialGraph.Nodes, node => node.Id == "SampleFixture.Impl");

        var view = GraphViewBuilder.Build(initialGraph);
        var html = GraphViewHtmlBuilder.Build(view);
        Assert.Contains("dep-graph-canvas", html);

        var openOk = GraphSelectionCoordinator.TryOpenFromMessage(
            initialGraph,
            "{\"type\":\"nodeSelected\",\"nodeId\":\"SampleFixture.Impl\"}",
            out var opened);

        Assert.True(openOk);
        Assert.NotNull(opened);
        Assert.Contains("class Impl", opened!.Content, StringComparison.Ordinal);

        var moveResult = await ClassMover.MoveNamespaceAsync(csprojPath, "SampleFixture.Impl", "Moved.Space");
        Assert.True(File.Exists(moveResult.TargetFilePath));

        var updater = new RealtimeGraphUpdater();
        var updateResult = await updater.UpdateAsync(
            csprojPath,
            initialGraph,
            new[]
            {
                new GraphChangeEvent(GraphChangeEventType.DocumentChanged, moveResult.SourceFilePath),
                new GraphChangeEvent(GraphChangeEventType.DocumentAdded, moveResult.TargetFilePath)
            });

        Assert.Contains(updateResult.UpdatedGraph.Nodes, node => node.Id == "Moved.Space.Impl");
        Assert.DoesNotContain(updateResult.UpdatedGraph.Nodes, node => node.Id == "SampleFixture.Impl");
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
