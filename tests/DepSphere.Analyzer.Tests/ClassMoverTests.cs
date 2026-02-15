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

    [Fact]
    public async Task ファイル移動で指定パスへクラスを移せる()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var relativePath = Path.Combine("Moved", "ImplMoved.cs");

        var result = await ClassMover.MoveFileAsync(csprojPath, "SampleFixture.Impl", relativePath);

        Assert.True(File.Exists(result.TargetFilePath));
        var movedContent = await File.ReadAllTextAsync(result.TargetFilePath);
        Assert.Contains("namespace SampleFixture;", movedContent, StringComparison.Ordinal);
        Assert.Contains("class Impl", movedContent, StringComparison.Ordinal);

        var originalContent = await File.ReadAllTextAsync(result.SourceFilePath);
        Assert.DoesNotContain("class Impl", originalContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ファイル移動後に再解析結果が新ファイルを指す()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var relativePath = Path.Combine("Moved", "ImplMoved.cs");

        var result = await ClassMover.MoveFileAsync(csprojPath, "SampleFixture.Impl", relativePath);
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);

        var movedNode = Assert.Single(graph.Nodes.Where(node => node.Id == "SampleFixture.Impl"));
        Assert.NotNull(movedNode.Location);
        Assert.Equal(result.TargetFilePath, movedNode.Location!.FilePath);
    }

    [Fact]
    public async Task プロジェクト間移動で移動先プロジェクトへクラスを移せる()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var sourceProjectPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var targetProjectPath = Path.Combine(workspaceRoot, "TargetLib", "TargetLib.csproj");
        var targetRelativePath = Path.Combine("Migrated", "ImplFromSample.cs");

        var result = await ClassMover.MoveProjectAsync(
            sourceProjectPath,
            targetProjectPath,
            "SampleFixture.Impl",
            targetRelativePath);

        Assert.True(File.Exists(result.TargetFilePath));
        var movedContent = await File.ReadAllTextAsync(result.TargetFilePath);
        Assert.Contains("namespace SampleFixture;", movedContent, StringComparison.Ordinal);
        Assert.Contains("class Impl", movedContent, StringComparison.Ordinal);

        var sourceContent = await File.ReadAllTextAsync(result.SourceFilePath);
        Assert.DoesNotContain("class Impl", sourceContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task プロジェクト間移動後に再解析結果が追随する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var sourceProjectPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var targetProjectPath = Path.Combine(workspaceRoot, "TargetLib", "TargetLib.csproj");
        var targetRelativePath = Path.Combine("Migrated", "ImplFromSample.cs");
        var solutionPath = Path.Combine(workspaceRoot, "SampleWorkspace.sln");

        var result = await ClassMover.MoveProjectAsync(
            sourceProjectPath,
            targetProjectPath,
            "SampleFixture.Impl",
            targetRelativePath);

        var graph = await DependencyAnalyzer.AnalyzePathAsync(solutionPath);
        var movedNode = Assert.Single(graph.Nodes.Where(node => node.Id == "SampleFixture.Impl"));

        Assert.NotNull(movedNode.Location);
        Assert.Equal(result.TargetFilePath, movedNode.Location!.FilePath);
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
