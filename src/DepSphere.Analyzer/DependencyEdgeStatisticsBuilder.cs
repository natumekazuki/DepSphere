namespace DepSphere.Analyzer;

public static class DependencyEdgeStatisticsBuilder
{
    public static DependencyEdgeStatistics Build(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var nodeCount = graph.Nodes.Count;
        var edgeCount = graph.Edges.Count;
        var possibleDirectedEdgeCount = nodeCount <= 1 ? 0 : nodeCount * (nodeCount - 1);
        var overallDensity = Divide(edgeCount, possibleDirectedEdgeCount);

        var kindStats = Enum
            .GetValues<DependencyKind>()
            .Select(kind =>
            {
                var count = graph.Edges.Count(edge => edge.Kind == kind);
                return new DependencyEdgeKindStat(kind, count, Divide(count, possibleDirectedEdgeCount));
            })
            .ToArray();

        return new DependencyEdgeStatistics(
            nodeCount,
            edgeCount,
            possibleDirectedEdgeCount,
            overallDensity,
            kindStats);
    }

    private static double Divide(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0;
        }

        return (double)numerator / denominator;
    }
}
