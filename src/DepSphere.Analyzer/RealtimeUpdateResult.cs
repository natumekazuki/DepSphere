namespace DepSphere.Analyzer;

public sealed record RealtimeUpdateResult(
    DependencyGraph UpdatedGraph,
    GraphPatch Patch,
    IReadOnlyList<GraphChangeEvent> AppliedEvents);
