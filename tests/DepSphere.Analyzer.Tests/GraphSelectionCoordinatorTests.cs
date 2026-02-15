using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class GraphSelectionCoordinatorTests
{
    [Fact]
    public async Task ノード選択メッセージからソースを開ける()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);
        var message = "{\"type\":\"nodeSelected\",\"nodeId\":\"SampleFixture.Impl\"}";

        var ok = GraphSelectionCoordinator.TryOpenFromMessage(graph, message, out var doc);

        Assert.True(ok);
        Assert.NotNull(doc);
        Assert.Contains("class Impl", doc!.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task 非対象メッセージは開かない()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);
        var message = "{\"type\":\"ping\"}";

        var ok = GraphSelectionCoordinator.TryOpenFromMessage(graph, message, out var doc);

        Assert.False(ok);
        Assert.Null(doc);
    }

    private static string GetFixturePath(params string[] paths)
    {
        var root = FindRepoRoot();
        var all = new[] { root, "tests", "DepSphere.Analyzer.Tests", "Fixtures" }
            .Concat(paths)
            .ToArray();
        return Path.Combine(all);
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
