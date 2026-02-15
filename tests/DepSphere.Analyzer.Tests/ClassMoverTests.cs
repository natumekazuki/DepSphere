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
    public async Task namespace移動で移動先に同名型があると失敗する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var sourcePath = Path.Combine(workspaceRoot, "SampleLib", "FixtureTypes.cs");
        var collisionFilePath = Path.Combine(workspaceRoot, "SampleLib", "CollisionImpl.cs");

        await File.WriteAllTextAsync(
            collisionFilePath,
            """
            namespace Moved.Space;

            public class Impl
            {
            }
            """);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClassMover.MoveNamespaceAsync(csprojPath, "SampleFixture.Impl", "Moved.Space"));

        var sourceContent = await File.ReadAllTextAsync(sourcePath);
        Assert.Contains("class Impl", sourceContent, StringComparison.Ordinal);
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
    public async Task ファイル移動で移動先ファイルが既存なら失敗する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var sourcePath = Path.Combine(workspaceRoot, "SampleLib", "FixtureTypes.cs");
        var relativePath = Path.Combine("Moved", "ImplMoved.cs");
        var existingTargetPath = Path.Combine(workspaceRoot, "SampleLib", relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(existingTargetPath)!);
        await File.WriteAllTextAsync(existingTargetPath, "namespace SampleFixture; public class Placeholder { }");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClassMover.MoveFileAsync(csprojPath, "SampleFixture.Impl", relativePath));

        var sourceContent = await File.ReadAllTextAsync(sourcePath);
        Assert.Contains("class Impl", sourceContent, StringComparison.Ordinal);
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

    [Fact]
    public async Task プロジェクト間移動で移動先に同名型があると失敗する()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var sourceProjectPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var targetProjectPath = Path.Combine(workspaceRoot, "TargetLib", "TargetLib.csproj");
        var targetCollisionPath = Path.Combine(workspaceRoot, "TargetLib", "ExistingImpl.cs");
        var sourcePath = Path.Combine(workspaceRoot, "SampleLib", "FixtureTypes.cs");

        await File.WriteAllTextAsync(
            targetCollisionPath,
            """
            namespace SampleFixture;

            public class Impl
            {
            }
            """);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClassMover.MoveProjectAsync(sourceProjectPath, targetProjectPath, "SampleFixture.Impl"));

        var sourceContent = await File.ReadAllTextAsync(sourcePath);
        Assert.Contains("class Impl", sourceContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task partialclassのファイル移動で全パーツを同時に移せる()
    {
        var workspaceRoot = CopyFixtureToTemp();
        var csprojPath = Path.Combine(workspaceRoot, "SampleLib", "SampleLib.csproj");
        var part1Path = Path.Combine(workspaceRoot, "SampleLib", "PartialTarget.Part1.cs");
        var part2Path = Path.Combine(workspaceRoot, "SampleLib", "PartialTarget.Part2.cs");
        var relativePath = Path.Combine("Moved", "PartialTargetMoved.cs");

        await File.WriteAllTextAsync(
            part1Path,
            """
            namespace SampleFixture;

            public partial class PartialTarget
            {
                public int PartA() => 1;
            }
            """);

        await File.WriteAllTextAsync(
            part2Path,
            """
            namespace SampleFixture;

            public partial class PartialTarget
            {
                public int PartB() => 2;
            }
            """);

        var result = await ClassMover.MoveFileAsync(csprojPath, "SampleFixture.PartialTarget", relativePath);

        var movedContent = await File.ReadAllTextAsync(result.TargetFilePath);
        Assert.Contains("PartA", movedContent, StringComparison.Ordinal);
        Assert.Contains("PartB", movedContent, StringComparison.Ordinal);

        var sourcePart1Content = await File.ReadAllTextAsync(part1Path);
        var sourcePart2Content = await File.ReadAllTextAsync(part2Path);
        Assert.DoesNotContain("PartialTarget", sourcePart1Content, StringComparison.Ordinal);
        Assert.DoesNotContain("PartialTarget", sourcePart2Content, StringComparison.Ordinal);
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
