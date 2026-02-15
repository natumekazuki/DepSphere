namespace DepSphere.Analyzer;

public static class GraphSelectionCoordinator
{
    public static bool TryOpenFromMessage(DependencyGraph graph, string rawMessage, out SourceCodeDocument? document)
    {
        document = null;

        if (!GraphHostMessageParser.TryParse(rawMessage, out var message) || message is null)
        {
            return false;
        }

        if (!string.Equals(message.Type, "nodeSelected", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.NodeId))
        {
            return false;
        }

        document = SourceCodeViewer.OpenNode(graph, message.NodeId);
        return true;
    }
}
