namespace DepSphere.Analyzer;

public sealed record AnalysisProgress(
    string Stage,
    string Message,
    int? Current = null,
    int? Total = null);
