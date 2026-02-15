namespace DepSphere.Analyzer;

public sealed record SourceLocation(
    string FilePath,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
