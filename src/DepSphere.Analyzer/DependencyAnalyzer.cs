using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace DepSphere.Analyzer;

public static class DependencyAnalyzer
{
    private static readonly SymbolDisplayFormat DisplayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;
    private static readonly object MsBuildLock = new();
    private static readonly SemaphoreSlim WorkspaceGate = new(1, 1);
    private static bool s_msBuildRegistered;

    public static DependencyGraph Analyze(IEnumerable<string> sourceCodes)
    {
        return Analyze(sourceCodes, options: null);
    }

    public static DependencyGraph Analyze(IEnumerable<string> sourceCodes, AnalysisOptions? options)
    {
        var syntaxTrees = sourceCodes
            .Select(code => CSharpSyntaxTree.ParseText(code))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "DepSphere.DynamicAnalysis",
            syntaxTrees: syntaxTrees,
            references: BuildMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var effectiveOptions = options ?? new AnalysisOptions();
        var normalizedWeights = effectiveOptions.GetNormalizedMetricWeights();
        _ = effectiveOptions.ValidateLevelThresholds();
        return AnalyzeCompilations(new[] { compilation }, normalizedWeights: normalizedWeights);
    }

    public static Task<DependencyGraph> AnalyzePathAsync(string path, CancellationToken cancellationToken = default)
    {
        return AnalyzePathAsync(path, options: null, progress: null, cancellationToken);
    }

    public static Task<DependencyGraph> AnalyzePathAsync(
        string path,
        IProgress<AnalysisProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        return AnalyzePathAsync(path, options: null, progress, cancellationToken);
    }

    public static async Task<DependencyGraph> AnalyzePathAsync(
        string path,
        AnalysisOptions? options,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Target file does not exist.", path);
        }

        var effectiveOptions = options ?? new AnalysisOptions();
        var metricsProgressReportInterval = effectiveOptions.ValidateMetricsProgressReportInterval();
        var normalizedWeights = effectiveOptions.GetNormalizedMetricWeights();
        _ = effectiveOptions.ValidateLevelThresholds();

