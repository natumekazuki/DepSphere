using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class DependencyEdgeStatisticsTests
{
    [Fact]
    public void エッジ種別ごとの件数と密度を算出できる()
    {
        var graph = new DependencyGraph(
            new[]
            {
                new DependencyNode("A", new TypeMetrics(0, 0, 0, 0, 0, 0, 0)),
                new DependencyNode("B", new TypeMetrics(0, 0, 0, 0, 0, 0, 0)),
                new DependencyNode("C", new TypeMetrics(0, 0, 0, 0, 0, 0, 0))
            },
            new[]
            {
                new DependencyEdge("A", "B", DependencyKind.Reference),
                new DependencyEdge("A", "C", DependencyKind.Reference),
                new DependencyEdge("B", "C", DependencyKind.Inherit),
                new DependencyEdge("C", "A", DependencyKind.Implement)
            });

        var stats = DependencyEdgeStatisticsBuilder.Build(graph);

        Assert.Equal(3, stats.NodeCount);
        Assert.Equal(4, stats.EdgeCount);
        Assert.Equal(6, stats.PossibleDirectedEdgeCount);
        Assert.Equal(4d / 6d, stats.OverallDensity, 6);

        var reference = Assert.Single(stats.KindStats.Where(item => item.Kind == DependencyKind.Reference));
        var inherit = Assert.Single(stats.KindStats.Where(item => item.Kind == DependencyKind.Inherit));
        var implement = Assert.Single(stats.KindStats.Where(item => item.Kind == DependencyKind.Implement));

        Assert.Equal(2, reference.Count);
        Assert.Equal(1, inherit.Count);
        Assert.Equal(1, implement.Count);
        Assert.Equal(2d / 6d, reference.Density, 6);
        Assert.Equal(1d / 6d, inherit.Density, 6);
        Assert.Equal(1d / 6d, implement.Density, 6);
    }

    [Fact]
    public void ノード数が1以下なら密度は0になる()
    {
        var graph = new DependencyGraph(
            new[]
            {
                new DependencyNode("Solo", new TypeMetrics(0, 0, 0, 0, 0, 0, 0))
            },
            new[]
            {
                new DependencyEdge("Solo", "Solo", DependencyKind.Reference)
            });

        var stats = DependencyEdgeStatisticsBuilder.Build(graph);

        Assert.Equal(1, stats.NodeCount);
        Assert.Equal(1, stats.EdgeCount);
        Assert.Equal(0, stats.PossibleDirectedEdgeCount);
        Assert.Equal(0, stats.OverallDensity);
        Assert.All(stats.KindStats, item => Assert.Equal(0, item.Density));
    }
}
