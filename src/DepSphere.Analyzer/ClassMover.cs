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
        var targetFilePath = Path.Combine(Path.GetDirectoryName(context.SourceFilePath)!, context.TypeName + ".cs");
        EnsureTargetFileDoesNotExist(targetFilePath);

        var targetTypeFqn = BuildTypeFqn(targetNamespace, context.TypeName);
        await EnsureTypeDoesNotExistInProjectAsync(csprojPath, targetTypeFqn, typeFqn, cancellationToken);

        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var movedSource = BuildMovedSource(
            context.SourceNamespace,
            targetNamespace,
            context.TypeParts.SelectMany(part => part.Declarations).ToArray());

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

        if (context.TypeParts.Any(part =>
            string.Equals(
                Path.GetFullPath(part.FilePath),
                Path.GetFullPath(resolvedTargetFilePath),
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Target file path must differ from source file path.");
        }

        EnsureTargetFileDoesNotExist(resolvedTargetFilePath);

        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var movedSource = BuildMovedSource(
            context.SourceNamespace,
            context.SourceNamespace,
            context.TypeParts.SelectMany(part => part.Declarations).ToArray());
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
        EnsureTargetFileDoesNotExist(resolvedTargetFilePath);

        var targetTypeFqn = BuildTypeFqn(context.SourceNamespace, context.TypeName);
        await EnsureTypeDoesNotExistInProjectAsync(targetProjectPath, targetTypeFqn, sourceTypeFqn: null, cancellationToken);

        await RemoveTypeFromSourceAsync(context, cancellationToken);

        var movedSource = BuildMovedSource(
            context.SourceNamespace,
            context.SourceNamespace,
            context.TypeParts.SelectMany(part => part.Declarations).ToArray());
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

        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException("Project file does not exist.", csprojPath);
        }

        if (string.IsNullOrWhiteSpace(typeFqn))
        {
            throw new ArgumentException("Type FQN is required.", nameof(typeFqn));
        }

        var projectDirectory = Path.GetDirectoryName(csprojPath)
            ?? throw new InvalidOperationException($"Project directory is invalid: {csprojPath}");

        var sourceFiles = Directory
            .EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifactPath(path))
            .ToArray();
        var parts = new List<TypeDeclarationPart>();

        foreach (var sourceFilePath in sourceFiles)
        {
            var sourceCode = await File.ReadAllTextAsync(sourceFilePath, cancellationToken);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: sourceFilePath, cancellationToken: cancellationToken);
            var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync(cancellationToken);

            var declarations = root
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Where(type => string.Equals(GetTypeFqn(type), typeFqn, StringComparison.Ordinal))
                .ToArray();

            if (declarations.Length > 0)
            {
                parts.Add(new TypeDeclarationPart(sourceFilePath, root, declarations));
            }
        }

        if (parts.Count == 0)
        {
            throw new InvalidOperationException($"Type declaration not found: {typeFqn}");
        }

        return new TypeDeclarationContext(
            parts[0].FilePath,
            GetNamespace(typeFqn),
            GetTypeName(typeFqn),
            parts);
    }

    private static async Task RemoveTypeFromSourceAsync(TypeDeclarationContext context, CancellationToken cancellationToken)
    {
        foreach (var part in context.TypeParts)
        {
            var updatedRoot = part.Root.RemoveNodes(part.Declarations, SyntaxRemoveOptions.KeepNoTrivia);
            if (updatedRoot is not CompilationUnitSyntax compilationUnit)
            {
                throw new InvalidOperationException("Failed to remove target type from source file.");
            }

            await File.WriteAllTextAsync(
                part.FilePath,
                compilationUnit.NormalizeWhitespace().ToFullString(),
                cancellationToken);
        }
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

    private static string BuildMovedSource(
        string sourceNamespace,
        string targetNamespace,
        IReadOnlyList<TypeDeclarationSyntax> movedTypes)
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

        foreach (var movedType in movedTypes)
        {
            lines.Add(movedType.NormalizeWhitespace().ToFullString());
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetTypeFqn(TypeDeclarationSyntax type)
    {
        var namespaceNode = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceNode is null
            ? type.Identifier.ValueText
            : namespaceNode.Name + "." + type.Identifier.ValueText;
    }

    private static bool IsBuildArtifactPath(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var marker = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        if (normalized.Contains(marker, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        marker = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
        return normalized.Contains(marker, StringComparison.OrdinalIgnoreCase);
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

    private static string BuildTypeFqn(string namespaceName, string typeName)
    {
        return string.IsNullOrWhiteSpace(namespaceName)
            ? typeName
            : namespaceName + "." + typeName;
    }

    private static void EnsureTargetFileDoesNotExist(string targetFilePath)
    {
        if (File.Exists(targetFilePath))
        {
            throw new InvalidOperationException($"Target file already exists: {targetFilePath}");
        }
    }

    private static async Task EnsureTypeDoesNotExistInProjectAsync(
        string projectPath,
        string targetTypeFqn,
        string? sourceTypeFqn,
        CancellationToken cancellationToken)
    {
        var graph = await DependencyAnalyzer.AnalyzePathAsync(projectPath, cancellationToken);
        var exists = graph.Nodes.Any(node =>
            string.Equals(node.Id, targetTypeFqn, StringComparison.Ordinal) &&
            !string.Equals(node.Id, sourceTypeFqn, StringComparison.Ordinal));

        if (exists)
        {
            throw new InvalidOperationException($"Type already exists in target scope: {targetTypeFqn}");
        }
    }

    private sealed record TypeDeclarationContext(
        string SourceFilePath,
        string SourceNamespace,
        string TypeName,
        IReadOnlyList<TypeDeclarationPart> TypeParts);

    private sealed record TypeDeclarationPart(
        string FilePath,
        CompilationUnitSyntax Root,
        IReadOnlyList<TypeDeclarationSyntax> Declarations);
}
