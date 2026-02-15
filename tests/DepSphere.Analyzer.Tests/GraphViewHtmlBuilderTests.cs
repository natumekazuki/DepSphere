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
        Assert.Contains("表示限定解除", html);
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
        Assert.Contains("postNodeSelected(selectedId)", html);
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
