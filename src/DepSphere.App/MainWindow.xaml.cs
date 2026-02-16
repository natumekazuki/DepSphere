using DepSphere.Analyzer;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace DepSphere.App;

public partial class MainWindow : Window
{
    private const int CodeNavigationHistoryLimit = 120;
    private DependencyGraph? _currentGraph;
    private string? _currentAnalysisPath;
    private CancellationTokenSource? _analysisCts;
    private bool _isAnalyzing;
    private bool _isWebViewInitialized;
    private Task? _webViewInitializationTask;
    private string? _lastErrorDetails;
    private bool _isOperationsPanelVisible = true;
    private GridLength _operationsPanelWidth = new(280);
    private readonly List<string> _codeNavigationHistory = [];
    private int _codeNavigationHistoryIndex = -1;
    private bool _isNavigatingCodeHistory;

    public MainWindow()
    {
        InitializeComponent();
        ApplyOperationsPanelVisibility();
        SetInitializationState(false);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "WebView2 初期化中...";
        if (!await EnsureWebView2InitializedAsync())
        {
            return;
        }

        GraphWebView.NavigateToString(BuildInitialGraphViewHtml());
        CodeWebView.NavigateToString(BuildInitialCodeViewHtml());
        SelectedNodeText.Text = "(未選択)";
        ResetCodeNavigationHistory();
        SetStatusMessage("準備完了。解析対象を指定してください。");
    }

    private async void OnLoadSampleClick(object sender, RoutedEventArgs e)
    {
        if (_isAnalyzing)
        {
            StatusText.Text = "解析中はサンプル読込できません。";
            return;
        }

        if (!await EnsureWebView2InitializedAsync())
        {
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

        if (!await EnsureWebView2InitializedAsync())
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_currentAnalysisPath))
        {
            await AnalyzeProjectAsync(_currentAnalysisPath);
            return;
        }

        SetStatusMessage("再解析対象がありません。解析対象を指定して実行してください。");
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

        if (!await EnsureWebView2InitializedAsync())
        {
            return;
        }

        if (!TryGetValidProjectPath(ProjectPathTextBox.Text, out var path, out var errorMessage))
        {
            SetStatusMessage(errorMessage ?? "解析対象が不正です。");
            return;
        }

        _currentAnalysisPath = path;
        await AnalyzeProjectAsync(path);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (!_isAnalyzing || _analysisCts is null)
        {
            SetStatusMessage("実行中の解析はありません。");
            return;
        }

