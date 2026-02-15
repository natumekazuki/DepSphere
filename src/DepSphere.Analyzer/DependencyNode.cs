namespace DepSphere.Analyzer;

public sealed record DependencyNode(string Id, TypeMetrics Metrics)
{
    public SourceLocation? Location { get; init; }
}
