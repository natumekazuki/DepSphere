using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DepSphere.Analyzer;

public static class ClassMover
{
    public static async Task<MoveNamespaceResult> MoveNamespaceAsync(
        string csprojPath,
        string typeFqn,
        string targetNamespace,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetNamespace))
        {
            throw new ArgumentException("Target namespace is required.", nameof(targetNamespace));
        }

        var context = await LoadTypeContextAsync(csprojPath, typeFqn, cancellationToken);
        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var targetFilePath = Path.Combine(Path.GetDirectoryName(context.SourceFilePath)!, context.TypeName + ".cs");
        var movedSource = BuildMovedSource(context.SourceNamespace, targetNamespace, context.TypeDeclaration);

        await WriteMovedFileAsync(targetFilePath, movedSource, cancellationToken);
        return new MoveNamespaceResult(context.SourceFilePath, targetFilePath);
    }

    public static async Task<MoveFileResult> MoveFileAsync(
        string csprojPath,
        string typeFqn,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
        {
            throw new ArgumentException("Target file path is required.", nameof(targetFilePath));
        }

        var context = await LoadTypeContextAsync(csprojPath, typeFqn, cancellationToken);
        var resolvedTargetFilePath = ResolveTargetFilePath(csprojPath, targetFilePath);

        if (string.Equals(
                Path.GetFullPath(context.SourceFilePath),
                Path.GetFullPath(resolvedTargetFilePath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Target file path must differ from source file path.");
        }

        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var movedSource = BuildMovedSource(context.SourceNamespace, context.SourceNamespace, context.TypeDeclaration);
        await WriteMovedFileAsync(resolvedTargetFilePath, movedSource, cancellationToken);

        return new MoveFileResult(context.SourceFilePath, resolvedTargetFilePath);
    }

    public static async Task<MoveProjectResult> MoveProjectAsync(
        string sourceProjectPath,
        string targetProjectPath,
        string typeFqn,
        string? targetRelativePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetProjectPath))
        {
            throw new ArgumentException("Target project path is required.", nameof(targetProjectPath));
        }

        if (!File.Exists(targetProjectPath))
        {
            throw new FileNotFoundException("Target project file does not exist.", targetProjectPath);
        }

        var context = await LoadTypeContextAsync(sourceProjectPath, typeFqn, cancellationToken);
        var relativePath = string.IsNullOrWhiteSpace(targetRelativePath)
            ? context.TypeName + ".cs"
            : targetRelativePath;
        var resolvedTargetFilePath = ResolveTargetFilePath(targetProjectPath, relativePath);

        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var movedSource = BuildMovedSource(context.SourceNamespace, context.SourceNamespace, context.TypeDeclaration);
        await WriteMovedFileAsync(resolvedTargetFilePath, movedSource, cancellationToken);

        return new MoveProjectResult(
            context.SourceFilePath,
            resolvedTargetFilePath,
            sourceProjectPath,
            targetProjectPath);
    }

    private static async Task<TypeDeclarationContext> LoadTypeContextAsync(
        string csprojPath,
        string typeFqn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csprojPath))
        {
            throw new ArgumentException("Project path is required.", nameof(csprojPath));
        }

        if (string.IsNullOrWhiteSpace(typeFqn))
        {
            throw new ArgumentException("Type FQN is required.", nameof(typeFqn));
        }

        var graph = await DependencyAnalyzer.AnalyzePathAsync(csprojPath, cancellationToken);
        var node = graph.Nodes.FirstOrDefault(item => item.Id == typeFqn)
            ?? throw new InvalidOperationException($"Type not found: {typeFqn}");

        var sourceFilePath = node.Location?.FilePath;
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            throw new InvalidOperationException($"Source file not found: {sourceFilePath}");
        }

        var sourceCode = await File.ReadAllTextAsync(sourceFilePath, cancellationToken);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: sourceFilePath, cancellationToken: cancellationToken);
        var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync(cancellationToken);

        var targetType = root
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(type => GetTypeFqn(type) == typeFqn)
            ?? throw new InvalidOperationException($"Type declaration not found in syntax tree: {typeFqn}");

        return new TypeDeclarationContext(
            sourceFilePath,
            GetNamespace(typeFqn),
            GetTypeName(typeFqn),
            root,
            targetType);
    }

    private static async Task RemoveTypeFromSourceAsync(TypeDeclarationContext context, CancellationToken cancellationToken)
    {
        var updatedRoot = context.Root.RemoveNode(context.TypeDeclaration, SyntaxRemoveOptions.KeepNoTrivia)
            ?? throw new InvalidOperationException("Failed to remove target type from source file.");

        await File.WriteAllTextAsync(
            context.SourceFilePath,
            updatedRoot.NormalizeWhitespace().ToFullString(),
            cancellationToken);
    }

    private static async Task WriteMovedFileAsync(string targetFilePath, string movedSource, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(targetFilePath, movedSource, cancellationToken);
    }

    private static string ResolveTargetFilePath(string csprojPath, string targetFilePath)
    {
        if (Path.IsPathRooted(targetFilePath))
        {
            return targetFilePath;
        }

        var projectDirectory = Path.GetDirectoryName(csprojPath)
            ?? throw new InvalidOperationException($"Project directory is invalid: {csprojPath}");

        return Path.Combine(projectDirectory, targetFilePath);
    }

    private static string BuildMovedSource(string sourceNamespace, string targetNamespace, TypeDeclarationSyntax movedType)
    {
        var lines = new List<string>
        {
            "namespace " + targetNamespace + ";",
            string.Empty
        };

        if (!string.IsNullOrWhiteSpace(sourceNamespace)
            && !string.Equals(sourceNamespace, targetNamespace, StringComparison.Ordinal))
        {
            lines.Insert(0, "using " + sourceNamespace + ";");
            lines.Insert(1, string.Empty);
        }

        lines.Add(movedType.NormalizeWhitespace().ToFullString());
        lines.Add(string.Empty);
        return string.Join(Environment.NewLine, lines);
    }

    private static string GetTypeFqn(TypeDeclarationSyntax type)
    {
        var namespaceNode = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceNode is null
            ? type.Identifier.ValueText
            : namespaceNode.Name + "." + type.Identifier.ValueText;
    }

    private static string GetNamespace(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        return lastDot > 0 ? typeFqn[..lastDot] : string.Empty;
    }

    private static string GetTypeName(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        return lastDot > 0 ? typeFqn[(lastDot + 1)..] : typeFqn;
    }

    private sealed record TypeDeclarationContext(
        string SourceFilePath,
        string SourceNamespace,
        string TypeName,
        CompilationUnitSyntax Root,
        TypeDeclarationSyntax TypeDeclaration);
}
