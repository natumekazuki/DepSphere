using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class DependencyAnalyzer
{
    private static readonly SymbolDisplayFormat DisplayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;

    public static DependencyGraph Analyze(IEnumerable<string> sourceCodes)
    {
        var syntaxTrees = sourceCodes
            .Select(code => CSharpSyntaxTree.ParseText(code))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "DepSphere.DynamicAnalysis",
            syntaxTrees: syntaxTrees,
            references: BuildMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var declaredTypes = CollectDeclaredTypes(compilation);
        var declaredTypeIds = declaredTypes.Keys.ToHashSet(StringComparer.Ordinal);
        var rawMetrics = new Dictionary<string, RawMetrics>(StringComparer.Ordinal);
        var edges = new HashSet<DependencyEdge>();

        foreach (var pair in declaredTypes)
        {
            var sourceId = pair.Key;
            var sourceType = pair.Value;
            var metrics = BuildRawMetrics(compilation, sourceType, declaredTypeIds, edges);
            rawMetrics[sourceId] = metrics;
        }

        var inDegreeMap = edges
            .GroupBy(edge => edge.To, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (var pair in rawMetrics)
        {
            pair.Value.InDegree = inDegreeMap.TryGetValue(pair.Key, out var degree) ? degree : 0;
        }

        var nodes = BuildNodes(rawMetrics);

        var orderedEdges = edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .ToArray();

        return new DependencyGraph(nodes, orderedEdges);
    }

    private static IReadOnlyList<DependencyNode> BuildNodes(IReadOnlyDictionary<string, RawMetrics> rawMetrics)
    {
        var methodN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.MethodCount, StringComparer.Ordinal));
        var statementN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.StatementCount, StringComparer.Ordinal));
        var branchN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.BranchCount, StringComparer.Ordinal));
        var callSiteN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.CallSiteCount, StringComparer.Ordinal));
        var fanOutN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.FanOut, StringComparer.Ordinal));
        var inDegreeN = Normalize(rawMetrics.ToDictionary(pair => pair.Key, pair => pair.Value.InDegree, StringComparer.Ordinal));

        var nodes = new List<DependencyNode>(rawMetrics.Count);
        foreach (var pair in rawMetrics.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var id = pair.Key;
            var raw = pair.Value;

            var score =
                0.15 * methodN[id] +
                0.30 * statementN[id] +
                0.20 * branchN[id] +
                0.20 * callSiteN[id] +
                0.10 * fanOutN[id] +
                0.05 * inDegreeN[id];

            nodes.Add(
                new DependencyNode(
                    id,
                    new TypeMetrics(
                        raw.MethodCount,
                        raw.StatementCount,
                        raw.BranchCount,
                        raw.CallSiteCount,
                        raw.FanOut,
                        raw.InDegree,
                        score)));
        }

        return nodes;
    }

    private static Dictionary<string, double> Normalize(IReadOnlyDictionary<string, int> values)
    {
        var transformed = values.ToDictionary(
            pair => pair.Key,
            pair => Math.Log(1 + pair.Value),
            StringComparer.Ordinal);

        var sorted = transformed.Values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0)
        {
            return transformed.ToDictionary(pair => pair.Key, _ => 0.0, StringComparer.Ordinal);
        }

        var index = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
        index = Math.Clamp(index, 0, sorted.Length - 1);
        var p95 = sorted[index];
        if (p95 <= 0)
        {
            p95 = 1;
        }

        return transformed.ToDictionary(
            pair => pair.Key,
            pair => Math.Min(pair.Value, p95) / p95,
            StringComparer.Ordinal);
    }

    private static RawMetrics BuildRawMetrics(
        Compilation compilation,
        INamedTypeSymbol source,
        IReadOnlySet<string> declaredTypeIds,
        ISet<DependencyEdge> edges)
    {
        var sourceId = ToTypeId(source);
        var metrics = new RawMetrics();

        AddInheritEdges(source, declaredTypeIds, edges);
        AddImplementEdges(source, declaredTypeIds, edges);

        foreach (var syntaxReference in source.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax() as TypeDeclarationSyntax;
            if (syntax is null)
            {
                continue;
            }

            var model = compilation.GetSemanticModel(syntax.SyntaxTree);

            metrics.MethodCount += syntax.Members.OfType<MethodDeclarationSyntax>().Count();
            metrics.StatementCount += syntax.DescendantNodes().OfType<StatementSyntax>().Count();
            metrics.BranchCount += syntax.DescendantNodes().Count(IsBranchNode);
            metrics.CallSiteCount += syntax.DescendantNodes().Count(node =>
                node is InvocationExpressionSyntax || node is ObjectCreationExpressionSyntax);

            foreach (var field in syntax.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    AddReferenceEdge(sourceId, fieldSymbol?.Type, declaredTypeIds, metrics.ReferenceTargets, edges);
                }
            }

            foreach (var property in syntax.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                var propertySymbol = model.GetDeclaredSymbol(property) as IPropertySymbol;
                AddReferenceEdge(sourceId, propertySymbol?.Type, declaredTypeIds, metrics.ReferenceTargets, edges);
            }

            foreach (var method in syntax.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                if (methodSymbol is null)
                {
                    continue;
                }

                AddReferenceEdge(sourceId, methodSymbol.ReturnType, declaredTypeIds, metrics.ReferenceTargets, edges);
                foreach (var parameter in methodSymbol.Parameters)
                {
                    AddReferenceEdge(sourceId, parameter.Type, declaredTypeIds, metrics.ReferenceTargets, edges);
                }
            }

            foreach (var objectCreation in syntax.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var type = model.GetTypeInfo(objectCreation).Type;
                AddReferenceEdge(sourceId, type, declaredTypeIds, metrics.ReferenceTargets, edges);
            }

            foreach (var memberAccess in syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbol = model.GetSymbolInfo(memberAccess.Expression).Symbol;
                if (symbol is INamedTypeSymbol namedType)
                {
                    AddReferenceEdge(sourceId, namedType, declaredTypeIds, metrics.ReferenceTargets, edges);
                }
            }
        }

        metrics.FanOut = metrics.ReferenceTargets.Count;
        return metrics;
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

    private static Dictionary<string, INamedTypeSymbol> CollectDeclaredTypes(Compilation compilation)
    {
        var declaredTypes = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

            foreach (var declaration in typeDeclarations)
            {
                var symbol = model.GetDeclaredSymbol(declaration);
                if (symbol is not INamedTypeSymbol namedType)
                {
                    continue;
                }

                declaredTypes[ToTypeId(namedType)] = namedType;
            }
        }

        return declaredTypes;
    }

    private static void AddInheritEdges(
        INamedTypeSymbol source,
        IReadOnlySet<string> declaredTypeIds,
        ISet<DependencyEdge> edges)
    {
        var baseType = source.BaseType;
        if (baseType is null || baseType.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        var targetId = ToTypeId(baseType);
        if (!declaredTypeIds.Contains(targetId))
        {
            return;
        }

        edges.Add(new DependencyEdge(ToTypeId(source), targetId, DependencyKind.Inherit));
    }

    private static void AddImplementEdges(
        INamedTypeSymbol source,
        IReadOnlySet<string> declaredTypeIds,
        ISet<DependencyEdge> edges)
    {
        foreach (var iface in source.Interfaces)
        {
            var targetId = ToTypeId(iface);
            if (!declaredTypeIds.Contains(targetId))
            {
                continue;
            }

            edges.Add(new DependencyEdge(ToTypeId(source), targetId, DependencyKind.Implement));
        }
    }

    private static void AddReferenceEdge(
        string sourceId,
        ITypeSymbol? targetType,
        IReadOnlySet<string> declaredTypeIds,
        ISet<string> referenceTargets,
        ISet<DependencyEdge> edges)
    {
        if (targetType is not INamedTypeSymbol namedType)
        {
            return;
        }

        var targetId = ToTypeId(namedType);
        if (sourceId == targetId || !declaredTypeIds.Contains(targetId))
        {
            return;
        }

        referenceTargets.Add(targetId);
        edges.Add(new DependencyEdge(sourceId, targetId, DependencyKind.Reference));
    }

    private static string ToTypeId(INamedTypeSymbol symbol)
    {
        return symbol.ToDisplayString(DisplayFormat);
    }

    private static IEnumerable<MetadataReference> BuildMetadataReferences()
    {
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
            {
                continue;
            }

            locations.Add(assembly.Location);
        }

        return locations.Select(location => MetadataReference.CreateFromFile(location)).ToArray();
    }

    private sealed class RawMetrics
    {
        public int MethodCount { get; set; }

        public int StatementCount { get; set; }

        public int BranchCount { get; set; }

        public int CallSiteCount { get; set; }

        public int FanOut { get; set; }

        public int InDegree { get; set; }

        public HashSet<string> ReferenceTargets { get; } = new(StringComparer.Ordinal);
    }
}
