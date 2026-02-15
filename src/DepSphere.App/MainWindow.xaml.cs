using DepSphere.Analyzer;
using System.Text;
using System.Windows;

namespace DepSphere.App;

public partial class MainWindow : Window
{
    private DependencyGraph? _currentGraph;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await GraphWebView.EnsureCoreWebView2Async();
            await CodeWebView.EnsureCoreWebView2Async();

            GraphWebView.CoreWebView2.WebMessageReceived += OnGraphMessageReceived;

            await LoadSampleAsync();
            StatusText.Text = "準備完了";
        }
        catch (Exception ex)
        {
            StatusText.Text = "初期化失敗: " + ex.Message;
        }
    }

    private async void OnLoadSampleClick(object sender, RoutedEventArgs e)
    {
        await LoadSampleAsync();
    }

    private async void OnReloadClick(object sender, RoutedEventArgs e)
    {
        await LoadSampleAsync();
    }

    private async Task LoadSampleAsync()
    {
        var sourceA = """
            namespace Demo;

            public interface IService { }
            public class Base { }
            public class Dependency { public static void Touch(){} }

            public class Impl : Base, IService
            {
                private readonly Dependency _field = new();

                public Dependency Run(Dependency arg)
                {
                    Dependency.Touch();
                    return arg;
                }
            }
            """;

        var sourceB = """
            namespace Demo;
            public class Consumer
            {
                public Impl Build() => new Impl();
            }
            """;

        _currentGraph = DependencyAnalyzer.Analyze(new[] { sourceA, sourceB });
        var view = GraphViewBuilder.Build(_currentGraph);

        GraphWebView.NavigateToString(GraphViewHtmlBuilder.Build(view));

        var initial = SourceCodeViewerHtmlBuilder.Build(
            new SourceCodeDocument(
                "(未選択)",
                1,
                1,
                "ノードをクリックするとコードを表示します。"));
        CodeWebView.NavigateToString(initial);

        SelectedNodeText.Text = "(未選択)";
        StatusText.Text = $"ノード {view.Nodes.Count} / エッジ {view.Edges.Count}";

        await Task.CompletedTask;
    }

    private void OnGraphMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_currentGraph is null)
        {
            return;
        }

        if (!GraphHostMessageParser.TryParse(e.WebMessageAsJson, out var message) || message is null)
        {
            return;
        }

        if (!string.Equals(message.Type, "nodeSelected", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message.NodeId))
        {
            return;
        }

        SelectedNodeText.Text = message.NodeId;

        if (GraphSelectionCoordinator.TryOpenFromMessage(_currentGraph, e.WebMessageAsJson, out var document) && document is not null)
        {
            CodeWebView.NavigateToString(SourceCodeViewerHtmlBuilder.Build(document));
            StatusText.Text = "コード表示を更新";
            return;
        }

        var fallback = BuildFallbackDocument(_currentGraph, message.NodeId);
        CodeWebView.NavigateToString(SourceCodeViewerHtmlBuilder.Build(fallback));
        StatusText.Text = "メタ情報表示にフォールバック";
    }

    private static SourceCodeDocument BuildFallbackDocument(DependencyGraph graph, string nodeId)
    {
        var node = graph.Nodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is null)
        {
            return new SourceCodeDocument("(unknown)", 1, 1, "ノードが見つかりません。");
        }

        var builder = new StringBuilder();
        builder.AppendLine("// SourceLocation が無いためメタ情報を表示");
        builder.AppendLine($"// Node: {node.Id}");
        builder.AppendLine($"// Weight: {node.Metrics.WeightScore:F3}");
        builder.AppendLine($"// Methods: {node.Metrics.MethodCount}, Statements: {node.Metrics.StatementCount}");
        builder.AppendLine($"// Branches: {node.Metrics.BranchCount}, CallSites: {node.Metrics.CallSiteCount}");
        builder.AppendLine($"// FanOut: {node.Metrics.FanOut}, InDegree: {node.Metrics.InDegree}");

        return new SourceCodeDocument("(metadata)", 1, 1, builder.ToString());
    }
}
