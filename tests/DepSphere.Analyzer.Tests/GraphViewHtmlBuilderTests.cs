using DepSphere.Analyzer;
using System.Text;
using System.Text.RegularExpressions;

namespace DepSphere.Analyzer.Tests;

public class GraphViewHtmlBuilderTests
{
    [Fact]
    public void HTMLにグラフデータと描画要素を含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("dep-graph-canvas", html);
        Assert.Contains("three.min.js", html);
        Assert.Contains("window.__depSphereGraph", html);

        var match = Regex.Match(html, "atob\\(\\\"(?<base64>[A-Za-z0-9+/=]+)\\\"\\)");
        Assert.True(match.Success);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups["base64"].Value));
        Assert.Contains("Sample.Impl", decoded);
    }

    [Fact]
    public void ノード選択通知スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("chrome.webview.postMessage", html);
        Assert.Contains("nodeSelected", html);
    }

    [Fact]
    public void グラフ操作とスケール調整UIを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("createCameraController", html);
        Assert.Contains("dragMode = 'rotate'", html);
        Assert.Contains("dragMode = 'pan'", html);
        Assert.Contains("surface.addEventListener('wheel', handleWheel", html);
        Assert.Contains("node-scale", html);
        Assert.Contains("spread-scale", html);
        Assert.Contains("clear-filter", html);
        Assert.Contains("fit-view", html);
        Assert.Contains("history-back", html);
        Assert.Contains("history-forward", html);
        Assert.Contains("filter-status", html);
        Assert.Contains("search-input", html);
        Assert.Contains("overlay-toggle", html);
        Assert.Contains("node-info-toggle", html);
        Assert.Contains("操作UIを隠す", html);
        Assert.Contains("ノード情報を隠す", html);
        Assert.Contains("表示限定解除", html);
        Assert.Contains("Fit to View", html);
        Assert.Contains("戻る", html);
        Assert.Contains("進む", html);
        Assert.Contains("ホバー: ノード強調", html);
        Assert.Contains("ラベルLOD: 遠景は重要ノード中心", html);
        Assert.DoesNotContain("OrbitControls.js", html);
        Assert.DoesNotContain("scene.rotation.y +=", html);
    }

    [Fact]
    public void シングルクリックは接続ノード絞り込みダブルクリックはコード表示を行う()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("setConnectedNodeFilter", html);
        Assert.Contains("canvas.addEventListener('click'", html);
        Assert.Contains("canvas.addEventListener('dblclick'", html);
        Assert.Contains("singleClickTimer = setTimeout", html);
        Assert.Contains("clearTimeout(singleClickTimer)", html);
        Assert.Contains("focusNodeById(selectedId, false, true)", html);
        Assert.Contains("postNodeSelected(nodeId)", html);
    }

    [Fact]
    public void ホバー強調スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("let hoveredNodeId = null", html);
        Assert.Contains("setHoveredNode", html);
        Assert.Contains("canvas.addEventListener('pointerleave'", html);
        Assert.Contains("mesh.material.emissive.setHex", html);
        Assert.Contains("label.material.opacity", html);
    }

    [Fact]
    public void ラベルLOD制御スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("function shouldShowLabelForMesh(mesh)", html);
        Assert.Contains("function updateLabelLod()", html);
        Assert.Contains("label.visible = shouldShowLabelForMesh(mesh);", html);
        Assert.Contains("cameraControl.getTarget()", html);
    }

    [Fact]
    public void ショートカットとFit操作スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("window.addEventListener('keydown'", html);
        Assert.Contains("event.key === 'Escape'", html);
        Assert.Contains("key === 'f'", html);
        Assert.Contains("event.altKey && key === 'arrowleft'", html);
        Assert.Contains("event.altKey && key === 'arrowright'", html);
        Assert.Contains("searchInput.focus()", html);
        Assert.Contains("fitVisibleNodes", html);
        Assert.Contains("fitToPoints", html);
    }

    [Fact]
    public void 履歴ナビゲーションスクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("const historyEntries = []", html);
        Assert.Contains("function pushHistoryState()", html);
        Assert.Contains("function navigateHistory(offset)", html);
        Assert.Contains("historyBackButton.addEventListener('click'", html);
        Assert.Contains("historyForwardButton.addEventListener('click'", html);
        Assert.Contains("captureState", html);
        Assert.Contains("applyState", html);
    }

    [Fact]
    public void ノード情報パネルにメソッド名表示ロジックを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("node.methodNames", html);
        Assert.Contains("Methods:", html);
        Assert.Contains("node-info-body", html);
    }

    [Fact]
    public void フォーカス操作スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("window.depSphereFocusNode", html);
    }

    [Fact]
    public void WebViewメッセージを解析できる()
    {
        var raw = "{\"type\":\"nodeSelected\",\"nodeId\":\"Sample.Impl\"}";

        var ok = GraphHostMessageParser.TryParse(raw, out var message);

        Assert.True(ok);
        Assert.NotNull(message);
        Assert.Equal("nodeSelected", message!.Type);
        Assert.Equal("Sample.Impl", message.NodeId);
    }

    [Fact]
    public void 文字列化されたWebViewメッセージも解析できる()
    {
        var raw = "\"{\\\"type\\\":\\\"nodeSelected\\\",\\\"nodeId\\\":\\\"Sample.Impl\\\"}\"";

        var ok = GraphHostMessageParser.TryParse(raw, out var message);

        Assert.True(ok);
        Assert.NotNull(message);
        Assert.Equal("nodeSelected", message!.Type);
        Assert.Equal("Sample.Impl", message.NodeId);
    }

    [Fact]
    public void フォーカスコマンドスクリプトを生成できる()
    {
        var script = GraphViewScriptCommandBuilder.BuildFocusNodeScript("Sample.Impl");

        Assert.Contains("depSphereFocusNode", script);
        Assert.Contains("Sample.Impl", script);
    }

    private static GraphView BuildSampleView()
    {
        return new GraphView(
            new[]
            {
                new GraphViewNode(
                    "Sample.Impl",
                    "Impl",
                    0,
                    0,
                    0,
                    16,
                    "#ef4444",
                    "critical",
                    new TypeMetrics(3, 20, 4, 6, 2, 1, 0.92))
                {
                    MethodNames = new[] { "Compute", "Execute", "Impl" }
                }
            },
            new[]
            {
                new GraphViewEdge("Sample.Impl", "Sample.Base", "inherit", "#22c55e")
            });
    }
}
