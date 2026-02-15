namespace DepSphere.Analyzer;

public sealed record DependencyEdgeStatistics(
    int NodeCount,
    int EdgeCount,
    int PossibleDirectedEdgeCount,
    double OverallDensity,
    IReadOnlyList<DependencyEdgeKindStat> KindStats);
