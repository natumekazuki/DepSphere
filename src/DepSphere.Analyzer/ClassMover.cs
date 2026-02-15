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
        if (string.IsNullOrWhiteSpace(csprojPath))
        {
            throw new ArgumentException("Project path is required.", nameof(csprojPath));
        }

        if (string.IsNullOrWhiteSpace(typeFqn))
        {
            throw new ArgumentException("Type FQN is required.", nameof(typeFqn));
        }

        if (string.IsNullOrWhiteSpace(targetNamespace))
        {
            throw new ArgumentException("Target namespace is required.", nameof(targetNamespace));
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

        var updatedRoot = root.RemoveNode(targetType, SyntaxRemoveOptions.KeepNoTrivia)
            ?? throw new InvalidOperationException("Failed to remove target type from source file.");

        await File.WriteAllTextAsync(sourceFilePath, updatedRoot.NormalizeWhitespace().ToFullString(), cancellationToken);

        var sourceNamespace = GetNamespace(typeFqn);
        var typeName = GetTypeName(typeFqn);
        var targetFilePath = Path.Combine(Path.GetDirectoryName(sourceFilePath)!, typeName + ".cs");

        var movedSource = BuildMovedSource(sourceNamespace, targetNamespace, targetType);
        await File.WriteAllTextAsync(targetFilePath, movedSource, cancellationToken);

        return new MoveNamespaceResult(sourceFilePath, targetFilePath);
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
}
