namespace DepSphere.Analyzer;

public sealed record GraphChangeEvent(
    GraphChangeEventType Type,
    string Path,
    DateTimeOffset? OccurredAt = null);
