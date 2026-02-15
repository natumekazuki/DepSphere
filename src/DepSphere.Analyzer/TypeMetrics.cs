namespace DepSphere.Analyzer;

public sealed record TypeMetrics(
    int MethodCount,
    int StatementCount,
    int BranchCount,
    int CallSiteCount,
    int FanOut,
    int InDegree,
    double WeightScore);
