namespace DepSphere.Analyzer;

public sealed record AnalysisOptions
{
    public const int DefaultMetricsProgressReportInterval = 25;
    public const double DefaultWeightMethod = 0.15;
    public const double DefaultWeightStatement = 0.30;
    public const double DefaultWeightBranch = 0.20;
    public const double DefaultWeightCallSite = 0.20;
    public const double DefaultWeightFanOut = 0.10;
    public const double DefaultWeightInDegree = 0.05;
    public const double DefaultHotspotTopPercent = 0.10;
    public const double DefaultCriticalTopPercent = 0.03;

    public int MetricsProgressReportInterval { get; init; } = DefaultMetricsProgressReportInterval;
    public double WeightMethod { get; init; } = DefaultWeightMethod;
    public double WeightStatement { get; init; } = DefaultWeightStatement;
    public double WeightBranch { get; init; } = DefaultWeightBranch;
    public double WeightCallSite { get; init; } = DefaultWeightCallSite;
    public double WeightFanOut { get; init; } = DefaultWeightFanOut;
    public double WeightInDegree { get; init; } = DefaultWeightInDegree;
    public double HotspotTopPercent { get; init; } = DefaultHotspotTopPercent;
    public double CriticalTopPercent { get; init; } = DefaultCriticalTopPercent;

    public int ValidateMetricsProgressReportInterval()
    {
        if (MetricsProgressReportInterval < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MetricsProgressReportInterval),
                MetricsProgressReportInterval,
                "MetricsProgressReportInterval must be greater than or equal to 1.");
        }

        return MetricsProgressReportInterval;
    }

    public (double Method, double Statement, double Branch, double CallSite, double FanOut, double InDegree) GetNormalizedMetricWeights()
    {
        EnsureNonNegative(WeightMethod, nameof(WeightMethod));
        EnsureNonNegative(WeightStatement, nameof(WeightStatement));
        EnsureNonNegative(WeightBranch, nameof(WeightBranch));
        EnsureNonNegative(WeightCallSite, nameof(WeightCallSite));
        EnsureNonNegative(WeightFanOut, nameof(WeightFanOut));
        EnsureNonNegative(WeightInDegree, nameof(WeightInDegree));

        var total =
            WeightMethod +
            WeightStatement +
            WeightBranch +
            WeightCallSite +
            WeightFanOut +
            WeightInDegree;

        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(total),
                total,
                "At least one weight must be greater than 0.");
        }

        return (
            WeightMethod / total,
            WeightStatement / total,
            WeightBranch / total,
            WeightCallSite / total,
            WeightFanOut / total,
            WeightInDegree / total);
    }

    public (double HotspotTopPercent, double CriticalTopPercent) ValidateLevelThresholds()
    {
        EnsurePercentRange(HotspotTopPercent, nameof(HotspotTopPercent));
        EnsurePercentRange(CriticalTopPercent, nameof(CriticalTopPercent));

        if (CriticalTopPercent > HotspotTopPercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CriticalTopPercent),
                CriticalTopPercent,
                "CriticalTopPercent must be less than or equal to HotspotTopPercent.");
        }

        return (HotspotTopPercent, CriticalTopPercent);
    }

    private static void EnsureNonNegative(double value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than or equal to 0.");
        }
    }

    private static void EnsurePercentRange(double value, string name)
    {
        if (value <= 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be in the range (0, 1].");
        }
    }
}
