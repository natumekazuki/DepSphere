namespace DepSphere.Analyzer;

public static class GraphViewScriptCommandBuilder
{
    public static string BuildFocusNodeScript(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node id is required.", nameof(nodeId));
        }

        var escaped = nodeId.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);

        return $"window.depSphereFocusNode && window.depSphereFocusNode('{escaped}');";
    }
}
