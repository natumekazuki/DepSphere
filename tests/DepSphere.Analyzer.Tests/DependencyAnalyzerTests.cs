using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class DependencyAnalyzerTests
{
    private const string Source = """
        namespace Sample;

        public interface IService { }

        public class Base { }

        public class Dependency
        {
            public static void Touch() { }
        }

        public class Impl : Base, IService
        {
            private readonly Dependency _field = new();

            public Dependency Prop { get; set; } = new();

            public Dependency Method(Dependency arg)
            {
                Dependency.Touch();
                return arg;
            }
        }
        """;

    [Fact]
    public void 継承依存を抽出できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        Assert.Contains(
            graph.Edges,
            edge => edge.From == "Sample.Impl" && edge.To == "Sample.Base" && edge.Kind == DependencyKind.Inherit);
    }

    [Fact]
    public void 実装依存を抽出できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        Assert.Contains(
            graph.Edges,
            edge => edge.From == "Sample.Impl" && edge.To == "Sample.IService" && edge.Kind == DependencyKind.Implement);
    }

    [Fact]
    public void 参照依存を抽出できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        Assert.Contains(
            graph.Edges,
            edge => edge.From == "Sample.Impl" && edge.To == "Sample.Dependency" && edge.Kind == DependencyKind.Reference);
    }

    [Fact]
    public void 重みメトリクスを算出できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        var impl = Assert.Single(graph.Nodes.Where(node => node.Id == "Sample.Impl"));
        var dependency = Assert.Single(graph.Nodes.Where(node => node.Id == "Sample.Dependency"));

        Assert.True(impl.Metrics.MethodCount >= 1);
        Assert.True(impl.Metrics.StatementCount >= 1);
        Assert.True(impl.Metrics.BranchCount >= 0);
        Assert.True(impl.Metrics.CallSiteCount >= 1);
        Assert.True(impl.Metrics.FanOut >= 1);
        Assert.True(impl.Metrics.WeightScore > dependency.Metrics.WeightScore);
    }

    [Fact]
    public async Task csprojパスから解析できる()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath);

        Assert.Contains(
            graph.Edges,
            edge => edge.From == "SampleFixture.Impl" && edge.To == "SampleFixture.Base" && edge.Kind == DependencyKind.Inherit);
    }

    [Fact]
    public async Task slnパスから解析できる()
    {
        var slnPath = GetFixturePath("SampleWorkspace.sln");
        var graph = await DependencyAnalyzer.AnalyzePathAsync(slnPath);

        Assert.Contains(
            graph.Edges,
            edge => edge.From == "SampleFixture.Impl" && edge.To == "SampleFixture.IService" && edge.Kind == DependencyKind.Implement);
    }

    [Fact]
    public async Task 解析進捗を通知できる()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var progress = new CollectingProgress<AnalysisProgress>();

        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath, progress, CancellationToken.None);

        Assert.NotEmpty(graph.Nodes);
        Assert.Contains(progress.Items, item => item.Stage == "prepare");
        Assert.Contains(progress.Items, item => item.Stage == "load");
        Assert.Contains(progress.Items, item => item.Stage == "compile");
        Assert.Contains(progress.Items, item => item.Stage == "metrics");
        Assert.Contains(progress.Items, item => item.Stage == "complete");
    }

    [Fact]
    public async Task 進捗更新間隔が不正なら例外になる()
    {
        var csprojPath = GetFixturePath("SampleLib", "SampleLib.csproj");
        var options = new AnalysisOptions
        {
            MetricsProgressReportInterval = 0
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            _ = await DependencyAnalyzer.AnalyzePathAsync(csprojPath, options, progress: null, cancellationToken: CancellationToken.None);
        });
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

    private sealed class CollectingProgress<T> : IProgress<T>
    {
        public List<T> Items { get; } = [];

        public void Report(T value)
        {
            Items.Add(value);
        }
    }
}
