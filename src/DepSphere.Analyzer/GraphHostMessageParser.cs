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
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                var nestedJson = root.GetString();
                if (string.IsNullOrWhiteSpace(nestedJson))
                {
                    return false;
                }

                using var nested = JsonDocument.Parse(nestedJson);
                return TryParseObject(nested.RootElement, out message);
            }

            return TryParseObject(root, out message);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseObject(JsonElement root, out GraphHostMessage? message)
    {
        message = null;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!root.TryGetProperty("type", out var typeElement))
        {
            return false;
        }

        var type = typeElement.GetString();
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        string? nodeId = null;
        if (root.TryGetProperty("nodeId", out var nodeIdElement))
        {
            nodeId = nodeIdElement.GetString();
        }

        message = new GraphHostMessage(type, nodeId);
        return true;
    }
}
