using System.Text.Json;

namespace DepSphere.Analyzer;

public static class GraphViewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(GraphView view)
    {
        return JsonSerializer.Serialize(view, Options);
    }
}
