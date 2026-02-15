namespace DepSphere.Analyzer;

public sealed record MoveProjectResult(
    string SourceFilePath,
    string TargetFilePath,
    string SourceProjectPath,
    string TargetProjectPath);
