using System.Text.Json;
using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class GraphViewBuilderTests
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
    public void 三次元表示用グラフへ変換できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        var view = GraphViewBuilder.Build(graph);

        Assert.Equal(graph.Nodes.Count, view.Nodes.Count);
        Assert.Equal(graph.Edges.Count, view.Edges.Count);
        Assert.All(view.Nodes, node =>
        {
            Assert.False(string.IsNullOrWhiteSpace(node.Id));
            Assert.True(node.Size > 0);
        });
    }

    [Fact]
    public void 三次元表示用グラフをJSONへ変換できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });
        var view = GraphViewBuilder.Build(graph);

        var json = GraphViewJsonSerializer.Serialize(view);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("nodes", out var nodes));
        Assert.True(doc.RootElement.TryGetProperty("edges", out var edges));
        Assert.True(nodes.GetArrayLength() > 0);
        Assert.True(edges.GetArrayLength() > 0);
    }

    [Fact]
    public void Hotspot閾値を変更するとレベル判定件数が変わる()
    {
        var nodes = Enumerable.Range(1, 10)
            .Select(index =>
                new DependencyNode(
                    $"Node{index}",
                    new TypeMetrics(0, 0, 0, 0, 0, 0, 100 - index)))
            .ToArray();
        var graph = new DependencyGraph(nodes, Array.Empty<DependencyEdge>());
        var options = new AnalysisOptions
        {
            HotspotTopPercent = 0.40,
            CriticalTopPercent = 0.20
        };

        var view = GraphViewBuilder.Build(graph, options);

        Assert.Equal(2, view.Nodes.Count(node => node.Level == "critical"));
        Assert.Equal(2, view.Nodes.Count(node => node.Level == "hotspot"));
        Assert.Equal(6, view.Nodes.Count(node => node.Level == "normal"));
    }

    [Fact]
    public void Critical閾値がHotspot閾値より大きい場合は例外になる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });
        var options = new AnalysisOptions
        {
            HotspotTopPercent = 0.10,
            CriticalTopPercent = 0.20
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = GraphViewBuilder.Build(graph, options);
        });
    }
}
