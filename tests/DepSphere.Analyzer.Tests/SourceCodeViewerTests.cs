using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class SourceCodeViewerTests
{
    [Fact]
    public async Task ノードからソースコードを取得できる()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);

        var doc = SourceCodeViewer.OpenNode(graph, "SampleFixture.Impl");

        Assert.EndsWith("FixtureTypes.cs", doc.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("class Impl", doc.Content, StringComparison.Ordinal);
        Assert.True(doc.StartLine >= 1);
        Assert.True(doc.EndLine >= doc.StartLine);
    }

    [Fact]
    public async Task ノードが存在しない場合は失敗する()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);

        Assert.Throws<InvalidOperationException>(() => SourceCodeViewer.OpenNode(graph, "Not.Exists"));
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
