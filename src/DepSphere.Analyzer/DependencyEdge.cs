namespace DepSphere.Analyzer;

public sealed record DependencyEdge(string From, string To, DependencyKind Kind);
