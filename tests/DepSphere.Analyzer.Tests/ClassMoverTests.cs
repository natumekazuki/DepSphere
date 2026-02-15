using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class ClassMoverTests
{
    [Fact]
    public async Task namespace移動で新ファイルへクラスを移せる()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");

        var result = await ClassMover.MoveNamespaceAsync(csprojPath, "SampleFixture.Impl", "Moved.Space");

        Assert.True(File.Exists(result.TargetFilePath));
        var newContent = await File.ReadAllTextAsync(result.TargetFilePath);
        Assert.Contains("namespace Moved.Space;", newContent, StringComparison.Ordinal);
        Assert.Contains("class Impl", newContent, StringComparison.Ordinal);

        var oldContent = await File.ReadAllTextAsync(result.SourceFilePath);
        Assert.DoesNotContain("class Impl", oldContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task namespace移動後に再解析結果が追随する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");

        await ClassMover.MoveNamespaceAsync(csprojPath, "SampleFixture.Impl", "Moved.Space");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);

        Assert.Contains(graph.Nodes, node => node.Id == "Moved.Space.Impl");
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