        CancelButton.IsEnabled = false;
        SetStatusMessage("キャンセル要求を送信しました...");
        _analysisCts.Cancel();
    }

    private void OnToggleOperationsPanelClick(object sender, RoutedEventArgs e)
    {
        _isOperationsPanelVisible = !_isOperationsPanelVisible;
        ApplyOperationsPanelVisibility();
    }

    private async Task LoadSampleAsync()
    {
        if (!await EnsureWebView2InitializedAsync())
        {
            return;
        }

        SetStatusMessage("サンプル解析中...");

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
        RenderGraph(graph, "サンプル解析完了", options: null);
        await Task.CompletedTask;
    }

    private async Task AnalyzeProjectAsync(string path)
    {
        if (_isAnalyzing)
        {
            return;
        }

        if (!await EnsureWebView2InitializedAsync())
        {
            return;
        }

        using var cts = new CancellationTokenSource();
        _analysisCts = cts;
        SetAnalysisState(isAnalyzing: true, canCancel: true);
        SetStatusMessage("解析中...");
        var progress = new Progress<AnalysisProgress>(item =>
        {
            if (_isAnalyzing)
            {
                SetStatusMessage(FormatProgress(item));
            }
        });

        try
        {
            var options = new AnalysisOptions();
            var graph = await DependencyAnalyzer.AnalyzePathAsync(path, options, progress, cts.Token);
            RenderGraph(graph, $"解析完了: {Path.GetFileName(path)}", options);
        }
        catch (OperationCanceledException)
        {
            SetStatusMessage("解析をキャンセルしました。");
        }
        catch (Exception ex)
        {
            SetErrorDetails("解析失敗: " + ex.Message, ex);
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

    private void RenderGraph(DependencyGraph graph, string statusPrefix, AnalysisOptions? options)
    {
        if (!_isWebViewInitialized)
        {
            SetStatusMessage("WebView2 初期化完了後に描画します。");
            return;
        }

        _currentGraph = graph;
        var view = GraphViewBuilder.Build(graph, options);
        GraphWebView.NavigateToString(GraphViewHtmlBuilder.Build(view));
        CodeWebView.NavigateToString(BuildInitialCodeViewHtml());
        SelectedNodeText.Text = "(未選択)";
        ResetCodeNavigationHistory();
        SetStatusMessage($"{statusPrefix} / ノード {view.Nodes.Count} / エッジ {view.Edges.Count}");
    }

    private static string BuildInitialGraphViewHtml()
    {
        return """
<!DOCTYPE html>
<html lang="ja">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>DepSphere Graph Viewer</title>
  <style>
    html, body {
      margin: 0;
      width: 100%;
      height: 100%;
      overflow: hidden;
      background: #020617;
      color: #cbd5e1;
      font-family: "Segoe UI", sans-serif;
    }
    .empty {
      width: 100%;
      height: 100%;
      display: grid;
      place-items: center;
      text-align: center;
      padding: 24px;
      box-sizing: border-box;
    }
  </style>
</head>
<body>
  <div class="empty">
    <div>
      <h2>グラフはまだ表示されていません</h2>
      <p>.sln または .csproj を指定して解析を実行してください。</p>
    </div>
  </div>
</body>
</html>
""";
    }

    private static string BuildInitialCodeViewHtml()
    {
        var initial = SourceCodeViewerHtmlBuilder.Build(
            new SourceCodeDocument(
                "(未選択)",
                1,
                1,
                "ノードをダブルクリックするとコードを表示します。"));
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
        var canInteract = _isWebViewInitialized && !isAnalyzing;
        LoadSampleButton.IsEnabled = canInteract;
        ReloadButton.IsEnabled = canInteract;
        BrowseProjectButton.IsEnabled = canInteract;
        AnalyzeButton.IsEnabled = canInteract;
        ProjectPathTextBox.IsEnabled = canInteract;
        CancelButton.IsEnabled = _isWebViewInitialized && isAnalyzing && canCancel;
        UpdateCodeNavigationButtonState();
    }

    private void OnGraphMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        HandleNodeSelectedMessage(e.WebMessageAsJson, focusGraph: false);
    }

    private void OnCodeMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        HandleNodeSelectedMessage(e.WebMessageAsJson, focusGraph: true);
    }

    private void HandleNodeSelectedMessage(string rawMessage, bool focusGraph)
    {
        if (!_isWebViewInitialized || _currentGraph is null)
        {
            return;
        }

        if (!GraphHostMessageParser.TryParse(rawMessage, out var message) || message is null)
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

        OpenNodeInCodePane(message.NodeId, focusGraph, appendHistory: true);
    }

    private void OpenNodeInCodePane(string nodeId, bool focusGraph, bool appendHistory)
    {
        if (!_isWebViewInitialized || _currentGraph is null || string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        SelectedNodeText.Text = nodeId;
        SourceCodeDocument document;
        string status;

        try
        {
            document = SourceCodeViewer.OpenNode(_currentGraph, nodeId);
            status = "コード表示を更新";
        }
        catch (InvalidOperationException)
        {
            document = BuildFallbackDocument(_currentGraph, nodeId);
            status = "メタ情報表示にフォールバック";
        }

        var symbolLinks = BuildSymbolLinkMap(_currentGraph, document);
        CodeWebView.NavigateToString(SourceCodeViewerHtmlBuilder.Build(document, symbolLinks));

        if (focusGraph)
        {
            FocusGraphNode(nodeId);
        }

        if (appendHistory)
        {
            AppendCodeNavigationHistory(nodeId);
        }

        SetStatusMessage(status);
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

    private async Task<bool> EnsureWebView2InitializedAsync()
    {
        if (_isWebViewInitialized)
        {
            return true;
        }

        _webViewInitializationTask ??= InitializeWebView2CoreAsync();

        try
        {
            await _webViewInitializationTask;
            return _isWebViewInitialized;
        }
        catch (Exception ex)
        {
            _webViewInitializationTask = null;
            SetErrorDetails("初期化失敗: " + ex.Message, ex);
            return false;
        }
    }

    private async Task InitializeWebView2CoreAsync()
    {
        ConfigureWebViewCreationProperties();
        await GraphWebView.EnsureCoreWebView2Async();
        await CodeWebView.EnsureCoreWebView2Async();

        GraphWebView.CoreWebView2.WebMessageReceived -= OnGraphMessageReceived;
        GraphWebView.CoreWebView2.WebMessageReceived += OnGraphMessageReceived;
        CodeWebView.CoreWebView2.WebMessageReceived -= OnCodeMessageReceived;
        CodeWebView.CoreWebView2.WebMessageReceived += OnCodeMessageReceived;

        SetInitializationState(true);
    }

    private void SetInitializationState(bool initialized)
    {
        _isWebViewInitialized = initialized;
        SetAnalysisState(_isAnalyzing, _analysisCts is not null && _isAnalyzing);
    }

    private void ConfigureWebViewCreationProperties()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DepSphere",
            "WebView2");
        var graphUserData = Path.Combine(root, "Graph");
        var codeUserData = Path.Combine(root, "Code");
        Directory.CreateDirectory(graphUserData);
        Directory.CreateDirectory(codeUserData);

        GraphWebView.CreationProperties ??= new CoreWebView2CreationProperties();
        CodeWebView.CreationProperties ??= new CoreWebView2CreationProperties();

        GraphWebView.CreationProperties.UserDataFolder = graphUserData;
        CodeWebView.CreationProperties.UserDataFolder = codeUserData;
    }

    private void SetStatusMessage(string message)
    {
        StatusText.Text = message;
    }

    private void OnCodeHistoryBackClick(object sender, RoutedEventArgs e)
    {
        NavigateCodeHistory(-1);
    }

    private void OnCodeHistoryForwardClick(object sender, RoutedEventArgs e)
    {
        NavigateCodeHistory(1);
    }

    private void AppendCodeNavigationHistory(string nodeId)
    {
        if (_isNavigatingCodeHistory || string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        if (_codeNavigationHistoryIndex >= 0 &&
            _codeNavigationHistoryIndex < _codeNavigationHistory.Count &&
            string.Equals(_codeNavigationHistory[_codeNavigationHistoryIndex], nodeId, StringComparison.Ordinal))
        {
            UpdateCodeNavigationButtonState();
            return;
        }

        if (_codeNavigationHistoryIndex < _codeNavigationHistory.Count - 1)
        {
            _codeNavigationHistory.RemoveRange(
                _codeNavigationHistoryIndex + 1,
                _codeNavigationHistory.Count - _codeNavigationHistoryIndex - 1);
        }

        _codeNavigationHistory.Add(nodeId);
        if (_codeNavigationHistory.Count > CodeNavigationHistoryLimit)
        {
            var overflow = _codeNavigationHistory.Count - CodeNavigationHistoryLimit;
            _codeNavigationHistory.RemoveRange(0, overflow);
        }

        _codeNavigationHistoryIndex = _codeNavigationHistory.Count - 1;
        UpdateCodeNavigationButtonState();
    }

    private void NavigateCodeHistory(int offset)
    {
        if (_codeNavigationHistory.Count == 0 || _currentGraph is null || _isAnalyzing)
        {
            UpdateCodeNavigationButtonState();
            return;
        }

        var nextIndex = _codeNavigationHistoryIndex + offset;
        if (nextIndex < 0 || nextIndex >= _codeNavigationHistory.Count)
        {
            UpdateCodeNavigationButtonState();
            return;
        }

        _codeNavigationHistoryIndex = nextIndex;
        var nodeId = _codeNavigationHistory[_codeNavigationHistoryIndex];

        _isNavigatingCodeHistory = true;
        try
        {
            OpenNodeInCodePane(nodeId, focusGraph: true, appendHistory: false);
        }
        finally
        {
            _isNavigatingCodeHistory = false;
            UpdateCodeNavigationButtonState();
        }
    }

    private void ResetCodeNavigationHistory()
    {
        _codeNavigationHistory.Clear();
        _codeNavigationHistoryIndex = -1;
        _isNavigatingCodeHistory = false;
        UpdateCodeNavigationButtonState();
    }

    private void UpdateCodeNavigationButtonState()
    {
        if (!_isWebViewInitialized || _isAnalyzing || _currentGraph is null)
        {
            CodeHistoryBackButton.IsEnabled = false;
            CodeHistoryForwardButton.IsEnabled = false;
            return;
        }

        CodeHistoryBackButton.IsEnabled = _codeNavigationHistoryIndex > 0;
        CodeHistoryForwardButton.IsEnabled =
            _codeNavigationHistoryIndex >= 0 &&
            _codeNavigationHistoryIndex < _codeNavigationHistory.Count - 1;
    }

    private void ApplyOperationsPanelVisibility()
    {
        if (_isOperationsPanelVisible)
        {
            var restored = _operationsPanelWidth.Value > 80 ? _operationsPanelWidth : new GridLength(280);
            LeftPanelColumn.Width = restored;
            LeftSplitterColumn.Width = new GridLength(6);
            OperationsPanelBorder.Visibility = Visibility.Visible;
            LeftPanelSplitter.Visibility = Visibility.Visible;
            ToggleOperationsPanelButton.Content = "操作パネルを隠す";
            return;
        }

        if (LeftPanelColumn.Width.Value > 0)
        {
            _operationsPanelWidth = LeftPanelColumn.Width;
        }

        LeftPanelColumn.Width = new GridLength(0);
        LeftSplitterColumn.Width = new GridLength(0);
        OperationsPanelBorder.Visibility = Visibility.Collapsed;
        LeftPanelSplitter.Visibility = Visibility.Collapsed;
        ToggleOperationsPanelButton.Content = "操作パネルを表示";
    }

    private void FocusGraphNode(string nodeId)
    {
        if (!_isWebViewInitialized || GraphWebView.CoreWebView2 is null || string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        var script = GraphViewScriptCommandBuilder.BuildFocusNodeScript(nodeId);
        _ = GraphWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private static IReadOnlyDictionary<string, string> BuildSymbolLinkMap(DependencyGraph graph, SourceCodeDocument document)
    {
        var tokens = Regex.Matches(document.Content ?? string.Empty, @"\b[A-Za-z_][A-Za-z0-9_]*\b")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var bySimpleName = graph.Nodes
            .GroupBy(node => ExtractSimpleTypeName(node.Id), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Id).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            if (!bySimpleName.TryGetValue(token, out var candidates))
            {
                continue;
            }

            if (candidates.Length == 1)
            {
                result[token] = candidates[0];
            }
        }

        return result;
    }

    private static string ExtractSimpleTypeName(string nodeId)
    {
        var lastDot = nodeId.LastIndexOf('.');
        var simple = lastDot >= 0 ? nodeId[(lastDot + 1)..] : nodeId;
        var genericStart = simple.IndexOf('<');
        return genericStart >= 0 ? simple[..genericStart] : simple;
    }

    private void SetErrorDetails(string summary, Exception ex)
    {
        var builder = new StringBuilder();
        builder.AppendLine(summary);
        builder.AppendLine("----");
        builder.AppendLine(ex.GetType().FullName ?? ex.GetType().Name);
        builder.AppendLine($"HResult: 0x{ex.HResult:X8}");
        builder.AppendLine(ex.Message);
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            builder.AppendLine("---- StackTrace ----");
            builder.AppendLine(ex.StackTrace);
        }

        _lastErrorDetails = builder.ToString();
        ErrorDetailsTextBox.Text = _lastErrorDetails;
        ErrorDetailsTextBox.Visibility = Visibility.Visible;
        CopyErrorButton.IsEnabled = true;
        SetStatusMessage(summary);
    }

    private void OnCopyErrorClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastErrorDetails))
        {
            SetStatusMessage("コピー対象のエラー詳細がありません。");
            return;
        }

        try
        {
            Clipboard.SetText(_lastErrorDetails);
            SetStatusMessage("エラー詳細をクリップボードにコピーしました。");
        }
        catch (Exception ex)
        {
            SetStatusMessage("コピー失敗: " + ex.Message);
        }
    }
}
