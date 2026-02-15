namespace DepSphere.Analyzer;

public sealed record SourceCodeDocument(
    string FilePath,
    int StartLine,
    int EndLine,
    string Content);
