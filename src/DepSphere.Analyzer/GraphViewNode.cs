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
    TypeMetrics Metrics)
{
    public string NodeKind { get; init; } = "type";
    public string? OwnerNodeId { get; init; }
    public IReadOnlyList<string> MethodNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PropertyNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FieldNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EventNames { get; init; } = Array.Empty<string>();
}
