using DepSphere.Analyzer;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace DepSphere.App;

public partial class MainWindow : Window
{
    private DependencyGraph? _currentGraph;
    private string? _currentAnalysisPath;
    private CancellationTokenSource? _analysisCts;
    private bool _isAnalyzing;

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
        if (_isAnalyzing)
        {
            StatusText.Text = "解析中はサンプル読込できません。";
            return;
        }

        _currentAnalysisPath = null;
        ProjectPathTextBox.Text = string.Empty;
        await LoadSampleAsync();
    }

    private async void OnReloadClick(object sender, RoutedEventArgs e)
    {
        if (_isAnalyzing)
        {
            StatusText.Text = "解析中です。キャンセル後に再実行してください。";
            return;
        }

        if (!string.IsNullOrWhiteSpace(_currentAnalysisPath))
        {
            if (!TryGetProgressInterval(ProgressIntervalTextBox.Text, out var progressInterval, out var progressError))
            {
                StatusText.Text = progressError ?? "進捗更新間隔が不正です。";
                return;
            }

            await AnalyzeProjectAsync(_currentAnalysisPath, progressInterval);
            return;
        }

        await LoadSampleAsync();
    }

    private void OnBrowseProjectClick(object sender, RoutedEventArgs e)
    {
        if (_isAnalyzing)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "解析対象を選択",
            Filter = "C# Solution/Project (*.sln;*.csproj)|*.sln;*.csproj|Solution (*.sln)|*.sln|C# Project (*.csproj)|*.csproj",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ProjectPathTextBox.Text = dialog.FileName;
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        if (_isAnalyzing)
        {
            StatusText.Text = "解析中です。キャンセル後に再実行してください。";
            return;
        }

        if (!TryGetValidProjectPath(ProjectPathTextBox.Text, out var path, out var errorMessage))
        {
            StatusText.Text = errorMessage ?? "解析対象が不正です。";
            return;
        }

        if (!TryGetProgressInterval(ProgressIntervalTextBox.Text, out var progressInterval, out var progressError))
        {
            StatusText.Text = progressError ?? "進捗更新間隔が不正です。";
            return;
        }

        _currentAnalysisPath = path;
        await AnalyzeProjectAsync(path, progressInterval);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (!_isAnalyzing || _analysisCts is null)
        {
            StatusText.Text = "実行中の解析はありません。";
            return;
        }

        CancelButton.IsEnabled = false;
        StatusText.Text = "キャンセル要求を送信しました...";
        _analysisCts.Cancel();
    }

    private async Task LoadSampleAsync()
    {
        StatusText.Text = "サンプル解析中...";

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

        var graph = DependencyAnalyzer.Analyze(new[] { sourceA, sourceB });
        RenderGraph(graph, "サンプル解析完了");
        await Task.CompletedTask;
    }

    private async Task AnalyzeProjectAsync(string path, int progressInterval)
    {
        if (_isAnalyzing)
        {
            return;
        }

        using var cts = new CancellationTokenSource();
        _analysisCts = cts;
        SetAnalysisState(isAnalyzing: true, canCancel: true);
        StatusText.Text = "解析中...";
        var progress = new Progress<AnalysisProgress>(item =>
        {
            if (_isAnalyzing)
            {
                StatusText.Text = FormatProgress(item);
            }
        });

        try
        {
            var options = new AnalysisOptions
            {
                MetricsProgressReportInterval = progressInterval
            };
            var graph = await DependencyAnalyzer.AnalyzePathAsync(path, options, progress, cts.Token);
            RenderGraph(graph, $"解析完了: {Path.GetFileName(path)}");
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "解析をキャンセルしました。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "解析失敗: " + ex.Message;
        }
        finally
        {
            if (ReferenceEquals(_analysisCts, cts))
            {
                _analysisCts = null;
            }

            SetAnalysisState(isAnalyzing: false, canCancel: false);
        }
    }

    private void RenderGraph(DependencyGraph graph, string statusPrefix)
    {
        _currentGraph = graph;
        var view = GraphViewBuilder.Build(graph);
        GraphWebView.NavigateToString(GraphViewHtmlBuilder.Build(view));
        CodeWebView.NavigateToString(BuildInitialCodeViewHtml());
        SelectedNodeText.Text = "(未選択)";
        StatusText.Text = $"{statusPrefix} / ノード {view.Nodes.Count} / エッジ {view.Edges.Count}";
    }

    private static string BuildInitialCodeViewHtml()
    {
        var initial = SourceCodeViewerHtmlBuilder.Build(
            new SourceCodeDocument(
                "(未選択)",
                1,
                1,
                "ノードをクリックするとコードを表示します。"));
        return initial;
    }

    private static bool TryGetValidProjectPath(string? input, out string path, out string? errorMessage)
    {
        path = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = ".sln または .csproj を入力してください。";
            return false;
        }

        var trimmed = input.Trim();
        if (!File.Exists(trimmed))
        {
            errorMessage = "指定ファイルが存在しません。";
            return false;
        }

        var extension = Path.GetExtension(trimmed);
        if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = ".sln または .csproj のみ対応です。";
            return false;
        }

        path = trimmed;
        return true;
    }

    private static bool TryGetProgressInterval(string? input, out int interval, out string? errorMessage)
    {
        interval = AnalysisOptions.DefaultMetricsProgressReportInterval;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (!int.TryParse(input.Trim(), out interval))
        {
            errorMessage = "進捗更新間隔は数値で入力してください。";
            return false;
        }

        if (interval < 1 || interval > 10000)
        {
            errorMessage = "進捗更新間隔は 1 から 10000 の範囲で入力してください。";
            return false;
        }

        return true;
    }

    private static string FormatProgress(AnalysisProgress progress)
    {
        if (progress.Current is int current && progress.Total is int total && total > 0)
        {
            return $"{progress.Message} ({current}/{total})";
        }

        return progress.Message;
    }

    private void SetAnalysisState(bool isAnalyzing, bool canCancel)
    {
        _isAnalyzing = isAnalyzing;
        LoadSampleButton.IsEnabled = !isAnalyzing;
        ReloadButton.IsEnabled = !isAnalyzing;
        BrowseProjectButton.IsEnabled = !isAnalyzing;
        AnalyzeButton.IsEnabled = !isAnalyzing;
        ProjectPathTextBox.IsEnabled = !isAnalyzing;
        ProgressIntervalTextBox.IsEnabled = !isAnalyzing;
        CancelButton.IsEnabled = isAnalyzing && canCancel;
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
