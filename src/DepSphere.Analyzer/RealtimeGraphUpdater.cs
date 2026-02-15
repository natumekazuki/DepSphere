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

        if (CanUseIncremental(mergedEvents))
        {
            try
            {
                var updatedGraph = ApplyIncremental(currentGraph, mergedEvents);
                var patch = GraphDiffBuilder.Build(currentGraph, updatedGraph);
                return new RealtimeUpdateResult(updatedGraph, patch, mergedEvents);
            }
            catch when (CanFallbackToFull(analysisPath))
            {
                // フォールバックは下の全量解析に委譲
            }
        }

        var fullyUpdatedGraph = await DependencyAnalyzer.AnalyzePathAsync(analysisPath, cancellationToken);
        var fullPatch = GraphDiffBuilder.Build(currentGraph, fullyUpdatedGraph);

        return new RealtimeUpdateResult(fullyUpdatedGraph, fullPatch, mergedEvents);
    }

    private static bool CanUseIncremental(IReadOnlyList<GraphChangeEvent> events)
    {
        foreach (var item in events)
        {
            if (item.Type is GraphChangeEventType.ClassMoved or GraphChangeEventType.DocumentRenamed)
            {
                return false;
            }

            var extension = Path.GetExtension(item.Path);
            if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanFallbackToFull(string analysisPath)
    {
        return !string.IsNullOrWhiteSpace(analysisPath) && File.Exists(analysisPath);
    }

    private static DependencyGraph ApplyIncremental(
        DependencyGraph currentGraph,
        IReadOnlyList<GraphChangeEvent> events)
    {
        var nodeMap = currentGraph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var edgeSet = currentGraph.Edges.ToHashSet();

        foreach (var changeEvent in events)
        {
            var normalizedPath = NormalizePath(changeEvent.Path);

            var affectedNodeIds = nodeMap.Values
                .Where(node => PathEquals(node.Location?.FilePath, normalizedPath))
                .Select(node => node.Id)
                .ToArray();

            var preservedIncoming = edgeSet
                .Where(edge => affectedNodeIds.Contains(edge.To, StringComparer.Ordinal)
                    && !affectedNodeIds.Contains(edge.From, StringComparer.Ordinal))
                .ToArray();

            foreach (var id in affectedNodeIds)
            {
                nodeMap.Remove(id);
            }

            edgeSet.RemoveWhere(edge =>
                affectedNodeIds.Contains(edge.From, StringComparer.Ordinal)
                || affectedNodeIds.Contains(edge.To, StringComparer.Ordinal));

            if (changeEvent.Type == GraphChangeEventType.DocumentRemoved || !File.Exists(normalizedPath))
            {
                continue;
            }

            var analysis = IncrementalFileAnalyzer.Analyze(normalizedPath, nodeMap.Values);
            foreach (var node in analysis.Nodes)
            {
                nodeMap[node.Id] = node;
            }

            foreach (var edge in analysis.Edges)
            {
                if (nodeMap.ContainsKey(edge.From) && nodeMap.ContainsKey(edge.To) && edge.From != edge.To)
                {
                    edgeSet.Add(edge);
                }
            }

            foreach (var incoming in preservedIncoming)
            {
                if (nodeMap.ContainsKey(incoming.From) && nodeMap.ContainsKey(incoming.To))
                {
                    edgeSet.Add(incoming);
                }
            }
        }

        return RecalculateMetrics(nodeMap.Values, edgeSet);
    }

    private static DependencyGraph RecalculateMetrics(
        IEnumerable<DependencyNode> nodes,
        IEnumerable<DependencyEdge> edges)
    {
        var nodeList = nodes.OrderBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var edgeList = edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .ToArray();

        var inDegreeMap = edgeList
            .GroupBy(edge => edge.To, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var fanOutMap = edgeList
            .GroupBy(edge => edge.From, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.To).Distinct(StringComparer.Ordinal).Count(), StringComparer.Ordinal);

        var methodN = Normalize(nodeList.ToDictionary(node => node.Id, node => node.Metrics.MethodCount, StringComparer.Ordinal));
        var statementN = Normalize(nodeList.ToDictionary(node => node.Id, node => node.Metrics.StatementCount, StringComparer.Ordinal));
        var branchN = Normalize(nodeList.ToDictionary(node => node.Id, node => node.Metrics.BranchCount, StringComparer.Ordinal));
        var callSiteN = Normalize(nodeList.ToDictionary(node => node.Id, node => node.Metrics.CallSiteCount, StringComparer.Ordinal));
        var fanOutN = Normalize(nodeList.ToDictionary(node => node.Id, node => fanOutMap.TryGetValue(node.Id, out var fanOut) ? fanOut : 0, StringComparer.Ordinal));
        var inDegreeN = Normalize(nodeList.ToDictionary(node => node.Id, node => inDegreeMap.TryGetValue(node.Id, out var inDegree) ? inDegree : 0, StringComparer.Ordinal));

        var updatedNodes = nodeList
            .Select(node =>
            {
                var fanOut = fanOutMap.TryGetValue(node.Id, out var valueFanOut) ? valueFanOut : 0;
                var inDegree = inDegreeMap.TryGetValue(node.Id, out var valueInDegree) ? valueInDegree : 0;
                var score =
                    0.15 * methodN[node.Id] +
                    0.30 * statementN[node.Id] +
                    0.20 * branchN[node.Id] +
                    0.20 * callSiteN[node.Id] +
                    0.10 * fanOutN[node.Id] +
                    0.05 * inDegreeN[node.Id];

                return node with
                {
                    Metrics = node.Metrics with
                    {
                        FanOut = fanOut,
                        InDegree = inDegree,
                        WeightScore = score
                    }
                };
            })
            .ToArray();

        return new DependencyGraph(updatedNodes, edgeList);
    }

    private static Dictionary<string, double> Normalize(IReadOnlyDictionary<string, int> values)
    {
        var transformed = values.ToDictionary(
            pair => pair.Key,
            pair => Math.Log(1 + pair.Value),
            StringComparer.Ordinal);

        var sorted = transformed.Values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0)
        {
            return transformed.ToDictionary(pair => pair.Key, _ => 0.0, StringComparer.Ordinal);
        }

        var index = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
        index = Math.Clamp(index, 0, sorted.Length - 1);
        var p95 = sorted[index];
        if (p95 <= 0)
        {
            p95 = 1;
        }

        return transformed.ToDictionary(
            pair => pair.Key,
            pair => Math.Min(pair.Value, p95) / p95,
            StringComparer.Ordinal);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    private static bool PathEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }
}
