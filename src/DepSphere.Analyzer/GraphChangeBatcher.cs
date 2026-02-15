namespace DepSphere.Analyzer;

public static class GraphChangeBatcher
{
    public static IReadOnlyList<GraphChangeEvent> Merge(IEnumerable<GraphChangeEvent> events)
    {
        var merged = new Dictionary<string, GraphChangeEvent>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in events)
        {
            if (string.IsNullOrWhiteSpace(item.Path))
            {
                continue;
            }

            merged[item.Path] = item;
        }

        return merged.Values
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
