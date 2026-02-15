using System.Text.Json;

namespace DepSphere.Analyzer;

public static class GraphHostMessageParser
{
    public static bool TryParse(string rawMessage, out GraphHostMessage? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawMessage);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return false;
            }

            var type = typeElement.GetString();
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            string? nodeId = null;
            if (doc.RootElement.TryGetProperty("nodeId", out var nodeIdElement))
            {
                nodeId = nodeIdElement.GetString();
            }

            message = new GraphHostMessage(type, nodeId);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
