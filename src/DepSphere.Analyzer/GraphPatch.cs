namespace DepSphere.Analyzer;

public sealed record GraphPatch(
    IReadOnlyList<DependencyNode> UpsertNodes,
    IReadOnlyList<string> RemoveNodeIds,
    IReadOnlyList<DependencyEdge> UpsertEdges,
    IReadOnlyList<DependencyEdge> RemoveEdges)
{
    public static readonly GraphPatch Empty = new(
        Array.Empty<DependencyNode>(),
        Array.Empty<string>(),
        Array.Empty<DependencyEdge>(),
        Array.Empty<DependencyEdge>());
}
