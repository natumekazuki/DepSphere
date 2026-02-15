namespace DepSphere.Analyzer;

public static class GraphViewBuilder
{
    public static GraphView Build(DependencyGraph graph)
    {
        var orderedNodes = graph.Nodes
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        var levelMap = BuildLevelMap(orderedNodes);
        var avgInDegree = orderedNodes.Length == 0
            ? 0
            : orderedNodes.Average(node => node.Metrics.InDegree);

        var nodes = new List<GraphViewNode>(orderedNodes.Length);
        for (var index = 0; index < orderedNodes.Length; index++)
        {
            var node = orderedNodes[index];
            var level = levelMap[node.Id];
            var size = 8 + node.Metrics.WeightScore * 24;
            var radius = 30 + size * 0.2;
            var angle = orderedNodes.Length == 0 ? 0 : (2 * Math.PI * index) / orderedNodes.Length;

            var x = Math.Cos(angle) * radius;
            var y = Math.Sin(angle) * radius;
            var z = (node.Metrics.InDegree - avgInDegree) * 4;

            nodes.Add(
                new GraphViewNode(
                    node.Id,
                    node.Id,
                    x,
                    y,
                    z,
                    size,
                    ToNodeColor(level),
                    level,
                    node.Metrics));
        }

        var edges = graph.Edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .Select(edge =>
                new GraphViewEdge(
                    edge.From,
                    edge.To,
                    edge.Kind.ToString().ToLowerInvariant(),
                    ToEdgeColor(edge.Kind)))
            .ToArray();

        return new GraphView(nodes, edges);
    }

    private static Dictionary<string, string> BuildLevelMap(IReadOnlyList<DependencyNode> nodes)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (nodes.Count == 0)
        {
            return map;
        }

        var ranked = nodes
            .OrderByDescending(node => node.Metrics.WeightScore)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        var criticalCount = Math.Max(1, (int)Math.Ceiling(ranked.Length * 0.03));
        var hotspotCount = Math.Max(1, (int)Math.Ceiling(ranked.Length * 0.10));
        hotspotCount = Math.Max(hotspotCount, criticalCount);

        for (var i = 0; i < ranked.Length; i++)
        {
            var level = i < criticalCount
                ? "critical"
                : i < hotspotCount
                    ? "hotspot"
                    : "normal";
            map[ranked[i].Id] = level;
        }

        return map;
    }

    private static string ToNodeColor(string level)
    {
        return level switch
        {
            "critical" => "#ef4444",
            "hotspot" => "#f97316",
            _ => "#3b82f6"
        };
    }

    private static string ToEdgeColor(DependencyKind kind)
    {
        return kind switch
        {
            DependencyKind.Inherit => "#22c55e",
            DependencyKind.Implement => "#f59e0b",
            _ => "#94a3b8"
        };
    }
}
