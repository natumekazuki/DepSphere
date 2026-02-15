namespace DepSphere.Analyzer;

public sealed record AnalysisOptions
{
    public const int DefaultMetricsProgressReportInterval = 25;

    public int MetricsProgressReportInterval { get; init; } = DefaultMetricsProgressReportInterval;

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
}
