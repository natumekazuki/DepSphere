namespace DepSphere.Analyzer;

public sealed record GraphView(IReadOnlyList<GraphViewNode> Nodes, IReadOnlyList<GraphViewEdge> Edges);