        ReportProgress(progress, "prepare", "入力検証完了");
        EnsureMsBuildRegistered();
        ReportProgress(progress, "prepare", "MSBuild 初期化完了");
        ReportProgress(progress, "prepare", "解析ワークスペース取得待機中");
        await WorkspaceGate.WaitAsync(cancellationToken);
        try
        {
            ReportProgress(progress, "prepare", "解析ワークスペースを確保");
            using var workspace = MSBuildWorkspace.Create();

            var extension = Path.GetExtension(path);
            if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                ReportProgress(progress, "load", "ソリューションを読み込み中");
                var solution = await workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken);
                var projects = solution.Projects.ToArray();
                ReportProgress(progress, "load", "ソリューション読込完了", projects.Length, projects.Length);
                var graph = await AnalyzeProjectsAsync(projects, metricsProgressReportInterval, normalizedWeights, progress, cancellationToken);
                ReportProgress(progress, "complete", "解析完了");
                return graph;
            }

            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                ReportProgress(progress, "load", "プロジェクトを読み込み中");
                var project = await workspace.OpenProjectAsync(path, cancellationToken: cancellationToken);
                ReportProgress(progress, "load", "プロジェクト読込完了", 1, 1);
                var graph = await AnalyzeProjectsAsync(new[] { project }, metricsProgressReportInterval, normalizedWeights, progress, cancellationToken);
                ReportProgress(progress, "complete", "解析完了");
                return graph;
            }

            throw new NotSupportedException("Only .sln and .csproj are supported.");
        }
        finally
        {
            WorkspaceGate.Release();
        }
    }

    private static async Task<DependencyGraph> AnalyzeProjectsAsync(
        IEnumerable<Project> projects,
        int metricsProgressReportInterval,
        (double Method, double Statement, double Branch, double CallSite, double FanOut, double InDegree) normalizedWeights,
        IProgress<AnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var projectList = projects.ToList();
        var compilations = new List<Compilation>();
        var total = projectList.Count;
        var current = 0;
        foreach (var project in projectList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;
            ReportProgress(progress, "compile", $"コンパイル作成中: {project.Name}", current, total);
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is not null)
            {
                compilations.Add(compilation);
            }
        }

        ReportProgress(progress, "compile", "コンパイル作成完了", compilations.Count, total);
        return AnalyzeCompilations(compilations, metricsProgressReportInterval, normalizedWeights, progress, cancellationToken);
    }

    private static DependencyGraph AnalyzeCompilations(
        IEnumerable<Compilation> compilations,
        int metricsProgressReportInterval = AnalysisOptions.DefaultMetricsProgressReportInterval,
        (double Method, double Statement, double Branch, double CallSite, double FanOut, double InDegree) normalizedWeights = default,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (normalizedWeights == default)
        {
            normalizedWeights = new AnalysisOptions().GetNormalizedMetricWeights();
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(progress, "metrics", "型定義を収集中");
        var declaredTypes = CollectDeclaredTypes(compilations);
        var declaredTypeIds = declaredTypes.Keys.ToHashSet(StringComparer.Ordinal);
        var rawMetrics = new Dictionary<string, RawMetrics>(StringComparer.Ordinal);
        var edges = new HashSet<DependencyEdge>();

        var totalTypes = declaredTypes.Count;
        var currentType = 0;
        foreach (var pair in declaredTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentType++;
            if (totalTypes > 0 && (currentType == 1 || currentType == totalTypes || currentType % metricsProgressReportInterval == 0))
            {
                ReportProgress(progress, "metrics", "メトリクス算出中", currentType, totalTypes);
            }

            var metrics = BuildRawMetrics(pair.Value.Compilation, pair.Value.Symbol, declaredTypeIds, edges);
            rawMetrics[pair.Key] = metrics;
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(progress, "metrics", "グラフ構築中");
        var inDegreeMap = edges
            .GroupBy(edge => edge.To, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (var pair in rawMetrics)
        {
            pair.Value.InDegree = inDegreeMap.TryGetValue(pair.Key, out var degree) ? degree : 0;
        }

        var nodes = BuildNodes(rawMetrics, normalizedWeights);
        var orderedEdges = edges
            .OrderBy(edge => edge.From, StringComparer.Ordinal)
            .ThenBy(edge => edge.To, StringComparer.Ordinal)
            .ThenBy(edge => edge.Kind)
            .ToArray();

        ReportProgress(progress, "metrics", "グラフ構築完了");
        return new DependencyGraph(nodes, orderedEdges);
    }

    private static IReadOnlyList<DependencyNode> BuildNodes(
        IReadOnlyDictionary<string, RawMetrics> rawMetrics,
        (double Method, double Statement, double Branch, double CallSite, double FanOut, double InDegree) normalizedWeights)
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
                normalizedWeights.Method * methodN[id] +
                normalizedWeights.Statement * statementN[id] +
                normalizedWeights.Branch * branchN[id] +
                normalizedWeights.CallSite * callSiteN[id] +
                normalizedWeights.FanOut * fanOutN[id] +
                normalizedWeights.InDegree * inDegreeN[id];

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
                        score))
                {
                    Location = raw.Location
                });
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

            if (metrics.Location is null)
            {
                var span = syntax.SyntaxTree.GetLineSpan(syntax.Span);
                var path = syntax.SyntaxTree.FilePath ?? string.Empty;
                metrics.Location = new SourceLocation(
                    path,
                    span.StartLinePosition.Line + 1,
                    span.StartLinePosition.Character + 1,
                    span.EndLinePosition.Line + 1,
                    span.EndLinePosition.Character + 1);
            }

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
                var symbolInfo = model.GetSymbolInfo(memberAccess);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                AddReferenceEdgesFromMemberSymbol(
                    sourceId,
                    symbol,
                    declaredTypeIds,
                    metrics.ReferenceTargets,
                    edges);
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

    private static Dictionary<string, DeclaredTypeContext> CollectDeclaredTypes(IEnumerable<Compilation> compilations)
    {
        var declaredTypes = new Dictionary<string, DeclaredTypeContext>(StringComparer.Ordinal);

        foreach (var compilation in compilations)
        {
            foreach (var pair in CollectDeclaredTypesFromCompilation(compilation))
            {
                declaredTypes.TryAdd(pair.Key, new DeclaredTypeContext(compilation, pair.Value));
            }
        }

        return declaredTypes;
    }

    private static Dictionary<string, INamedTypeSymbol> CollectDeclaredTypesFromCompilation(Compilation compilation)
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

    private static void AddReferenceEdgesFromMemberSymbol(
        string sourceId,
        ISymbol? symbol,
        IReadOnlySet<string> declaredTypeIds,
        ISet<string> referenceTargets,
        ISet<DependencyEdge> edges)
    {
        switch (symbol)
        {
            case IPropertySymbol property:
                AddReferenceEdge(sourceId, property.ContainingType, declaredTypeIds, referenceTargets, edges);
                AddReferenceEdge(sourceId, property.Type, declaredTypeIds, referenceTargets, edges);
                break;

            case IFieldSymbol field:
                AddReferenceEdge(sourceId, field.ContainingType, declaredTypeIds, referenceTargets, edges);
                AddReferenceEdge(sourceId, field.Type, declaredTypeIds, referenceTargets, edges);
                break;

            case IMethodSymbol method:
                AddReferenceEdge(sourceId, method.ContainingType, declaredTypeIds, referenceTargets, edges);
                AddReferenceEdge(sourceId, method.ReturnType, declaredTypeIds, referenceTargets, edges);
                foreach (var parameter in method.Parameters)
                {
                    AddReferenceEdge(sourceId, parameter.Type, declaredTypeIds, referenceTargets, edges);
                }
                break;

            case IEventSymbol @event:
                AddReferenceEdge(sourceId, @event.ContainingType, declaredTypeIds, referenceTargets, edges);
                AddReferenceEdge(sourceId, @event.Type, declaredTypeIds, referenceTargets, edges);
                break;

            case INamedTypeSymbol namedType:
                AddReferenceEdge(sourceId, namedType, declaredTypeIds, referenceTargets, edges);
                break;
        }
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

    private static void EnsureMsBuildRegistered()
    {
        if (MSBuildLocator.IsRegistered || s_msBuildRegistered)
        {
            return;
        }

        lock (MsBuildLock)
        {
            if (MSBuildLocator.IsRegistered || s_msBuildRegistered)
            {
                return;
            }

            var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            if (instances.Length > 0)
            {
                var instance = instances.OrderByDescending(item => item.Version).First();
                MSBuildLocator.RegisterInstance(instance);
            }
            else
            {
                MSBuildLocator.RegisterDefaults();
            }

            s_msBuildRegistered = true;
        }
    }

    private static void ReportProgress(
        IProgress<AnalysisProgress>? progress,
        string stage,
        string message,
        int? current = null,
        int? total = null)
    {
        progress?.Report(new AnalysisProgress(stage, message, current, total));
    }

    private sealed record DeclaredTypeContext(Compilation Compilation, INamedTypeSymbol Symbol);

    private sealed class RawMetrics
    {
        public int MethodCount { get; set; }

        public int StatementCount { get; set; }

        public int BranchCount { get; set; }

        public int CallSiteCount { get; set; }

        public int FanOut { get; set; }

        public int InDegree { get; set; }

        public HashSet<string> ReferenceTargets { get; } = new(StringComparer.Ordinal);

        public SourceLocation? Location { get; set; }
    }
}
