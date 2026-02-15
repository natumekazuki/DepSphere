using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class IncrementalFileAnalyzer
{
    public static IncrementalFileAnalysis Analyze(
        string filePath,
        IReadOnlyCollection<DependencyNode> existingNodes)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new IncrementalFileAnalysis(Array.Empty<DependencyNode>(), Array.Empty<DependencyEdge>());
        }

        var text = File.ReadAllText(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(text, path: filePath);
        var root = syntaxTree.GetRoot();

        var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();
        if (typeDeclarations.Length == 0)
        {
            return new IncrementalFileAnalysis(Array.Empty<DependencyNode>(), Array.Empty<DependencyEdge>());
        }

        var nodes = new List<DependencyNode>(typeDeclarations.Length);
        foreach (var type in typeDeclarations)
        {
            var nodeId = BuildTypeId(type);
            var lineSpan = type.SyntaxTree.GetLineSpan(type.Span);

            var methodCount = type.Members.OfType<MethodDeclarationSyntax>().Count();
            var statementCount = type.DescendantNodes().OfType<StatementSyntax>().Count();
            var branchCount = type.DescendantNodes().Count(IsBranchNode);
            var callSiteCount = type.DescendantNodes().Count(node =>
                node is InvocationExpressionSyntax || node is ObjectCreationExpressionSyntax);

            nodes.Add(
                new DependencyNode(
                    nodeId,
                    new TypeMetrics(methodCount, statementCount, branchCount, callSiteCount, 0, 0, 0))
                {
                    Location = new SourceLocation(
                        filePath,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        lineSpan.EndLinePosition.Line + 1,
                        lineSpan.EndLinePosition.Character + 1)
                });
        }

        var knownIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in existingNodes)
        {
            knownIds.Add(node.Id);
        }

        foreach (var node in nodes)
        {
            knownIds.Add(node.Id);
        }

        var bySimpleName = BuildSimpleNameMap(knownIds);
        var edges = new HashSet<DependencyEdge>();

        foreach (var type in typeDeclarations)
        {
            var sourceId = BuildTypeId(type);
            var sourceNamespace = GetNamespace(type);

            AddInheritanceEdges(type, sourceId, sourceNamespace, knownIds, bySimpleName, edges);
            AddReferenceEdges(type, sourceId, sourceNamespace, knownIds, bySimpleName, edges);
        }

        var validEdges = edges
            .Where(edge => knownIds.Contains(edge.From) && knownIds.Contains(edge.To) && edge.From != edge.To)
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .ToArray();

        return new IncrementalFileAnalysis(nodes, validEdges);
    }

    private static void AddInheritanceEdges(
        TypeDeclarationSyntax type,
        string sourceId,
        string sourceNamespace,
        IReadOnlySet<string> knownIds,
        IReadOnlyDictionary<string, IReadOnlyList<string>> bySimpleName,
        ISet<DependencyEdge> edges)
    {
        if (type.BaseList is null)
        {
            return;
        }

        var baseTypes = type.BaseList.Types;
        for (var i = 0; i < baseTypes.Count; i++)
        {
            var rawName = baseTypes[i].Type.ToString();
            var targetId = ResolveTypeId(sourceNamespace, rawName, knownIds, bySimpleName);
            if (string.IsNullOrWhiteSpace(targetId))
            {
                continue;
            }

            var kind = ResolveBaseEdgeKind(type, i);
            edges.Add(new DependencyEdge(sourceId, targetId, kind));
        }
    }

    private static void AddReferenceEdges(
        TypeDeclarationSyntax type,
        string sourceId,
        string sourceNamespace,
        IReadOnlySet<string> knownIds,
        IReadOnlyDictionary<string, IReadOnlyList<string>> bySimpleName,
        ISet<DependencyEdge> edges)
    {
        foreach (var field in type.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var targetId = ResolveTypeId(sourceNamespace, field.Declaration.Type.ToString(), knownIds, bySimpleName);
            AddReference(sourceId, targetId, edges);
        }

        foreach (var property in type.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var targetId = ResolveTypeId(sourceNamespace, property.Type.ToString(), knownIds, bySimpleName);
            AddReference(sourceId, targetId, edges);
        }

        foreach (var method in type.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var returnTarget = ResolveTypeId(sourceNamespace, method.ReturnType.ToString(), knownIds, bySimpleName);
            AddReference(sourceId, returnTarget, edges);

            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type is null)
                {
                    continue;
                }

                var parameterTarget = ResolveTypeId(sourceNamespace, parameter.Type.ToString(), knownIds, bySimpleName);
                AddReference(sourceId, parameterTarget, edges);
            }
        }

        foreach (var objectCreation in type.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var targetId = ResolveTypeId(sourceNamespace, objectCreation.Type.ToString(), knownIds, bySimpleName);
            AddReference(sourceId, targetId, edges);
        }

        foreach (var memberAccess in type.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var targetId = ResolveTypeId(sourceNamespace, memberAccess.Expression.ToString(), knownIds, bySimpleName);
            AddReference(sourceId, targetId, edges);
        }
    }

    private static void AddReference(string sourceId, string? targetId, ISet<DependencyEdge> edges)
    {
        if (string.IsNullOrWhiteSpace(targetId) || string.Equals(sourceId, targetId, StringComparison.Ordinal))
        {
            return;
        }

        edges.Add(new DependencyEdge(sourceId, targetId, DependencyKind.Reference));
    }

    private static DependencyKind ResolveBaseEdgeKind(TypeDeclarationSyntax type, int index)
    {
        if (type is InterfaceDeclarationSyntax)
        {
            return DependencyKind.Inherit;
        }

        if (type is ClassDeclarationSyntax or RecordDeclarationSyntax)
        {
            return index == 0 ? DependencyKind.Inherit : DependencyKind.Implement;
        }

        return DependencyKind.Implement;
    }

    private static string? ResolveTypeId(
        string sourceNamespace,
        string rawName,
        IReadOnlySet<string> knownIds,
        IReadOnlyDictionary<string, IReadOnlyList<string>> bySimpleName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var cleaned = rawName.Trim();
        cleaned = cleaned.Replace("global::", string.Empty, StringComparison.Ordinal);

        var genericTick = cleaned.IndexOf('<');
        if (genericTick >= 0)
        {
            cleaned = cleaned[..genericTick];
        }

        var nullableMark = cleaned.IndexOf('?');
        if (nullableMark >= 0)
        {
            cleaned = cleaned[..nullableMark];
        }

        if (cleaned.EndsWith("[]", StringComparison.Ordinal))
        {
            cleaned = cleaned[..^2];
        }

        if (knownIds.Contains(cleaned))
        {
            return cleaned;
        }

        var simpleName = cleaned.Contains('.')
            ? cleaned[(cleaned.LastIndexOf('.') + 1)..]
            : cleaned;

        if (!string.IsNullOrWhiteSpace(sourceNamespace))
        {
            var scoped = sourceNamespace + "." + simpleName;
            if (knownIds.Contains(scoped))
            {
                return scoped;
            }
        }

        if (bySimpleName.TryGetValue(simpleName, out var candidates) && candidates.Count == 1)
        {
            return candidates[0];
        }

        if (cleaned.Contains('.'))
        {
            var suffix = "." + simpleName;
            var matched = knownIds.FirstOrDefault(id => id.EndsWith(suffix, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(matched))
            {
                return matched;
            }
        }

        return null;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildSimpleNameMap(IEnumerable<string> ids)
    {
        return ids
            .GroupBy(GetSimpleName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
    }

    private static string GetSimpleName(string id)
    {
        var lastDot = id.LastIndexOf('.');
        return lastDot >= 0 ? id[(lastDot + 1)..] : id;
    }

    private static string BuildTypeId(TypeDeclarationSyntax type)
    {
        var ns = GetNamespace(type);
        var name = type.Identifier.ValueText;
        return string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
    }

    private static string GetNamespace(TypeDeclarationSyntax type)
    {
        var namespaceNode = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceNode?.Name.ToString() ?? string.Empty;
    }

    private static bool IsBranchNode(SyntaxNode node)
    {
        return node is IfStatementSyntax
            or SwitchStatementSyntax
            or SwitchExpressionSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax
            or ConditionalExpressionSyntax;
    }
}
