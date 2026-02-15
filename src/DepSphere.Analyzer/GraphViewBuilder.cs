using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class GraphViewBuilder
{
    public static GraphView Build(DependencyGraph graph)
    {
        return Build(graph, options: null);
    }

    public static GraphView Build(DependencyGraph graph, AnalysisOptions? options)
    {
        var effectiveOptions = options ?? new AnalysisOptions();
        var thresholds = effectiveOptions.ValidateLevelThresholds();

        var orderedNodes = graph.Nodes
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        var levelMap = BuildLevelMap(orderedNodes, thresholds.HotspotTopPercent, thresholds.CriticalTopPercent);
        var avgInDegree = orderedNodes.Length == 0
            ? 0
            : orderedNodes.Average(node => node.Metrics.InDegree);
        var methodNameIndex = BuildMethodNameIndex(orderedNodes);

        var nodes = new List<GraphViewNode>(orderedNodes.Length);
        for (var index = 0; index < orderedNodes.Length; index++)
        {
            var node = orderedNodes[index];
            var level = levelMap[node.Id];
            var size = 8 + node.Metrics.WeightScore * 24;
            var radius = 30 + size * 0.2;
            var angle = orderedNodes.Length == 0 ? 0 : (2 * Math.PI * index) / orderedNodes.Length;

            var x = Math.Cos(angle) * radius;
            var y = Math.Sin(angle) * radius;
            var z = (node.Metrics.InDegree - avgInDegree) * 4;

            nodes.Add(
                new GraphViewNode(
                    node.Id,
                    GetSimpleName(node.Id),
                    x,
                    y,
                    z,
                    size,
                    ToNodeColor(level),
                    level,
                    node.Metrics));
            nodes[^1] = nodes[^1] with
            {
                MethodNames = methodNameIndex.TryGetValue(node.Id, out var names)
                    ? names
                    : Array.Empty<string>()
            };
        }

        var edges = graph.Edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .Select(edge =>
                new GraphViewEdge(
                    edge.From,
                    edge.To,
                    edge.Kind.ToString().ToLowerInvariant(),
                    ToEdgeColor(edge.Kind)))
            .ToArray();

        return new GraphView(nodes, edges);
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildMethodNameIndex(
        IReadOnlyList<DependencyNode> nodes)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var nodesByFile = nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Location?.FilePath))
            .GroupBy(node => node.Location!.FilePath!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in nodesByFile)
        {
            var file = group.Key;
            if (!File.Exists(file))
            {
                continue;
            }

            string sourceText;
            try
            {
                sourceText = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: file);
            var root = syntaxTree.GetRoot();
            var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();

            foreach (var node in group)
            {
                var type = FindBestMatchingType(types, node);
                result[node.Id] = type is null
                    ? Array.Empty<string>()
                    : CollectMethodNames(type);
            }
        }

        return result;
    }

    private static TypeDeclarationSyntax? FindBestMatchingType(
        IReadOnlyList<TypeDeclarationSyntax> types,
        DependencyNode node)
    {
        if (types.Count == 0)
        {
            return null;
        }

        var expectedName = RemoveGenericSuffix(GetSimpleName(node.Id));
        var byName = types
            .Where(type => string.Equals(type.Identifier.ValueText, expectedName, StringComparison.Ordinal))
            .ToArray();

        var candidates = byName.Length > 0 ? byName : types.ToArray();
        var locationLine = node.Location?.StartLine;
        if (locationLine is null || locationLine.Value <= 0)
        {
            return candidates[0];
        }

        var onLine = candidates
            .Where(type => ContainsLine(type, locationLine.Value))
            .ToArray();
        if (onLine.Length > 0)
        {
            return onLine[0];
        }

        return candidates
            .OrderBy(type => Math.Abs(GetStartLine(type) - locationLine.Value))
            .FirstOrDefault();
    }

    private static bool ContainsLine(TypeDeclarationSyntax type, int line)
    {
        var span = type.SyntaxTree.GetLineSpan(type.Span);
        var start = span.StartLinePosition.Line + 1;
        var end = span.EndLinePosition.Line + 1;
        return line >= start && line <= end;
    }

    private static int GetStartLine(TypeDeclarationSyntax type)
    {
        var span = type.SyntaxTree.GetLineSpan(type.Span);
        return span.StartLinePosition.Line + 1;
    }

    private static string[] CollectMethodNames(TypeDeclarationSyntax type)
    {
        return type.Members
            .Select(GetMethodLikeMemberName)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? GetMethodLikeMemberName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax ctor => ctor.Identifier.ValueText,
            _ => null
        };
    }

    private static string GetSimpleName(string typeId)
    {
        var lastDot = typeId.LastIndexOf('.');
        return lastDot >= 0 ? typeId[(lastDot + 1)..] : typeId;
    }

    private static string RemoveGenericSuffix(string typeName)
    {
        var marker = typeName.IndexOf('<');
        return marker >= 0 ? typeName[..marker] : typeName;
    }

    private static Dictionary<string, string> BuildLevelMap(
        IReadOnlyList<DependencyNode> nodes,
        double hotspotTopPercent,
        double criticalTopPercent)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (nodes.Count == 0)
        {
            return map;
        }

        var ranked = nodes
            .OrderByDescending(node => node.Metrics.WeightScore)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        var criticalCount = Math.Max(1, (int)Math.Ceiling(ranked.Length * criticalTopPercent));
        var hotspotCount = Math.Max(1, (int)Math.Ceiling(ranked.Length * hotspotTopPercent));
        hotspotCount = Math.Max(hotspotCount, criticalCount);

        for (var i = 0; i < ranked.Length; i++)
        {
            var level = i < criticalCount
                ? "critical"
                : i < hotspotCount
                    ? "hotspot"
                    : "normal";
            map[ranked[i].Id] = level;
        }

        return map;
    }

    private static string ToNodeColor(string level)
    {
        return level switch
        {
            "critical" => "#ef4444",
            "hotspot" => "#f97316",
            _ => "#3b82f6"
        };
    }

    private static string ToEdgeColor(DependencyKind kind)
    {
        return kind switch
        {
            DependencyKind.Inherit => "#22c55e",
            DependencyKind.Implement => "#f59e0b",
            _ => "#94a3b8"
        };
    }
}
