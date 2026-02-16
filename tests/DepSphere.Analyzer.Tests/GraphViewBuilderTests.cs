using System.Text.Json;
using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class GraphViewBuilderTests
{
    private const string Source = """
        namespace Sample;

        public interface IService { }

        public class Base { }

        public class Dependency
        {
            public static void Touch() { }
        }

        public class Impl : Base, IService
        {
            private readonly Dependency _field = new();

            public Dependency Prop { get; set; } = new();

            public Dependency Method(Dependency arg)
            {
                Dependency.Touch();
                return arg;
            }
        }
        """;

    [Fact]
    public void 三次元表示用グラフへ変換できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });

        var view = GraphViewBuilder.Build(graph);

        Assert.True(view.Nodes.Count >= graph.Nodes.Count);
        Assert.True(view.Edges.Count >= graph.Edges.Count);
        Assert.All(graph.Nodes, node => Assert.Contains(view.Nodes, item => item.Id == node.Id));
        Assert.All(view.Nodes, node =>
        {
            Assert.False(string.IsNullOrWhiteSpace(node.Id));
            Assert.True(node.Size > 0);
        });
    }

    [Fact]
    public void ノードラベルは型の単純名になる()
    {
        var graph = new DependencyGraph(
            new[]
            {
                new DependencyNode(
                    "Sample.Outer.Inner",
                    new TypeMetrics(1, 1, 1, 1, 1, 1, 0.5))
            },
            Array.Empty<DependencyEdge>());

        var view = GraphViewBuilder.Build(graph);

        var typeNode = Assert.Single(view.Nodes.Where(node => node.Id == "Sample.Outer.Inner"));
        Assert.Equal("Inner", typeNode.Label);
        Assert.Equal("type", typeNode.NodeKind);
    }

    [Fact]
    public void ノード情報とメンバーノードと集約ノードを生成できる()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"depsphere-methods-{Guid.NewGuid():N}");
        var projectDir = Path.Combine(tempRoot, "SampleProject");
        Directory.CreateDirectory(projectDir);
        var tempFile = Path.Combine(projectDir, "Impl.cs");
        var tempCsproj = Path.Combine(projectDir, "SampleProject.csproj");
        File.WriteAllText(tempCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        File.WriteAllText(
            tempFile,
            """
            namespace Sample.Domain;

            public class Impl
            {
                private int _counter;
                public event EventHandler? Changed;
                public string Name { get; set; } = string.Empty;
                public Impl() { }
                public void Execute() { }
                private int Compute() => _counter;
                public System.Text.StringBuilder BuildBuffer() => new();
            }
            """);

        try
        {
            var graph = new DependencyGraph(
                new[]
                {
                    new DependencyNode(
                        "Sample.Domain.Impl",
                        new TypeMetrics(1, 1, 1, 1, 1, 1, 0.5))
                    {
                        Location = new SourceLocation(tempFile, 3, 1, 12, 2)
                    }
                },
                Array.Empty<DependencyEdge>());

            var view = GraphViewBuilder.Build(graph);

            var classNode = Assert.Single(view.Nodes.Where(node => node.Id == "Sample.Domain.Impl"));
            Assert.Equal("type", classNode.NodeKind);
            Assert.Equal(new[] { "BuildBuffer", "Compute", "Execute", "Impl" }, classNode.MethodNames);
            Assert.Equal(new[] { "Name" }, classNode.PropertyNames);
            Assert.Equal(new[] { "_counter" }, classNode.FieldNames);
            Assert.Equal(new[] { "Changed" }, classNode.EventNames);

            var memberNodes = view.Nodes
                .Where(node => node.OwnerNodeId == "Sample.Domain.Impl")
                .OrderBy(node => node.Id, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(7, memberNodes.Length);
            Assert.Contains(memberNodes, node => node.NodeKind == "method" && node.Label == "Compute()");
            Assert.Contains(memberNodes, node => node.NodeKind == "method" && node.Label == "Execute()");
            Assert.Contains(memberNodes, node => node.NodeKind == "method" && node.Label == "Impl()");
            Assert.Contains(memberNodes, node => node.NodeKind == "method" && node.Label == "BuildBuffer()");
            Assert.Contains(memberNodes, node => node.NodeKind == "property" && node.Label == "Name");
            Assert.Contains(memberNodes, node => node.NodeKind == "field" && node.Label == "_counter");
            Assert.Contains(memberNodes, node => node.NodeKind == "event" && node.Label == "Changed event");

            var memberEdges = view.Edges
                .Where(edge => edge.From == "Sample.Domain.Impl" && edge.Kind == "member")
                .ToArray();
            Assert.Equal(memberNodes.Length, memberEdges.Length);

            Assert.Contains(view.Nodes, node => node.NodeKind == "project" && node.Label == "SampleProject");
            Assert.Contains(view.Nodes, node => node.NodeKind == "namespace" && node.Label == "Sample.Domain");
            Assert.Contains(view.Nodes, node => node.NodeKind == "file" && node.Label == "Impl.cs");
            Assert.Contains(view.Nodes, node => node.NodeKind == "external" && node.Label == "ext:System");
            Assert.Contains(view.Edges, edge => edge.From == "Sample.Domain.Impl" && edge.Kind == "external");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void 判定不能な外部型はexternalノードを生成しない()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"depsphere-external-unknown-{Guid.NewGuid():N}");
        var projectDir = Path.Combine(tempRoot, "SampleProject");
        Directory.CreateDirectory(projectDir);
        var tempFile = Path.Combine(projectDir, "Impl.cs");
        var tempCsproj = Path.Combine(projectDir, "SampleProject.csproj");
        File.WriteAllText(tempCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        File.WriteAllText(
            tempFile,
            """
            namespace Sample.Domain;

            public class Impl
            {
                public Foo Value { get; set; }
            }
            """);

        try
        {
            var graph = new DependencyGraph(
                new[]
                {
                    new DependencyNode(
                        "Sample.Domain.Impl",
                        new TypeMetrics(1, 1, 1, 1, 1, 1, 0.5))
                    {
                        Location = new SourceLocation(tempFile, 3, 1, 6, 2)
                    }
                },
                Array.Empty<DependencyEdge>());

            var view = GraphViewBuilder.Build(graph);

            Assert.DoesNotContain(view.Nodes, node => node.NodeKind == "external");
            Assert.DoesNotContain(view.Edges, edge => edge.Kind == "external");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void 三次元表示用グラフをJSONへ変換できる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });
        var view = GraphViewBuilder.Build(graph);

        var json = GraphViewJsonSerializer.Serialize(view);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("nodes", out var nodes));
        Assert.True(doc.RootElement.TryGetProperty("edges", out var edges));
        Assert.True(nodes.GetArrayLength() > 0);
        Assert.True(edges.GetArrayLength() > 0);
    }

    [Fact]
    public void Hotspot閾値を変更するとレベル判定件数が変わる()
    {
        var nodes = Enumerable.Range(1, 10)
            .Select(index =>
                new DependencyNode(
                    $"Node{index}",
                    new TypeMetrics(0, 0, 0, 0, 0, 0, 100 - index)))
            .ToArray();
        var graph = new DependencyGraph(nodes, Array.Empty<DependencyEdge>());
        var options = new AnalysisOptions
        {
            HotspotTopPercent = 0.40,
            CriticalTopPercent = 0.20
        };

        var view = GraphViewBuilder.Build(graph, options);

        Assert.Equal(2, view.Nodes.Count(node => node.Level == "critical"));
        Assert.Equal(2, view.Nodes.Count(node => node.Level == "hotspot"));
        Assert.Equal(6, view.Nodes.Count(node => node.Level == "normal"));
    }

    [Fact]
    public void Critical閾値がHotspot閾値より大きい場合は例外になる()
    {
        var graph = DependencyAnalyzer.Analyze(new[] { Source });
        var options = new AnalysisOptions
        {
            HotspotTopPercent = 0.10,
            CriticalTopPercent = 0.20
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = GraphViewBuilder.Build(graph, options);
        });
    }
}
