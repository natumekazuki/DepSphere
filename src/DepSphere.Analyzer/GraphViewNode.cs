namespace DepSphere.Analyzer;

public sealed record GraphViewNode(
    string Id,
    string Label,
    double X,
    double Y,
    double Z,
    double Size,
    string Color,
    string Level,
    TypeMetrics Metrics);
