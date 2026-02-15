namespace DepSphere.Analyzer;

public sealed record DependencyEdgeKindStat(
    DependencyKind Kind,
    int Count,
    double Density);
