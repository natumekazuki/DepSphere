using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class GraphViewBuilder
{
    private const string ProjectNodeKind = "project";
    private const string NamespaceNodeKind = "namespace";
    private const string FileNodeKind = "file";
    private const string TypeNodeKind = "type";
    private const string MethodNodeKind = "method";
    private const string PropertyNodeKind = "property";
    private const string FieldNodeKind = "field";
    private const string EventNodeKind = "event";
    private const string ExternalNodeKind = "external";

    private const string ContainsEdgeKind = "contains";
    private const string MemberEdgeKind = "member";
    private const string ExternalEdgeKind = "external";
    private static readonly TypeMetrics EmptyMetrics = new(0, 0, 0, 0, 0, 0, 0);
    private static readonly HashSet<string> PrimitiveTypeNames = new(StringComparer.Ordinal)
    {
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "nint", "nuint", "char", "float", "double", "decimal", "string", "object",
        "dynamic", "void"
    };

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
        var typeMetaIndex = BuildTypeMetaIndex(orderedTypeNodes);

        var viewNodes = new List<GraphViewNode>(orderedTypeNodes.Length * 3);
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

            typeMetaIndex.TryGetValue(node.Id, out var meta);
            meta ??= TypeNodeMeta.Empty;

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
                MethodNames = meta.MethodNames,
                PropertyNames = meta.PropertyNames,
                FieldNames = meta.FieldNames,
                EventNames = meta.EventNames
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
            .ToHashSet();

        var projectNodeIds = new HashSet<string>(StringComparer.Ordinal);
        var namespaceNodeIds = new HashSet<string>(StringComparer.Ordinal);
        var fileNodeIds = new HashSet<string>(StringComparer.Ordinal);
        var externalNodeIds = new HashSet<string>(StringComparer.Ordinal);
        var namespaceLabelMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var fileLabelMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var externalLabelMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var typeNode in typeViewNodes)
        {
            typeMetaIndex.TryGetValue(typeNode.Id, out var meta);
            meta ??= TypeNodeMeta.Empty;

            if (!string.IsNullOrWhiteSpace(meta.ProjectName))
            {
                var projectNodeId = BuildProjectNodeId(meta.ProjectName);
                projectNodeIds.Add(projectNodeId);
            }

            if (!string.IsNullOrWhiteSpace(meta.NamespaceName))
            {
                var namespaceNodeId = BuildNamespaceNodeId(meta.NamespaceName);
                namespaceNodeIds.Add(namespaceNodeId);
                namespaceLabelMap[namespaceNodeId] = meta.NamespaceName;
            }

            if (!string.IsNullOrWhiteSpace(meta.FilePath))
            {
                var fileNodeId = BuildFileNodeId(meta.FilePath);
                fileNodeIds.Add(fileNodeId);
                fileLabelMap[fileNodeId] = Path.GetFileName(meta.FilePath);
            }

            if (!string.IsNullOrWhiteSpace(meta.ProjectName) && !string.IsNullOrWhiteSpace(meta.NamespaceName))
            {
                edges.Add(
                    new GraphViewEdge(
                        BuildProjectNodeId(meta.ProjectName),
                        BuildNamespaceNodeId(meta.NamespaceName),
                        ContainsEdgeKind,
                        ToContainsEdgeColor()));
            }

            if (!string.IsNullOrWhiteSpace(meta.NamespaceName) && !string.IsNullOrWhiteSpace(meta.FilePath))
            {
                edges.Add(
                    new GraphViewEdge(
                        BuildNamespaceNodeId(meta.NamespaceName),
                        BuildFileNodeId(meta.FilePath),
                        ContainsEdgeKind,
                        ToContainsEdgeColor()));
            }

            if (!string.IsNullOrWhiteSpace(meta.FilePath))
            {
                edges.Add(
                    new GraphViewEdge(
                        BuildFileNodeId(meta.FilePath),
                        typeNode.Id,
                        ContainsEdgeKind,
                        ToContainsEdgeColor()));
            }
            else if (!string.IsNullOrWhiteSpace(meta.NamespaceName))
            {
                edges.Add(
                    new GraphViewEdge(
                        BuildNamespaceNodeId(meta.NamespaceName),
                        typeNode.Id,
                        ContainsEdgeKind,
                        ToContainsEdgeColor()));
            }

            var memberNodes = BuildMemberNodes(typeNode, meta);
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

            foreach (var externalRoot in meta.ExternalRoots)
            {
                var externalNodeId = BuildExternalNodeId(externalRoot);
                externalNodeIds.Add(externalNodeId);
                externalLabelMap[externalNodeId] = externalRoot;

                edges.Add(
                    new GraphViewEdge(
                        typeNode.Id,
                        externalNodeId,
                        ExternalEdgeKind,
                        ToExternalEdgeColor()));
            }
        }

        var projectNodes = BuildProjectNodes(projectNodeIds);
        var namespaceNodes = BuildNamespaceNodes(namespaceNodeIds, namespaceLabelMap);
        var fileNodes = BuildFileNodes(fileNodeIds, fileLabelMap);
        var externalNodes = BuildExternalNodes(externalNodeIds, externalLabelMap);

        viewNodes.AddRange(projectNodes);
        viewNodes.AddRange(namespaceNodes);
        viewNodes.AddRange(fileNodes);
        viewNodes.AddRange(externalNodes);

        var allNodeIds = viewNodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);

        var orderedEdges = edges
            .Where(edge => allNodeIds.Contains(edge.From) && allNodeIds.Contains(edge.To))
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind, StringComparer.Ordinal)
            .ToArray();

        var orderedNodes = viewNodes
            .OrderBy(node => NodeKindOrder(node.NodeKind))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        return new GraphView(orderedNodes, orderedEdges);
    }

    private static IReadOnlyList<GraphViewNode> BuildProjectNodes(IReadOnlyCollection<string> ids)
    {
        return BuildRingNodes(
            ids,
            radius: 180,
            baseZ: 16,
            size: 12,
            color: "#0ea5e9",
            kind: ProjectNodeKind,
            level: ProjectNodeKind,
            labelFactory: id => id.Replace("project::", string.Empty, StringComparison.Ordinal));
    }

    private static IReadOnlyList<GraphViewNode> BuildNamespaceNodes(
        IReadOnlyCollection<string> ids,
        IReadOnlyDictionary<string, string> labelMap)
    {
        return BuildRingNodes(
            ids,
            radius: 145,
            baseZ: 10,
            size: 8.8,
            color: "#22d3ee",
            kind: NamespaceNodeKind,
            level: NamespaceNodeKind,
            labelFactory: id => labelMap.TryGetValue(id, out var label)
                ? label
                : id.Replace("namespace::", string.Empty, StringComparison.Ordinal));
    }

    private static IReadOnlyList<GraphViewNode> BuildFileNodes(
        IReadOnlyCollection<string> ids,
        IReadOnlyDictionary<string, string> labelMap)
    {
        return BuildRingNodes(
            ids,
            radius: 112,
            baseZ: 6,
            size: 7.6,
            color: "#06b6d4",
            kind: FileNodeKind,
            level: FileNodeKind,
            labelFactory: id => labelMap.TryGetValue(id, out var label)
                ? label
                : id.Replace("file::", string.Empty, StringComparison.Ordinal));
    }

    private static IReadOnlyList<GraphViewNode> BuildExternalNodes(
        IReadOnlyCollection<string> ids,
        IReadOnlyDictionary<string, string> labelMap)
    {
        return BuildRingNodes(
            ids,
            radius: 205,
            baseZ: -20,
            size: 10.5,
            color: "#f59e0b",
            kind: ExternalNodeKind,
            level: ExternalNodeKind,
            labelFactory: id => labelMap.TryGetValue(id, out var label)
                ? $"ext:{label}"
                : id.Replace("external::", string.Empty, StringComparison.Ordinal));
    }

    private static IReadOnlyList<GraphViewNode> BuildRingNodes(
        IReadOnlyCollection<string> ids,
        double radius,
        double baseZ,
        double size,
        string color,
        string kind,
        string level,
        Func<string, string> labelFactory)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<GraphViewNode>();
        }

        var ordered = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var result = new List<GraphViewNode>(ordered.Length);
        for (var index = 0; index < ordered.Length; index++)
        {
            var id = ordered[index];
            var angle = (2 * Math.PI * index) / ordered.Length;
            var x = Math.Cos(angle) * radius;
            var y = Math.Sin(angle) * radius;
            var z = baseZ + ((index % 2 == 0) ? 2.4 : -2.4);
            result.Add(
                new GraphViewNode(
                    id,
                    labelFactory(id),
                    x,
                    y,
                    z,
                    size,
                    color,
                    level,
                    EmptyMetrics)
                {
                    NodeKind = kind
                });
        }

        return result;
    }

    private static IReadOnlyList<GraphViewNode> BuildMemberNodes(GraphViewNode typeNode, TypeNodeMeta meta)
    {
        var memberSeeds = meta.MethodNames
            .Select(name => new MemberSeed(name, MethodNodeKind))
            .Concat(meta.PropertyNames.Select(name => new MemberSeed(name, PropertyNodeKind)))
            .Concat(meta.FieldNames.Select(name => new MemberSeed(name, FieldNodeKind)))
            .Concat(meta.EventNames.Select(name => new MemberSeed(name, EventNodeKind)))
            .ToArray();

        if (memberSeeds.Length == 0)
        {
            return Array.Empty<GraphViewNode>();
        }

        var orbitRadius = Math.Max(12, typeNode.Size * 1.02);
        var baseSize = Math.Max(3.2, typeNode.Size * 0.32);
        var nodes = new List<GraphViewNode>(memberSeeds.Length);

        for (var index = 0; index < memberSeeds.Length; index++)
        {
            var seed = memberSeeds[index];
            var angle = (2 * Math.PI * index) / memberSeeds.Length;
            var x = typeNode.X + Math.Cos(angle) * orbitRadius;
            var y = typeNode.Y + Math.Sin(angle) * orbitRadius;
            var z = typeNode.Z + ((index % 2 == 0) ? 3.5 : -3.5);
            var size = seed.Kind == MethodNodeKind ? baseSize : baseSize * 0.9;
            var label = seed.Kind switch
            {
                MethodNodeKind => $"{seed.Name}()",
                EventNodeKind => $"{seed.Name} event",
                _ => seed.Name
            };
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
                    seed.Kind,
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

    private static string BuildProjectNodeId(string projectName)
    {
        return $"project::{projectName}";
    }

    private static string BuildNamespaceNodeId(string namespaceName)
    {
        return $"namespace::{namespaceName}";
    }

    private static string BuildFileNodeId(string filePath)
    {
        return $"file::{filePath}";
    }

    private static string BuildExternalNodeId(string externalRoot)
    {
        return $"external::{externalRoot}";
    }

    private static Dictionary<string, TypeNodeMeta> BuildTypeMetaIndex(
        IReadOnlyList<DependencyNode> nodes)
    {
        var result = new Dictionary<string, TypeNodeMeta>(StringComparer.Ordinal);
        var internalSimpleNames = nodes
            .Select(node => RemoveGenericSuffix(GetSimpleName(node.Id)))
            .ToHashSet(StringComparer.Ordinal);
        var internalNamespaceRoots = nodes
            .Select(node => GetNamespaceFromTypeId(node.Id))
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Select(ns => ns!.Split('.')[0])
            .ToHashSet(StringComparer.Ordinal);
        var projectCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var nodesByFile = nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Location?.FilePath))
            .GroupBy(node => node.Location!.FilePath!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in nodesByFile)
        {
            var filePath = group.Key;
            if (!File.Exists(filePath))
            {
                continue;
            }

            string sourceText;
            try
            {
                sourceText = File.ReadAllText(filePath);
            }
            catch
            {
                continue;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
            var root = syntaxTree.GetRoot();
            var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();
            var projectName = FindProjectName(filePath, projectCache);

            foreach (var node in group)
            {
                var type = FindBestMatchingType(types, node);
                if (type is null)
                {
                    result[node.Id] = TypeNodeMeta.Empty with
                    {
                        FilePath = filePath,
                        NamespaceName = GetNamespaceFromTypeId(node.Id),
                        ProjectName = projectName
                    };
                    continue;
                }

                result[node.Id] = new TypeNodeMeta(
                    CollectMethodNames(type),
                    CollectPropertyNames(type),
                    CollectFieldNames(type),
                    CollectEventNames(type),
                    CollectExternalRoots(type, internalSimpleNames, internalNamespaceRoots),
                    filePath,
                    GetNamespaceName(type) ?? GetNamespaceFromTypeId(node.Id),
                    projectName);
            }
        }

        foreach (var node in nodes)
        {
            if (result.ContainsKey(node.Id))
            {
                continue;
            }

            result[node.Id] = TypeNodeMeta.Empty with
            {
                NamespaceName = GetNamespaceFromTypeId(node.Id)
            };
        }

        return result;
    }

    private static string? FindProjectName(string filePath, IDictionary<string, string?> cache)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var visited = new List<string>();
        var cursor = directory;
        while (!string.IsNullOrWhiteSpace(cursor))
        {
            if (cache.TryGetValue(cursor, out var cached))
            {
                foreach (var item in visited)
                {
                    cache[item] = cached;
                }

                return cached;
            }

            visited.Add(cursor);

            try
            {
                var csproj = Directory
                    .EnumerateFiles(cursor, "*.csproj", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(csproj))
                {
                    var projectName = Path.GetFileNameWithoutExtension(csproj);
                    foreach (var item in visited)
                    {
                        cache[item] = projectName;
                    }

                    return projectName;
                }
            }
            catch
            {
                foreach (var item in visited)
                {
                    cache[item] = null;
                }

                return null;
            }

            cursor = Path.GetDirectoryName(cursor);
        }

        foreach (var item in visited)
        {
            cache[item] = null;
        }

        return null;
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

    private static string? GetNamespaceName(TypeDeclarationSyntax type)
    {
        var ns = type.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        return ns?.Name.ToString();
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

    private static string[] CollectFieldNames(TypeDeclarationSyntax type)
    {
        return type.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(field => field.Declaration.Variables)
            .Select(variable => variable.Identifier.ValueText)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] CollectEventNames(TypeDeclarationSyntax type)
    {
        var fieldEvents = type.Members
            .OfType<EventFieldDeclarationSyntax>()
            .SelectMany(item => item.Declaration.Variables)
            .Select(variable => variable.Identifier.ValueText);
        var explicitEvents = type.Members
            .OfType<EventDeclarationSyntax>()
            .Select(item => item.Identifier.ValueText);

        return fieldEvents
            .Concat(explicitEvents)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] CollectExternalRoots(
        TypeDeclarationSyntax type,
        IReadOnlySet<string> internalSimpleNames,
        IReadOnlySet<string> internalNamespaceRoots)
    {
        var roots = new HashSet<string>(StringComparer.Ordinal);
        foreach (var typeSyntax in EnumerateMemberTypeSyntaxes(type))
        {
            var root = ExtractExternalRoot(typeSyntax, internalSimpleNames, internalNamespaceRoots);
            if (!string.IsNullOrWhiteSpace(root))
            {
                roots.Add(root);
            }
        }

        return roots
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<TypeSyntax> EnumerateMemberTypeSyntaxes(TypeDeclarationSyntax type)
    {
        foreach (var field in type.Members.OfType<FieldDeclarationSyntax>())
        {
            yield return field.Declaration.Type;
        }

        foreach (var property in type.Members.OfType<PropertyDeclarationSyntax>())
        {
            yield return property.Type;
        }

        foreach (var eventField in type.Members.OfType<EventFieldDeclarationSyntax>())
        {
            yield return eventField.Declaration.Type;
        }

        foreach (var eventDecl in type.Members.OfType<EventDeclarationSyntax>())
        {
            yield return eventDecl.Type;
        }

        foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
        {
            yield return method.ReturnType;
            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type is not null)
                {
                    yield return parameter.Type;
                }
            }
        }
    }

    private static string? ExtractExternalRoot(
        TypeSyntax syntax,
        IReadOnlySet<string> internalSimpleNames,
        IReadOnlySet<string> internalNamespaceRoots)
    {
        var text = syntax.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (PrimitiveTypeNames.Contains(text))
        {
            return null;
        }

        var normalized = text.Replace("global::", string.Empty, StringComparison.Ordinal);
        var token = ReadFirstIdentifierToken(normalized);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var shortName = RemoveGenericSuffix(token);
        if (PrimitiveTypeNames.Contains(shortName))
        {
            return null;
        }

        if (internalSimpleNames.Contains(shortName) || internalNamespaceRoots.Contains(shortName))
        {
            return null;
        }

        if (string.Equals(shortName, "System", StringComparison.Ordinal))
        {
            return "System";
        }

        if (normalized.Contains('.', StringComparison.Ordinal))
        {
            return shortName;
        }

        return null;
    }

    private static string ReadFirstIdentifierToken(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (!(char.IsLetter(ch) || ch == '_'))
            {
                continue;
            }

            var start = index;
            var cursor = index + 1;
            while (cursor < text.Length)
            {
                var inner = text[cursor];
                if (!(char.IsLetterOrDigit(inner) || inner == '_'))
                {
                    break;
                }

                cursor++;
            }

            return text[start..cursor];
        }

        return string.Empty;
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

    private static string GetNamespaceFromTypeId(string typeId)
    {
        var marker = typeId.LastIndexOf('.');
        return marker > 0 ? typeId[..marker] : "(global)";
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
            FieldNodeKind => "#0ea5e9",
            EventNodeKind => "#06b6d4",
            _ => "#64748b"
        };
    }

    private static int NodeKindOrder(string nodeKind)
    {
        return nodeKind switch
        {
            ProjectNodeKind => 0,
            NamespaceNodeKind => 1,
            FileNodeKind => 2,
            TypeNodeKind => 3,
            MethodNodeKind => 4,
            PropertyNodeKind => 5,
            FieldNodeKind => 6,
            EventNodeKind => 7,
            ExternalNodeKind => 8,
            _ => 9
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

    private static string ToContainsEdgeColor()
    {
        return "#475569";
    }

    private static string ToExternalEdgeColor()
    {
        return "#0ea5e9";
    }

    private sealed record MemberSeed(string Name, string Kind);

    private sealed record TypeNodeMeta(
        IReadOnlyList<string> MethodNames,
        IReadOnlyList<string> PropertyNames,
        IReadOnlyList<string> FieldNames,
        IReadOnlyList<string> EventNames,
        IReadOnlyList<string> ExternalRoots,
        string? FilePath,
        string? NamespaceName,
        string? ProjectName)
    {
        public static readonly TypeNodeMeta Empty = new(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            null);
    }
}
