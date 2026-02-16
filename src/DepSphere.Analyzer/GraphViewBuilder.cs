using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class GraphViewBuilder
{
    private const string TypeNodeKind = "type";
    private const string MethodNodeKind = "method";
    private const string PropertyNodeKind = "property";
    private const string MemberNodeLevel = "member";
    private const string MemberEdgeKind = "member";
    private static readonly TypeMetrics EmptyMetrics = new(0, 0, 0, 0, 0, 0, 0);

    public static GraphView Build(DependencyGraph graph)
    {
        return Build(graph, options: null);
    }

    public static GraphView Build(DependencyGraph graph, AnalysisOptions? options)
    {
        var effectiveOptions = options ?? new AnalysisOptions();
        var thresholds = effectiveOptions.ValidateLevelThresholds();

        var orderedTypeNodes = graph.Nodes
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        var levelMap = BuildLevelMap(orderedTypeNodes, thresholds.HotspotTopPercent, thresholds.CriticalTopPercent);
        var avgInDegree = orderedTypeNodes.Length == 0
            ? 0
            : orderedTypeNodes.Average(node => node.Metrics.InDegree);
        var typeMemberIndex = BuildTypeMemberIndex(orderedTypeNodes);

        var viewNodes = new List<GraphViewNode>(orderedTypeNodes.Length * 2);
        var typeViewNodes = new List<GraphViewNode>(orderedTypeNodes.Length);

        for (var index = 0; index < orderedTypeNodes.Length; index++)
        {
            var node = orderedTypeNodes[index];
            var level = levelMap[node.Id];
            var size = 8 + node.Metrics.WeightScore * 24;
            var radius = 30 + size * 0.2;
            var angle = orderedTypeNodes.Length == 0 ? 0 : (2 * Math.PI * index) / orderedTypeNodes.Length;

            var x = Math.Cos(angle) * radius;
            var y = Math.Sin(angle) * radius;
            var z = (node.Metrics.InDegree - avgInDegree) * 4;

            typeMemberIndex.TryGetValue(node.Id, out var memberIndex);
            memberIndex ??= TypeMemberIndex.Empty;

            var viewNode = new GraphViewNode(
                node.Id,
                GetSimpleName(node.Id),
                x,
                y,
                z,
                size,
                ToNodeColor(level),
                level,
                node.Metrics)
            {
                NodeKind = TypeNodeKind,
                MethodNames = memberIndex.MethodNames,
                PropertyNames = memberIndex.PropertyNames
            };

            typeViewNodes.Add(viewNode);
            viewNodes.Add(viewNode);
        }

        var edges = graph.Edges
            .Select(edge =>
                new GraphViewEdge(
                    edge.From,
                    edge.To,
                    edge.Kind.ToString().ToLowerInvariant(),
                    ToEdgeColor(edge.Kind)))
            .ToList();

        foreach (var typeNode in typeViewNodes)
        {
            if (!typeMemberIndex.TryGetValue(typeNode.Id, out var memberIndex) || memberIndex is null)
            {
                continue;
            }

            var memberNodes = BuildMemberNodes(typeNode, memberIndex);
            viewNodes.AddRange(memberNodes);

            foreach (var memberNode in memberNodes)
            {
                edges.Add(
                    new GraphViewEdge(
                        typeNode.Id,
                        memberNode.Id,
                        MemberEdgeKind,
                        ToMemberEdgeColor()));
            }
        }

        var orderedEdges = edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind, StringComparer.Ordinal)
            .ToArray();

        return new GraphView(viewNodes, orderedEdges);
    }

    private static IReadOnlyList<GraphViewNode> BuildMemberNodes(GraphViewNode typeNode, TypeMemberIndex index)
    {
        var memberSeeds = index.MethodNames
            .Select(name => new MemberSeed(name, MethodNodeKind))
            .Concat(index.PropertyNames.Select(name => new MemberSeed(name, PropertyNodeKind)))
            .ToArray();
        if (memberSeeds.Length == 0)
        {
            return Array.Empty<GraphViewNode>();
        }

        var orbitRadius = Math.Max(12, typeNode.Size * 0.95);
        var baseSize = Math.Max(3.6, typeNode.Size * 0.34);
        var nodes = new List<GraphViewNode>(memberSeeds.Length);

        for (var indexSeed = 0; indexSeed < memberSeeds.Length; indexSeed++)
        {
            var seed = memberSeeds[indexSeed];
            var angle = (2 * Math.PI * indexSeed) / memberSeeds.Length;
            var x = typeNode.X + Math.Cos(angle) * orbitRadius;
            var y = typeNode.Y + Math.Sin(angle) * orbitRadius;
            var z = typeNode.Z + ((indexSeed % 2 == 0) ? 3.5 : -3.5);
            var size = seed.Kind == MethodNodeKind ? baseSize : baseSize * 0.92;
            var label = seed.Kind == MethodNodeKind ? $"{seed.Name}()" : seed.Name;
            var memberId = BuildMemberNodeId(typeNode.Id, seed.Kind, seed.Name);

            nodes.Add(
                new GraphViewNode(
                    memberId,
                    label,
                    x,
                    y,
                    z,
                    size,
                    ToMemberNodeColor(seed.Kind),
                    MemberNodeLevel,
                    EmptyMetrics)
                {
                    NodeKind = seed.Kind,
                    OwnerNodeId = typeNode.Id
                });
        }

        return nodes;
    }

    private static string BuildMemberNodeId(string ownerNodeId, string memberKind, string memberName)
    {
        return $"{ownerNodeId}::{memberKind}:{memberName}";
    }

    private static Dictionary<string, TypeMemberIndex> BuildTypeMemberIndex(
        IReadOnlyList<DependencyNode> nodes)
    {
        var result = new Dictionary<string, TypeMemberIndex>(StringComparer.Ordinal);
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
                if (type is null)
                {
                    result[node.Id] = TypeMemberIndex.Empty;
                    continue;
                }

                result[node.Id] = new TypeMemberIndex(
                    CollectMethodNames(type),
                    CollectPropertyNames(type));
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

    private static string[] CollectPropertyNames(TypeDeclarationSyntax type)
    {
        return type.Members
            .Select(GetPropertyLikeMemberName)
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

    private static string? GetPropertyLikeMemberName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
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

    private static string ToMemberNodeColor(string memberKind)
    {
        return memberKind switch
        {
            MethodNodeKind => "#38bdf8",
            PropertyNodeKind => "#14b8a6",
            _ => "#64748b"
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

    private static string ToMemberEdgeColor()
    {
        return "#64748b";
    }

    private sealed record MemberSeed(string Name, string Kind);

    private sealed record TypeMemberIndex(IReadOnlyList<string> MethodNames, IReadOnlyList<string> PropertyNames)
    {
        public static readonly TypeMemberIndex Empty = new(Array.Empty<string>(), Array.Empty<string>());
    }
}
