namespace DepSphere.Analyzer;

public static class GraphDiffBuilder
{
    public static GraphPatch Build(DependencyGraph oldGraph, DependencyGraph newGraph)
    {
        var oldNodes = oldGraph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var newNodes = newGraph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        var upsertNodes = newNodes
            .Where(pair => !oldNodes.TryGetValue(pair.Key, out var oldNode) || oldNode != pair.Value)
            .Select(pair => pair.Value)
            .ToArray();

        var removeNodeIds = oldNodes
            .Where(pair => !newNodes.ContainsKey(pair.Key))
            .Select(pair => pair.Key)
            .ToArray();

        var oldEdges = oldGraph.Edges.ToHashSet();
        var newEdges = newGraph.Edges.ToHashSet();

        var upsertEdges = newEdges
            .Where(edge => !oldEdges.Contains(edge))
            .ToArray();

        var removeEdges = oldEdges
            .Where(edge => !newEdges.Contains(edge))
            .ToArray();

        return new GraphPatch(upsertNodes, removeNodeIds, upsertEdges, removeEdges);
    }
}
