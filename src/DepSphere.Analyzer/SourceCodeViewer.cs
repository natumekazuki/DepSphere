namespace DepSphere.Analyzer;

public static class SourceCodeViewer
{
    public static SourceCodeDocument OpenNode(DependencyGraph graph, string nodeId, int contextLines = 3)
    {
        var node = graph.Nodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is null)
        {
            throw new InvalidOperationException($"Node not found: {nodeId}");
        }

        if (node.Location is null)
        {
            throw new InvalidOperationException($"Source location is not available: {nodeId}");
        }

        if (string.IsNullOrWhiteSpace(node.Location.FilePath) || !File.Exists(node.Location.FilePath))
        {
            throw new InvalidOperationException($"Source file does not exist: {node.Location.FilePath}");
        }

        var lines = File.ReadAllLines(node.Location.FilePath);
        if (lines.Length == 0)
        {
            return new SourceCodeDocument(node.Location.FilePath, 1, 1, string.Empty);
        }

        var startLine = Math.Max(1, node.Location.StartLine - contextLines);
        var endLine = Math.Min(lines.Length, node.Location.EndLine + contextLines);
        var content = string.Join(Environment.NewLine, lines[(startLine - 1)..endLine]);

        return new SourceCodeDocument(node.Location.FilePath, startLine, endLine, content);
    }
}
