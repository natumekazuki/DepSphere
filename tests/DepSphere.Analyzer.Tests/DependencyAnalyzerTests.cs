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
}
