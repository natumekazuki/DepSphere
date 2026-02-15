namespace DepSphere.Analyzer;

public sealed record IncrementalFileAnalysis(
    IReadOnlyList<DependencyNode> Nodes,
    IReadOnlyList<DependencyEdge> Edges);
