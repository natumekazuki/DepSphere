namespace DepSphere.Analyzer;

public sealed class RealtimeGraphUpdater
{
    public async Task<RealtimeUpdateResult> UpdateAsync(
        string analysisPath,
        DependencyGraph currentGraph,
        IEnumerable<GraphChangeEvent> events,
        CancellationToken cancellationToken = default)
    {
        var mergedEvents = GraphChangeBatcher.Merge(events);
        if (mergedEvents.Count == 0)
        {
            return new RealtimeUpdateResult(currentGraph, GraphPatch.Empty, mergedEvents);
        }

        var updatedGraph = await DependencyAnalyzer.AnalyzePathAsync(analysisPath, cancellationToken);
        var patch = GraphDiffBuilder.Build(currentGraph, updatedGraph);

        return new RealtimeUpdateResult(updatedGraph, patch, mergedEvents);
    }
}
