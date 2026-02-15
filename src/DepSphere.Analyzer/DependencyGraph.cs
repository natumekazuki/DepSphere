namespace DepSphere.Analyzer;

public sealed record DependencyGraph(IReadOnlyList<DependencyNode> Nodes, IReadOnlyList<DependencyEdge> Edges);
